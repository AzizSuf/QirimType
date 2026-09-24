namespace QirimType.Configuration;

public class HotkeyMapping
{
    public string Symbol { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Keys Key { get; set; }
    public bool RequireAlt { get; set; } = true;

    public HotkeyMapping() { }

    public HotkeyMapping(string symbol, string displayName, Keys key, bool requireAlt = true)
    {
        Symbol = symbol;
        DisplayName = displayName;
        Key = key;
        RequireAlt = requireAlt;
    }

    public string ShortcutText => RequireAlt ? $"Alt + {Key}" : Key.ToString();
}
