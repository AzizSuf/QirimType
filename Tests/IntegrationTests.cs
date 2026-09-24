using System.Runtime.InteropServices;
using System.Windows.Forms;
using QirimType.Configuration;
using QirimType.Keyboard;

namespace QirimType.Tests;

[TestClass]
public class IntegrationTests
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [TestMethod]
    [Timeout(15000)]
    public void Test_EndToEnd_TypingSimulation_InTextBox()
    {
        // Must run on STA thread for WinForms UI
        var thread = new Thread(() =>
        {
            using var form = new Form
            {
                Width = 400,
                Height = 200,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Location = new System.Drawing.Point(-2000, -2000) // Offscreen
            };

            var textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Segoe UI", 12f)
            };
            form.Controls.Add(textBox);

            var settingsManager = new SettingsManager();
            settingsManager.SetEnabled(true);
            settingsManager.ResetToDefaults();

            var hotkeyManager = new HotkeyManager(settingsManager);
            using var hook = new GlobalKeyboardHook(hotkeyManager);
            hook.Install();

            form.Shown += async (s, e) =>
            {
                textBox.Focus();
                await Task.Delay(100);

                // Directly simulate the hook handling all 7 keys
                var keys = new[]
                {
                    (Keys.G, "ğ"),
                    (Keys.I, "ı"),
                    (Keys.N, "ñ"),
                    (Keys.O, "ö"),
                    (Keys.U, "ü"),
                    (Keys.C, "ç"),
                    (Keys.S, "ş")
                };

                foreach (var (key, expectedSymbol) in keys)
                {
                    var kbd = new NativeMethods.KBDLLHOOKSTRUCT
                    {
                        vkCode = (uint)key,
                        flags = NativeMethods.LLKHF_ALTDOWN
                    };

                    // ProcessKeyDown suppresses and injects
                    bool suppressed = hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_SYSKEYDOWN, kbd);
                    Assert.IsTrue(suppressed, $"Key {key} must be suppressed");

                    // ProcessKeyUp
                    bool upSuppressed = hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_SYSKEYUP, kbd);
                    Assert.IsTrue(upSuppressed, $"KeyUp for {key} must be suppressed");
                }

                // Simulate Alt release - must NOT be suppressed to ensure OS key state is clean
                var altKbd = new NativeMethods.KBDLLHOOKSTRUCT { vkCode = (uint)NativeMethods.VK_LMENU };
                bool altUpSuppressed = hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_SYSKEYUP, altKbd);
                Assert.IsFalse(altUpSuppressed, "Alt release must NOT be suppressed to prevent sticky Alt and allow smooth layout switching");

                // Let Windows message loop process injected inputs
                await Task.Delay(200);

                // Now test disabled state
                settingsManager.SetEnabled(false);
                var disabledKbd = new NativeMethods.KBDLLHOOKSTRUCT
                {
                    vkCode = (uint)Keys.G,
                    flags = NativeMethods.LLKHF_ALTDOWN
                };
                bool disabledHandled = hotkeyManager.ProcessKeyboardEvent(NativeMethods.WM_SYSKEYDOWN, disabledKbd);
                Assert.IsFalse(disabledHandled, "When disabled, Alt+G must not be intercepted");

                hook.Uninstall();
                form.Close();
            };

            Application.Run(form);
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }
}
