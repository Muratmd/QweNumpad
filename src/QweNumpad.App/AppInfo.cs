using System.Reflection;

namespace QweNumpad.App;

internal static class AppInfo
{
    public const string GitHubUrl = "https://github.com/Muratmd/QweNumpad";

    public const string NewIssueUrl = GitHubUrl + "/issues/new/choose";

    /// <summary>"1.0.0", from the Version in Directory.Build.props.</summary>
    public static string Version { get; } =
        typeof(AppInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(AppInfo).Assembly.GetName().Version?.ToString(3)
        ?? "?";
}
