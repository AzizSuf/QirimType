using System.Diagnostics;
using QirimType.Configuration;

namespace QirimType.Keyboard;

public class HotkeyManager
{
    private readonly SettingsManager _settingsManager;
    private readonly HashSet<uint> _suppressedKeys = new();
    private readonly Func<bool> _isEnglishLayout;
    private bool _isPhysicalAltDown;

    public HotkeyManager(SettingsManager settingsManager, Func<bool>? isEnglishLayout = null)
    {
        _settingsManager = settingsManager;
        _isEnglishLayout = isEnglishLayout ?? NativeMethods.IsForegroundLayoutEnglish;
    }

    /// <summary>
    /// Processes a low-level keyboard event.
    /// Returns true if the event should be suppressed; false if it should be passed through.
    /// </summary>
    public bool ProcessKeyboardEvent(int msg, NativeMethods.KBDLLHOOKSTRUCT kbd)
    {
        // If app is disabled, do not intercept anything
        if (!_settingsManager.Settings.IsEnabled)
        {
            _suppressedKeys.Clear();
            _isPhysicalAltDown = false;
            return false;
        }

        uint vkCode = kbd.vkCode;
        bool isKeyDown = (msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN);
        bool isKeyUp = (msg == NativeMethods.WM_KEYUP || msg == NativeMethods.WM_SYSKEYUP);

        bool isAltKey = (vkCode == NativeMethods.VK_MENU ||
                         vkCode == NativeMethods.VK_LMENU ||
                         vkCode == NativeMethods.VK_RMENU);

        bool isModifierKey = isAltKey ||
                             vkCode == NativeMethods.VK_SHIFT ||
                             vkCode == NativeMethods.VK_LSHIFT ||
                             vkCode == NativeMethods.VK_RSHIFT ||
                             vkCode == NativeMethods.VK_CONTROL ||
                             vkCode == NativeMethods.VK_LCONTROL ||
                             vkCode == NativeMethods.VK_RCONTROL ||
                             vkCode == NativeMethods.VK_LWIN ||
                             vkCode == NativeMethods.VK_RWIN;

        // 1. Modifiers (Alt, Shift, Ctrl, Win) must NEVER be suppressed.
        // Passing them through ensures the OS keyboard state remains perfectly synchronized,
        // preventing stuck keys and allowing language switching (Alt+Shift, Win+Space) to work cleanly.
        if (isModifierKey)
        {
            if (isAltKey)
            {
                if (isKeyDown)
                    _isPhysicalAltDown = true;
                else if (isKeyUp)
                    _isPhysicalAltDown = false;
            }
            return false;
        }

        // 2. Handle KeyUp for previously intercepted symbol keys
        if (isKeyUp)
        {
            if (_suppressedKeys.Contains(vkCode))
            {
                _suppressedKeys.Remove(vkCode);
                return true; // Suppress the release of the intercepted key
            }
            return false;
        }

        // 3. Handle KeyDown
        if (isKeyDown)
        {
            bool isAltDown = _isPhysicalAltDown ||
                             (kbd.flags & NativeMethods.LLKHF_ALTDOWN) != 0 ||
                             (NativeMethods.GetAsyncKeyState(NativeMethods.VK_MENU) & 0x8000) != 0;

            if (!isAltDown)
                return false;

            bool isCtrlDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;
            if (isCtrlDown)
                return false;

            bool isShiftDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0;
            if (isShiftDown)
                return false; // Requirement: no Shift chords (Alt+Shift+... not used)

            bool isWinDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LWIN) & 0x8000) != 0 ||
                             (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RWIN) & 0x8000) != 0;
            if (isWinDown)
                return false;

            // Do not replace symbols in keyboard layouts other than English (e.g. Russian, etc.)
            if (_settingsManager.Settings.OnlyEnglishLayout && !_isEnglishLayout())
                return false;

            // Check if vkCode matches any of our mappings
            var currentMappings = _settingsManager.Settings.Mappings;
            var match = currentMappings.FirstOrDefault(m => (uint)m.Key == vkCode && m.RequireAlt);

            if (match != null && !string.IsNullOrEmpty(match.Symbol))
            {
                _suppressedKeys.Add(vkCode);

                // Inject the Unicode character into foreground window
                UnicodeInput.SendUnicodeString(match.Symbol);

                // Suppress the original keystroke
                return true;
            }
        }

        return false;
    }
}
