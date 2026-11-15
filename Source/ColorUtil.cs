using System;
using System.Globalization;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ZKO;

public static class ColorUtil {
    public struct Preset {
        public string Name;
        public string Hex;
        public Preset(string name, string hex) { Name = name; Hex = hex; }
    }

    // Index 0 is reserved for "Custom" in the menu.
    public static readonly Preset[] Presets = {
        new("White", "FFFFFF"),
        new("Black", "000000"),
        new("Grey", "808080"),
        new("Olive", "A8B03A"),
        new("Yellow", "FFD800"),
        new("Orange", "FF8A00"),
        new("Red", "E0303A"),
        new("Pink", "FF6FB5"),
        new("Purple", "9B5DE5"),
        new("Blue", "3A7BE0"),
        new("Cyan", "3AD7E0"),
        new("Green", "3AE07A"),
        new("Madeline", "AC3232"),
        new("Badeline", "9B3FB5"),
    };

    public static Color Parse(string hex, Color fallback) {
        if (string.IsNullOrWhiteSpace(hex))
            return fallback;
        hex = hex.Trim().TrimStart('#');
        if (hex.Length != 6 || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int v))
            return fallback;
        return new Color((v >> 16) & 0xFF, (v >> 8) & 0xFF, v & 0xFF);
    }

    public static string ToHex(int r, int g, int b)
        => $"{Math.Clamp(r, 0, 255):X2}{Math.Clamp(g, 0, 255):X2}{Math.Clamp(b, 0, 255):X2}";

    /// <summary>Returns the preset index (0-based in Presets) matching the colour exactly, or -1.</summary>
    public static int FindPreset(string hex) {
        string norm = (hex ?? "").Trim().TrimStart('#').ToUpperInvariant();
        for (int i = 0; i < Presets.Length; i++)
            if (Presets[i].Hex == norm)
                return i;
        return -1;
    }
}
