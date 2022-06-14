using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace MapAssist.Helpers
{
    public class Hotkey
    {
        private string _hotkeyString;
        private Keys _hotkey;

        public Hotkey(string hotkeyString = "None")
        {
            _hotkeyString = hotkeyString;
            _hotkey = hotkeyString != "" ? ParseKeys(hotkeyString) : Keys.None;
        }

        public Hotkey(Keys modifiers, Keys key)
        {
            if (key == Keys.Menu || key == Keys.ShiftKey || key == Keys.ControlKey)
            {
                _hotkey = modifiers;
            }
            else
            {
                _hotkey = modifiers | key;
            }
        }

        public void Monitor(Control control)
        {
            control.KeyDown += OnKeyDown;
            control.KeyPress += (sender, e) => { e.Handled = true; };
            control.KeyUp += (sender, e) => { e.Handled = true; };

            control.Text = _hotkeyString;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            var control = (Control)sender;

            if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
            {
                _hotkey = Keys.None;
                control.Text = "None";
                return;
            }

            if (e.KeyCode == Keys.Menu || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.ControlKey)
            {
                control.Text = e.Modifiers.ToString().Replace(", ", " + ").Replace("Control", "Ctrl");
            }
            else if (e.Modifiers == Keys.None)
            {
                control.Text = FormatKey(e.KeyCode);
            }
            else
            {
                _hotkey = e.Modifiers | e.KeyCode;

                control.Text = e.Modifiers.ToString().Replace(", ", " + ").Replace("Control", "Ctrl") + " + " + FormatKey(e.KeyCode);
            }

            e.Handled = true;
        }

        public override int GetHashCode()
        {
            return _hotkey.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Hotkey)) return false;

            var other = obj as Hotkey;
            return _hotkey == other._hotkey;
        }

        private string FormatKey(Keys key)
        {
            if (textLookup.TryGetValue(key, out var keyString))
            {
                return keyString;
            }

            return key.ToString();
        }

        private static Keys ParseKeys(string keysString)
        {
            var keys = Keys.None;

            foreach (var keyString in keysString.Split(new string[] { " + " }, StringSplitOptions.None))
            {
                if (keyLookup.TryGetValue(keyString, out var key1))
                {
                    keys |= key1;
                }
                else if (Enum.TryParse(keyString, out Keys key2))
                {
                    keys |= key2;
                }
            }

            return keys;
        }

        private static Dictionary<Keys, string> textLookup = new Dictionary<Keys, string>()
        {
            { Keys.Control, "Ctrl" },

            { Keys.NumPad0, "Num 0" },
            { Keys.NumPad1, "Num 1" },
            { Keys.NumPad2, "Num 2" },
            { Keys.NumPad3, "Num 3" },
            { Keys.NumPad4, "Num 4" },
            { Keys.NumPad5, "Num 5" },
            { Keys.NumPad6, "Num 6" },
            { Keys.NumPad7, "Num 7" },
            { Keys.NumPad8, "Num 8" },
            { Keys.NumPad9, "Num 9" },
            { Keys.Add, "Num +" },
            { Keys.Subtract, "Num -" },
            { Keys.Multiply, "Num *" },
            { Keys.Divide, "Num /" },
            { Keys.Decimal, "Num ." },

            { Keys.D0, "0" },
            { Keys.D1, "1" },
            { Keys.D2, "2" },
            { Keys.D3, "3" },
            { Keys.D4, "4" },
            { Keys.D5, "5" },
            { Keys.D6, "6" },
            { Keys.D7, "7" },
            { Keys.D8, "8" },
            { Keys.D9, "9" },

            { Keys.OemBackslash, "\\" },
            { Keys.OemCloseBrackets, "]" },
            { Keys.Oemcomma, "," },
            { Keys.OemMinus, "-" },
            { Keys.OemOpenBrackets, "[" },
            { Keys.OemPeriod, "." },
            { Keys.OemPipe, "|" },
            { Keys.Oemplus, "+" },
            { Keys.OemQuestion, "?" },
            { Keys.OemQuotes, "'" },
            { Keys.OemSemicolon, ";" },
            { Keys.Oemtilde, "`" },
        };

        private static Dictionary<string, Keys> keyLookup = textLookup.ToDictionary(x => x.Value, x => x.Key);

        public static bool operator ==(Hotkey a, Hotkey b)
        {
            if (a is null)
                return b is null;

            return a.Equals(b);
        }

        public static bool operator !=(Hotkey a, Hotkey b)
        {
            return !(a == b);
        }

        // https://stackoverflow.com/questions/736279/c-is-there-a-way-to-properly-convert-keys-oem-to-proper-string-without-doing
        public static char ToAscii(Keys key)
        {
            var outputBuilder = new StringBuilder(2);
            var result = ToAscii((uint)key, 0, new byte[256], outputBuilder, 0);
            if (result == 1)
                return outputBuilder[0];
            else
                throw new Exception("Invalid key");
        }

        [DllImport("user32.dll")]
        private static extern int ToAscii(uint uVirtKey, uint uScanCode, byte[] lpKeyState, [Out] StringBuilder lpChar, uint uFlags);
    }
}
