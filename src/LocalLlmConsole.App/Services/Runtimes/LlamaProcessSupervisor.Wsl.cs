
namespace LocalLlmConsole.Services;

public sealed partial class LlamaProcessSupervisor : IDisposable
{
    internal static string ToWslPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        if (value.StartsWith('/')) return value.Replace('\\', '/');
        var full = Path.GetFullPath(value);
        if (full.Length >= 3 && full[1] == ':' && (full[2] == '\\' || full[2] == '/'))
        {
            var drive = char.ToLowerInvariant(full[0]);
            var rest = full[3..].Replace('\\', '/');
            return $"/mnt/{drive}/{rest}";
        }
        return full.Replace('\\', '/');
    }

    internal static string ToWslPathList(string value, bool scaled)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var entries = value.Split(',', StringSplitOptions.TrimEntries);
        var converted = new List<string>(entries.Length);
        foreach (var entry in entries)
        {
            if (scaled)
            {
                var lastColon = entry.LastIndexOf(':');
                if (lastColon > 1)
                {
                    var path = entry[..lastColon].Trim();
                    var scale = entry[(lastColon + 1)..].Trim();
                    converted.Add($"{ToWslPath(path)}:{scale}");
                    continue;
                }
            }
            converted.Add(ToWslPath(entry));
        }
        return string.Join(",", converted);
    }

    internal static RuntimeLaunchRequest ConvertFlagValuesForWsl(RuntimeLaunchRequest request)
    {
        var convertedFlagValues = request.FlagValues.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value,
            StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in request.FlagValues)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            var flag = LlamaServerFlagSchema.FindByName(key);
            if (flag?.ValueType is FlagValueType.File or FlagValueType.Path)
                convertedFlagValues[key] = ToWslPath(value);
            else if (flag?.ValueType is FlagValueType.PathList or FlagValueType.ScaledPathList)
                convertedFlagValues[key] = ToWslPathList(value, flag.ValueType == FlagValueType.ScaledPathList);
        }
        return request with { FlagValues = convertedFlagValues.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase) };
    }

    private static string WslDirectoryName(string path)
    {
        var normalized = (path ?? "").Replace('\\', '/').TrimEnd('/');
        var split = normalized.LastIndexOf('/');
        return split <= 0 ? "" : normalized[..split];
    }

    private static string WslSiblingDirectory(string path, string sibling)
    {
        var parent = WslDirectoryName(path);
        return string.IsNullOrWhiteSpace(parent) ? sibling : $"{parent.TrimEnd('/')}/{sibling}";
    }

    private static string BashQuote(string value)
    {
        var safe = value ?? "";
        if (safe.IndexOf('\0') >= 0)
            throw new ArgumentException("Shell arguments cannot contain null bytes.");
        return "'" + safe.Replace("'", "'\"'\"'") + "'";
    }
}
