using System.Runtime.InteropServices;
using System.Windows.Forms;
using QirimType.Configuration;
using QirimType.Keyboard;
using QirimType.Utils;

namespace QirimType.Tests;

[TestClass]
public sealed class QirimTypeTests
{
    [TestMethod]
    public void Test_DefaultMappings_SymbolsAndKeys()
    {
        var defaults = AppSettings.GetDefaultMappings();
        Assert.AreEqual(7, defaults.Count, "Must have exactly 7 default mappings");

        var map = defaults.ToDictionary(d => d.Key, d => d.Symbol);

        Assert.IsTrue(map.ContainsKey(Keys.G), "Must contain Alt+G");
        Assert.AreEqual("ğ", map[Keys.G]);
        Assert.AreEqual((int)'\u011F', (int)map[Keys.G][0], "ğ Unicode code point must be U+011F");

        Assert.IsTrue(map.ContainsKey(Keys.I), "Must contain Alt+I");
        Assert.AreEqual("ı", map[Keys.I]);
        Assert.AreEqual((int)'\u0131', (int)map[Keys.I][0], "ı Unicode code point must be U+0131");

        Assert.IsTrue(map.ContainsKey(Keys.N), "Must contain Alt+N");
        Assert.AreEqual("ñ", map[Keys.N]);
        Assert.AreEqual((int)'\u00F1', (int)map[Keys.N][0], "ñ Unicode code point must be U+00F1");

        Assert.IsTrue(map.ContainsKey(Keys.O), "Must contain Alt+O");
        Assert.AreEqual("ö", map[Keys.O]);
        Assert.AreEqual((int)'\u00F6', (int)map[Keys.O][0], "ö Unicode code point must be U+00F6");

        Assert.IsTrue(map.ContainsKey(Keys.U), "Must contain Alt+U");
        Assert.AreEqual("ü", map[Keys.U]);
        Assert.AreEqual((int)'\u00FC', (int)map[Keys.U][0], "ü Unicode code point must be U+00FC");

        Assert.IsTrue(map.ContainsKey(Keys.C), "Must contain Alt+C");
        Assert.AreEqual("ç", map[Keys.C]);
        Assert.AreEqual((int)'\u00E7', (int)map[Keys.C][0], "ç Unicode code point must be U+00E7");

        Assert.IsTrue(map.ContainsKey(Keys.S), "Must contain Alt+S");
        Assert.AreEqual("ş", map[Keys.S]);
        Assert.AreEqual((int)'\u015F', (int)map[Keys.S][0], "ş Unicode code point must be U+015F");
    }

    [TestMethod]
    public void Test_OnlyLowerCaseCharacters()
    {
        var defaults = AppSettings.GetDefaultMappings();
        foreach (var mapping in defaults)
        {
            Assert.AreEqual(1, mapping.Symbol.Length, "Must be single characters");
            char c = mapping.Symbol[0];
            Assert.IsTrue(char.IsLower(c), $"Character {mapping.Symbol} must be lowercase");
        }
    }

    [TestMethod]
    public void Test_InputStructSize_64Bit()
    {
        int inputSize = Marshal.SizeOf<NativeMethods.INPUT>();
        int pointerSize = IntPtr.Size;

        if (pointerSize == 8)
        {
            // 64-bit Windows: INPUT structure size must be 40 bytes
            Assert.AreEqual(40, inputSize, "NativeMethods.INPUT size must be 40 bytes on 64-bit Windows");
        }
        else
        {
            // 32-bit Windows: INPUT structure size must be 28 bytes
            Assert.AreEqual(28, inputSize, "NativeMethods.INPUT size must be 28 bytes on 32-bit Windows");
        }
    }

