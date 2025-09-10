namespace AppAlling.Abstractions;

/// <summary>
/// Allows a plugin to declare tool windows it contributes to the host shell.
/// The host will create implicit "Open {Title}" commands that show each tool window.
/// </summary>
public interface IToolWindowContribution
{
    /// <summary>
    /// Describes the tool windows provided by this plugin.
    /// </summary>
    IEnumerable<ToolWindowDescriptor> DescribeToolWindows();
}