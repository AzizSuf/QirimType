namespace QirimType.Configuration;

public class AppSettings
{
    public bool IsEnabled { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
    public List<HotkeyMapping> Mappings { get; set; } = GetDefaultMappings();

    public static List<HotkeyMapping> GetDefaultMappings()
    {
        return new List<HotkeyMapping>
        {
            new("ğ", "ğ (Latin Small Letter G with Breve)", Keys.G, true),
            new("ı", "ı (Latin Small Letter Dotless I)", Keys.I, true),
            new("ñ", "ñ (Latin Small Letter N with Tilde)", Keys.N, true),
            new("ö", "ö (Latin Small Letter O with Diaeresis)", Keys.O, true),
            new("ü", "ü (Latin Small Letter U with Diaeresis)", Keys.U, true),
            new("ç", "ç (Latin Small Letter C with Cedilla)", Keys.C, true),
            new("ş", "ş (Latin Small Letter S with Cedilla)", Keys.S, true)
        };
    }

    public AppSettings Clone()
    {
        return new AppSettings
        {
            IsEnabled = this.IsEnabled,
            StartWithWindows = this.StartWithWindows,
            Mappings = this.Mappings.Select(m => new HotkeyMapping(m.Symbol, m.DisplayName, m.Key, m.RequireAlt)).ToList()
        };
    }
}
