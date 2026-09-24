using System.Diagnostics;
using QirimType.Configuration;

namespace QirimType.Keyboard;

public class HotkeyManager
{
    private readonly SettingsManager _settingsManager;
    private readonly HashSet<uint> _suppressedKeys = new();
    private bool _altUsedForSymbol;

    public HotkeyManager(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
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
            _altUsedForSymbol = false;
            return false;
        }

        uint vkCode = kbd.vkCode;
        bool isKeyDown = (msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN);
        bool isKeyUp = (msg == NativeMethods.WM_KEYUP || msg == NativeMethods.WM_SYSKEYUP);

        // 1. Handle KeyUp for previously intercepted symbol keys
        if (isKeyUp)
        {
            if (_suppressedKeys.Contains(vkCode))
            {
                _suppressedKeys.Remove(vkCode);
                return true; // Suppress the release of the intercepted key
            }

            // If user releases the Alt key after having typed a symbol with it,
            // suppress the Alt keyup to prevent the active window from focusing its menu bar.
            if (vkCode == NativeMethods.VK_MENU ||
                vkCode == NativeMethods.VK_LMENU ||
                vkCode == NativeMethods.VK_RMENU)
            {
                if (_altUsedForSymbol)
                {
                    _altUsedForSymbol = false;
                    return true; // Suppress Alt release
                }
            }

            return false;
        }

        // 2. Handle KeyDown
        if (isKeyDown)
        {
            // Strict modifier check: Alt MUST be down, but Ctrl, Shift, and Win MUST NOT be down
            bool isAltDown = (kbd.flags & NativeMethods.LLKHF_ALTDOWN) != 0 ||
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

            // Check if vkCode matches any of our mappings
            var currentMappings = _settingsManager.Settings.Mappings;
            var match = currentMappings.FirstOrDefault(m => (uint)m.Key == vkCode && m.RequireAlt);

            if (match != null && !string.IsNullOrEmpty(match.Symbol))
            {
                _suppressedKeys.Add(vkCode);
                _altUsedForSymbol = true;

                // Inject the Unicode character into foreground window
                UnicodeInput.SendUnicodeString(match.Symbol);

                // Suppress the original keystroke
                return true;
            }
        }

        return false;
    }
}
