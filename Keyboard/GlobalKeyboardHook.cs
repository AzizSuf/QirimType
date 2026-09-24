using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace QirimType.Keyboard;

public class GlobalKeyboardHook : IDisposable
{
    private readonly HotkeyManager _hotkeyManager;
    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private bool _disposed;

    public bool IsHooked => _hookId != IntPtr.Zero;

    public GlobalKeyboardHook(HotkeyManager hotkeyManager)
    {
        _hotkeyManager = hotkeyManager;
        // Keep reference to delegate to prevent garbage collection
        _proc = HookCallback;
    }

    public void Install()
    {
        if (_hookId != IntPtr.Zero)
            return;

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;

        IntPtr hMod = IntPtr.Zero;
        if (curModule != null)
        {
            hMod = NativeMethods.GetModuleHandle(curModule.ModuleName);
        }

        _hookId = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL,
            _proc,
            hMod,
            0);

        if (_hookId == IntPtr.Zero)
        {
            int errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(errorCode, $"Failed to install WH_KEYBOARD_LL hook. Error code: {errorCode}");
        }

        Debug.WriteLine($"[QirimType] Global keyboard hook installed successfully (ID: {_hookId})");
    }

    public void Uninstall()
    {
        if (_hookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            Debug.WriteLine("[QirimType] Global keyboard hook uninstalled.");
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var kbd = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

            // Skip processing for our own injected input or system-injected inputs to prevent recursion
            if (kbd.dwExtraInfo == NativeMethods.QRMT_EXTRA_INFO || (kbd.flags & NativeMethods.LLKHF_INJECTED) != 0)
            {
                return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            int msg = wParam.ToInt32();
            bool suppress = _hotkeyManager.ProcessKeyboardEvent(msg, kbd);

            if (suppress)
            {
                return (IntPtr)1; // Block the keystroke from reaching the active application
            }
        }

        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            Uninstall();
            _disposed = true;
        }
    }

    ~GlobalKeyboardHook()
    {
        Dispose(false);
    }
}
