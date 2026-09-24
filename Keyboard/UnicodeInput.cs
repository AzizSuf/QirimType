using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace QirimType.Keyboard;

public static class UnicodeInput
{
    private static readonly int InputStructSize = Marshal.SizeOf<NativeMethods.INPUT>();

    /// <summary>
    /// Injects a Unicode string (e.g. "ğ", "ı", "ö") into the currently active window
    /// while temporarily ensuring Alt is released so the character is treated as text input.
    /// </summary>
    public static bool SendUnicodeString(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        // Check if Alt is physically or logically pressed
        bool altDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_MENU) & 0x8000) != 0;

        var inputList = new List<NativeMethods.INPUT>();

        // Step 1: If Alt is held down, release it logically before inserting the character
        if (altDown)
        {
            inputList.Add(new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_KEYBOARD,
                u = new NativeMethods.InputUnion
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = NativeMethods.VK_MENU,
                        wScan = 0,
                        dwFlags = NativeMethods.KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = NativeMethods.QRMT_EXTRA_INFO
                    }
                }
            });
        }

        // Step 2: Inject Unicode characters
        foreach (char c in text)
        {
            // Key down
            inputList.Add(new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_KEYBOARD,
                u = new NativeMethods.InputUnion
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = c,
                        dwFlags = NativeMethods.KEYEVENTF_UNICODE,
                        time = 0,
                        dwExtraInfo = NativeMethods.QRMT_EXTRA_INFO
                    }
                }
            });

            // Key up
            inputList.Add(new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_KEYBOARD,
                u = new NativeMethods.InputUnion
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = c,
                        dwFlags = NativeMethods.KEYEVENTF_UNICODE | NativeMethods.KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = NativeMethods.QRMT_EXTRA_INFO
                    }
                }
            });
        }

        NativeMethods.INPUT[] inputs = inputList.ToArray();
        uint sent = NativeMethods.SendInput((uint)inputs.Length, inputs, InputStructSize);

        if (sent != inputs.Length)
        {
            int err = Marshal.GetLastWin32Error();
            Debug.WriteLine($"SendInput failed. Expected {inputs.Length}, sent {sent}, error code: {err}");
            return false;
        }

        return true;
    }
}
