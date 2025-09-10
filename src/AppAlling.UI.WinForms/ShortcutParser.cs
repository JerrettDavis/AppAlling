namespace AppAlling.UI.WinForms;

internal static class ShortcutParser
{
    public static Keys? Parse(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        try
        {
            // "Ctrl+Shift+T" -> Keys.Control | Keys.Shift | Keys.T
            var parts = s.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var k = parts
                .Select(p => p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ? "Control" : p)
                .Aggregate(Keys.None, (current, token) => current | (Enum.TryParse<Keys>(token, true, out var key) ? key : Keys.None));
            return k == Keys.None ? null : k;
        }
        catch
        {
            return null;
        }
    }
}