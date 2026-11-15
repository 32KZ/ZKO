using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Monocle;

namespace Celeste.Mod.ZKO;

/// <summary>Turns the keyboard key bound to a Celeste action into a short label.</summary>
public static class KeyLabels {
    private static readonly Dictionary<Keys, string> cache = new();

    private static readonly Dictionary<Keys, string> names = new() {
        [Keys.Space] = "SPC", [Keys.Enter] = "ENT", [Keys.Tab] = "TAB", [Keys.Back] = "BSP",
        [Keys.Escape] = "ESC", [Keys.CapsLock] = "CAP",
        [Keys.LeftShift] = "LSH", [Keys.RightShift] = "RSH",
        [Keys.LeftControl] = "LCT", [Keys.RightControl] = "RCT",
        [Keys.LeftAlt] = "LAL", [Keys.RightAlt] = "RAL",
        [Keys.Insert] = "INS", [Keys.Delete] = "DEL", [Keys.Home] = "HOM", [Keys.End] = "END",
        [Keys.PageUp] = "PGU", [Keys.PageDown] = "PGD",
        [Keys.OemComma] = ",", [Keys.OemPeriod] = ".", [Keys.OemQuestion] = "/",
        [Keys.OemSemicolon] = ";", [Keys.OemQuotes] = "'", [Keys.OemOpenBrackets] = "[",
        [Keys.OemCloseBrackets] = "]", [Keys.OemPipe] = "\\", [Keys.OemBackslash] = "\\",
        [Keys.OemMinus] = "-", [Keys.OemPlus] = "=", [Keys.OemTilde] = "`",
        [Keys.Add] = "+", [Keys.Subtract] = "-", [Keys.Multiply] = "*", [Keys.Divide] = "/",
        [Keys.Decimal] = ".",
    };

    public static bool IsArrow(Keys k) => k is Keys.Up or Keys.Down or Keys.Left or Keys.Right;

    /// <summary>First keyboard key of the binding, or Keys.None.</summary>
    public static Keys FirstKey(Binding binding) {
        if (binding?.Keyboard == null)
            return Keys.None;
        foreach (Keys k in binding.Keyboard)
            if (k != Keys.None)
                return k;
        return Keys.None;
    }

    public static string Label(Keys k) {
        if (cache.TryGetValue(k, out string s))
            return s;

        if (names.TryGetValue(k, out s)) {
        } else if (k >= Keys.A && k <= Keys.Z) {
            s = k.ToString();
        } else if (k >= Keys.D0 && k <= Keys.D9) {
            s = ((int) (k - Keys.D0)).ToString();
        } else if (k >= Keys.NumPad0 && k <= Keys.NumPad9) {
            s = "N" + (int) (k - Keys.NumPad0);
        } else if (k >= Keys.F1 && k <= Keys.F24) {
            s = "F" + (1 + (int) (k - Keys.F1));
        } else {
            s = k.ToString().ToUpperInvariant();
            if (s.Length > 3)
                s = s.Substring(0, 3);
        }
        cache[k] = s;
        return s;
    }
}
