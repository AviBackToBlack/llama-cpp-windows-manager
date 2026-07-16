
namespace LocalLlmConsole.Services;

public sealed partial class LlamaProcessSupervisor : IDisposable
{
    private static string ToWslPath(string value)
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

    private static RuntimeLaunchRequest ConvertFlagValuesForWsl(RuntimeLaunchRequest request)
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