    [TestMethod]
    public void Test_HotkeyManager_Disabled_PassesThrough()
    {
        var settingsManager = new SettingsManager();
        settingsManager.SetEnabled(false);

        var hotkeyManager = new HotkeyManager(settingsManager);

        var kbd = new NativeMethods.KBDLLHOOKSTRUCT
        {
            vkCode = (uint)Keys.G,
            flags = NativeMethods.LLKHF_ALTDOWN
        };

        // When disabled, even Alt+G should not be suppressed
        bool handled = hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_SYSKEYDOWN, kbd);
        Assert.IsFalse(handled, "When disabled, Alt+G must not be intercepted");
    }

    [TestMethod]
    public void Test_HotkeyManager_Enabled_Intercepts_AltG()
    {
        var settingsManager = new SettingsManager();
        settingsManager.SetEnabled(true);

        var hotkeyManager = new HotkeyManager(settingsManager);

        var kbd = new NativeMethods.KBDLLHOOKSTRUCT
        {
            vkCode = (uint)Keys.G,
            flags = NativeMethods.LLKHF_ALTDOWN
        };

        bool handled = hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_SYSKEYDOWN, kbd);
        Assert.IsTrue(handled, "Alt+G must be intercepted when enabled");

        // KeyUp for G should also be suppressed
        bool keyUpHandled = hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_SYSKEYUP, kbd);
        Assert.IsTrue(keyUpHandled, "KeyUp for intercepted G must be suppressed");

        // KeyUp for Alt should be suppressed because a symbol was typed
        var altKbd = new NativeMethods.KBDLLHOOKSTRUCT
        {
            vkCode = (uint)NativeMethods.VK_LMENU
        };
        bool altUpHandled = hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_SYSKEYUP, altKbd);
        Assert.IsTrue(altUpHandled, "KeyUp for Alt should be suppressed after typing a symbol to avoid menu activation");

        // Second KeyUp for Alt (if any) should not be suppressed
        bool secondAltUp = hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_SYSKEYUP, altKbd);
        Assert.IsFalse(secondAltUp, "Subsequent Alt release should pass through normally");
    }

    [TestMethod]
    public void Test_HotkeyManager_Ignores_NonMapped_AltKeys()
    {
        var settingsManager = new SettingsManager();
        settingsManager.SetEnabled(true);
        var hotkeyManager = new HotkeyManager(settingsManager);

        // Alt + F4
        var kbdF4 = new NativeMethods.KBDLLHOOKSTRUCT
        {
            vkCode = (uint)Keys.F4,
            flags = NativeMethods.LLKHF_ALTDOWN
        };
        Assert.IsFalse(hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_SYSKEYDOWN, kbdF4), "Alt+F4 must not be intercepted");

        // Alt + Tab
        var kbdTab = new NativeMethods.KBDLLHOOKSTRUCT
        {
            vkCode = (uint)Keys.Tab,
            flags = NativeMethods.LLKHF_ALTDOWN
        };
        Assert.IsFalse(hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_SYSKEYDOWN, kbdTab), "Alt+Tab must not be intercepted");
    }

    [TestMethod]
    public void Test_HotkeyManager_Ignores_When_Alt_Not_Pressed()
    {
        var settingsManager = new SettingsManager();
        settingsManager.SetEnabled(true);
        var hotkeyManager = new HotkeyManager(settingsManager);

        // Just 'G' without Alt
        var kbdG = new NativeMethods.KBDLLHOOKSTRUCT
        {
            vkCode = (uint)Keys.G,
            flags = 0 // No Alt down
        };

        // When Alt is not pressed, WM_KEYDOWN G should pass through
        Assert.IsFalse(hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_KEYDOWN, kbdG), "Plain 'G' without Alt must not be intercepted");
    }

    [TestMethod]
    public void Test_AppSettings_Clone_And_Defaults()
    {
        var appSettings = new AppSettings();
        var clone = appSettings.Clone();

        Assert.AreEqual(appSettings.IsEnabled, clone.IsEnabled);
        Assert.AreEqual(appSettings.StartWithWindows, clone.StartWithWindows);
        Assert.AreEqual(appSettings.Mappings.Count, clone.Mappings.Count);

        // Mutating clone should not mutate original
        clone.Mappings[0].Key = Keys.Z;
        Assert.AreNotEqual(appSettings.Mappings[0].Key, clone.Mappings[0].Key);
    }

    [TestMethod]
    public void Test_AllSevenHotkeys_AreIntercepted()
    {
        var settingsManager = new SettingsManager();
        settingsManager.SetEnabled(true);
        var hotkeyManager = new HotkeyManager(settingsManager);

        var expectedKeys = new[] { Keys.G, Keys.I, Keys.N, Keys.O, Keys.U, Keys.C, Keys.S };

        foreach (var key in expectedKeys)
        {
            var kbd = new NativeMethods.KBDLLHOOKSTRUCT
            {
                vkCode = (uint)key,
                flags = NativeMethods.LLKHF_ALTDOWN
            };

            bool handled = hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_SYSKEYDOWN, kbd);
            Assert.IsTrue(handled, $"Alt+{key} must be intercepted");

            bool keyUpHandled = hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_SYSKEYUP, kbd);
            Assert.IsTrue(keyUpHandled, $"KeyUp for Alt+{key} must be suppressed");
        }
    }

    [TestMethod]
    public void Test_CustomKeyRemapping()
    {
        var settingsManager = new SettingsManager();
        settingsManager.SetEnabled(true);

        var settings = settingsManager.Settings.Clone();
        var gMapping = settings.Mappings.First(m => m.Symbol == "ğ");
        gMapping.Key = Keys.K; // Remap 'ğ' to Alt+K
        settingsManager.SaveSettings(settings);

        var hotkeyManager = new HotkeyManager(settingsManager);

        // Old key Alt+G should no longer be intercepted
        var kbdG = new NativeMethods.KBDLLHOOKSTRUCT
        {
            vkCode = (uint)Keys.G,
            flags = NativeMethods.LLKHF_ALTDOWN
        };
        Assert.IsFalse(hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_SYSKEYDOWN, kbdG), "Old Alt+G must not be intercepted after remapping");

        // New key Alt+K should be intercepted
        var kbdK = new NativeMethods.KBDLLHOOKSTRUCT
        {
            vkCode = (uint)Keys.K,
            flags = NativeMethods.LLKHF_ALTDOWN
        };
        Assert.IsTrue(hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_SYSKEYDOWN, kbdK), "New Alt+K must be intercepted after remapping");

        // Restore defaults
        settingsManager.ResetToDefaults();
        Assert.AreEqual(Keys.G, settingsManager.Settings.Mappings.First(m => m.Symbol == "ğ").Key, "Defaults must restore Alt+G for ğ");
    }

    [TestMethod]
    public void Test_GenerateAppIcon()
    {
        string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\"));
        string icoPath = Path.Combine(projectDir, "app.ico");

        IconHelper.SaveMultiResolutionIcon(icoPath, new[] { 16, 32, 48, 256 });
        Assert.IsTrue(File.Exists(icoPath), "app.ico should be generated");

        var fi = new FileInfo(icoPath);
        Assert.IsTrue(fi.Length > 1000, "app.ico should have non-zero size");
    }

    [TestMethod]
    public void Test_HoldingAltDown_MultiplePresses_AreAllIntercepted()
    {
        var settingsManager = new SettingsManager();
        settingsManager.SetEnabled(true);
        var hotkeyManager = new HotkeyManager(settingsManager);

        // 1. User presses Alt down
        var altDown = new NativeMethods.KBDLLHOOKSTRUCT { vkCode = (uint)NativeMethods.VK_LMENU };
        bool altDownResult = hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_SYSKEYDOWN, altDown);
        Assert.IsFalse(altDownResult, "Alt down itself should pass through");

        // 2. User presses G first time (with LLKHF_ALTDOWN)
        var kbdG1 = new NativeMethods.KBDLLHOOKSTRUCT
        {
            vkCode = (uint)Keys.G,
            flags = NativeMethods.LLKHF_ALTDOWN
        };
        Assert.IsTrue(hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_SYSKEYDOWN, kbdG1), "First G press must be intercepted");
        Assert.IsTrue(hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_SYSKEYUP, kbdG1), "First G keyup must be suppressed");

        // 3. User presses G second time (Alt is still physically down, but flags might have 0 because SendInput logically released Alt)
        var kbdG2 = new NativeMethods.KBDLLHOOKSTRUCT
        {
            vkCode = (uint)Keys.G,
            flags = 0 // Context flag might be 0 after SendInput released Alt
        };
        Assert.IsTrue(hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_KEYDOWN, kbdG2), "Second G press while Alt is held MUST be intercepted");
        Assert.IsTrue(hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_KEYUP, kbdG2), "Second G keyup must be suppressed");

        // 4. User presses G third time
        var kbdG3 = new NativeMethods.KBDLLHOOKSTRUCT
        {
            vkCode = (uint)Keys.G,
            flags = 0
        };
        Assert.IsTrue(hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_KEYDOWN, kbdG3), "Third G press while Alt is held MUST be intercepted");
        Assert.IsTrue(hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_KEYUP, kbdG3), "Third G keyup must be suppressed");

        // 5. User presses O (still holding Alt)
        var kbdO = new NativeMethods.KBDLLHOOKSTRUCT
        {
            vkCode = (uint)Keys.O,
            flags = 0
        };
        Assert.IsTrue(hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_KEYDOWN, kbdO), "O press while Alt is held MUST be intercepted");
        Assert.IsTrue(hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_KEYUP, kbdO), "O keyup must be suppressed");

        // 6. User finally releases physical Alt
        var altUp = new NativeMethods.KBDLLHOOKSTRUCT { vkCode = (uint)NativeMethods.VK_LMENU };
        Assert.IsTrue(hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_SYSKEYUP, altUp), "Alt release after typing symbols must be suppressed");

        // 7. Now that Alt is released, pressing G should NOT be intercepted!
        var kbdGAfterAltReleased = new NativeMethods.KBDLLHOOKSTRUCT
        {
            vkCode = (uint)Keys.G,
            flags = 0
        };
        Assert.IsFalse(hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_KEYDOWN, kbdGAfterAltReleased), "G press after Alt is released must pass through as normal G");
    }
}
