namespace AppAlling.UI.WinForms;

/// <summary>
/// Strongly-typed application settings bound from configuration.
/// </summary>
public record AppSettings
{
    /// <summary>
    /// Plugin-related settings.
    /// </summary>
    public required PluginSettings Plugins { get; init; }
}

/// <summary>
/// Settings for locating plugins on disk.
/// </summary>
public record PluginSettings
{
    /// <summary>
    /// Relative or absolute directory path where plugin DLLs are discovered.
    /// </summary>
    public string Directory { get; init; } = "";
}