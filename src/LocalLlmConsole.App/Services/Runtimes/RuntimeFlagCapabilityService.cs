namespace LocalLlmConsole.Services;

/// <summary>Result of querying a runtime for supported, unsupported, and unknown llama-server flags.</summary>
public sealed record RuntimeFlagCapabilityResult(
    IReadOnlySet<string> Supported,
    IReadOnlySet<string> Unsupported,
    IReadOnlySet<string> Unknown);

/// <summary>Queries a runtime executable for supported flags and caches the result in memory and on disk.</summary>
public sealed class RuntimeFlagCapabilityService
{
    private readonly IRuntimeFlagHelpRunner _runner;
    private readonly string _cacheDirectory;
    private readonly TimeSpan _cacheExpiration;
    private readonly Dictionary<string, RuntimeFlagCapabilityResult> _memoryCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _cacheLock = new();
    private readonly SemaphoreSlim _runLock = new(1, 1);

    public RuntimeFlagCapabilityService(IRuntimeFlagHelpRunner runner, string? cacheDirectory = null, TimeSpan? cacheExpiration = null)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _cacheDirectory = cacheDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "cache");
        _cacheExpiration = cacheExpiration ?? TimeSpan.FromMinutes(5);
    }

    public async Task<RuntimeFlagCapabilityResult> GetCapabilitiesAsync(
        string executablePath,
        RuntimeMode mode,
        string? wslDistro,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
            return new RuntimeFlagCapabilityResult(
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var key = BuildCacheKey(executablePath, mode, wslDistro);
        var filePath = Path.Combine(_cacheDirectory, $"llama-server-help-{key}.json");

        lock (_cacheLock)
        {
            if (_memoryCache.TryGetValue(key, out var cached))
                return cached;
        }

        if (TryReadCache(filePath, out var fileCached))
        {
            lock (_cacheLock)
            {
                _memoryCache[key] = fileCached;
            }
            return fileCached;
        }

        await _runLock.WaitAsync(cancellationToken);
        try
        {
            // Re-check caches after acquiring the lock so concurrent callers
            // don't spawn multiple llama-server --help processes for the same key.
            lock (_cacheLock)
            {
                if (_memoryCache.TryGetValue(key, out var cached))
                    return cached;
            }

            if (TryReadCache(filePath, out fileCached))
            {
                lock (_cacheLock)
                {
                    _memoryCache[key] = fileCached;
                }
                return fileCached;
            }

            var result = await RunAndParseAsync(executablePath, mode, wslDistro, cancellationToken);

            lock (_cacheLock)
            {
                _memoryCache[key] = result;
            }
            WriteCache(filePath, result);

            return result;
        }
        finally
        {
            _runLock.Release();
        }
    }

    public async Task<IReadOnlySet<string>> GetSupportedFlagsAsync(
        string executablePath,
        RuntimeMode mode,
        string? wslDistro,
        CancellationToken cancellationToken = default)
    {
        var capabilities = await GetCapabilitiesAsync(executablePath, mode, wslDistro, cancellationToken);
        return capabilities.Supported;
    }

    private static string BuildCacheKey(string executablePath, RuntimeMode mode, string? wslDistro)
    {
        var lastWrite = TryGetLastWriteTimeUtc(executablePath);
        var keyInput = string.Join('|', executablePath, mode, wslDistro ?? string.Empty, lastWrite?.Ticks.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        var bytes = Encoding.UTF8.GetBytes(keyInput);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static DateTime? TryGetLastWriteTimeUtc(string executablePath)
    {
        try
        {
            if (File.Exists(executablePath))
                return File.GetLastWriteTimeUtc(executablePath);
        }
        catch
        {
            // Ignore access errors.
        }
        return null;
    }

    private async Task<RuntimeFlagCapabilityResult> RunAndParseAsync(
        string executablePath,
        RuntimeMode mode,
        string? wslDistro,
        CancellationToken cancellationToken)
    {
        var runResult = await _runner.RunHelpAsync(executablePath, mode, wslDistro, cancellationToken);
        var output = runResult.ExitCode == 0 ? runResult.Output : runResult.Output + runResult.Error;
        var parsedFlags = RuntimeFlagHelpParser.ParseSupportedFlags(output);

        var schemaFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var flag in LlamaServerFlagSchema.All)
        {
            foreach (var name in flag.Names)
                schemaFlags.Add(name);
        }

        var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unknown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var flag in parsedFlags)
        {
            if (schemaFlags.Contains(flag))
                supported.Add(flag);
            else
                unknown.Add(flag);
        }

        var unsupported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var schemaFlag in schemaFlags)
        {
            if (!parsedFlags.Contains(schemaFlag))
                unsupported.Add(schemaFlag);
        }

        return new RuntimeFlagCapabilityResult(supported, unsupported, unknown);
    }

    private bool TryReadCache(string filePath, out RuntimeFlagCapabilityResult result)
    {
        result = new RuntimeFlagCapabilityResult(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        try
        {
            if (!File.Exists(filePath)) return false;
            var info = new FileInfo(filePath);
            if (DateTime.UtcNow - info.LastWriteTimeUtc > _cacheExpiration) return false;

            var json = File.ReadAllText(filePath);
            var document = JsonSerializer.Deserialize<JsonObject>(json);
            if (document is null) return false;

            result = new RuntimeFlagCapabilityResult(
                ReadStringSet(document, "supported"),
                ReadStringSet(document, "unsupported"),
                ReadStringSet(document, "unknown"));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void WriteCache(string filePath, RuntimeFlagCapabilityResult result)
    {
        try
        {
            Directory.CreateDirectory(_cacheDirectory);
            var document = new JsonObject
            {
                ["supported"] = new JsonArray(result.Supported.Select(s => (JsonNode?)s).ToArray()),
                ["unsupported"] = new JsonArray(result.Unsupported.Select(s => (JsonNode?)s).ToArray()),
                ["unknown"] = new JsonArray(result.Unknown.Select(s => (JsonNode?)s).ToArray())
            };
            File.WriteAllText(filePath, document.ToJsonString());
        }
        catch
        {
            // Cache writes are best-effort.
        }
    }

    private static IReadOnlySet<string> ReadStringSet(JsonObject document, string propertyName)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (document.TryGetPropertyValue(propertyName, out var node) && node is JsonArray array)
        {
            foreach (var item in array)
            {
                if (item is null) continue;
                var value = item.GetValue<string?>();
                if (!string.IsNullOrWhiteSpace(value))
                    set.Add(value);
            }
        }
        return set;
    }
}
