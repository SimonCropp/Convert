namespace Convert;

/// <summary>
/// Builds a pre-filled GitHub "new issue" URL for an unexpected exception, surfaced as a link the user
/// can choose to follow.
/// </summary>
public static class IssueLauncher
{
    const string newIssue = "https://github.com/SimonCropp/Convert/issues/new";

    public static string ForException(string action, Exception exception, string? environment = null)
    {
        var title = WebUtility.UrlEncode($"{action}: {exception.GetType().Name}");

        List<string> lines =
        [
            "An unexpected error occurred in Convert.",
            "",
            $"* Action: {action}",
        ];

        if (!string.IsNullOrWhiteSpace(environment))
        {
            lines.Add(environment);
        }

        lines.Add("* Exception:");
        lines.Add("```");
        lines.Add(exception.ToString());
        lines.Add("```");

        var body = WebUtility.UrlEncode(string.Join('\n', lines));
        return $"{newIssue}?title={title}&body={body}";
    }

    /// <summary>Markdown bullet lines describing the runtime, for an issue body.</summary>
    public static string Environment(string? userAgent) =>
        string.Join(
            '\n',
            $"* Convert version: {AppInfo.Version}",
            $"* User agent: {userAgent}");
}

/// <summary>Build facts for the footer and bug reports.</summary>
public static class AppInfo
{
    public static string Version { get; } = ShortenSha(
        typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ??
        typeof(AppInfo).Assembly.GetName().Version?.ToString() ??
        "unknown");

    static string ShortenSha(string version)
    {
        var plus = version.IndexOf('+');
        if (plus < 0 || version.Length - plus - 1 <= 7)
        {
            return version;
        }

        return version[..(plus + 8)];
    }
}
