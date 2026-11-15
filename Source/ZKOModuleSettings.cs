using System;
using System.Collections.Generic;

namespace Celeste.Mod.ZKO;

public enum ShowInMode { Gameplay, GameplayPause, Everywhere }
public enum OverlayPosition { TopLeft, BottomLeft, TopRight, BottomRight, Custom }

/// <summary>
/// ZKO settings. Menu entries appear in declaration order. Everest calls Create{Property}Entry
/// for each property that has one; the toggle hotkey (ButtonBinding) is added by Everest at the end.
/// </summary>
public class ZKOModuleSettings : EverestModuleSettings {

    // ---- Visibility -------------------------------------------------------------------------

    [SettingName("MODOPTIONS_ZKO_SHOW")]
    public bool Show { get; set; } = true;

    public ShowInMode ShowIn { get; set; } = ShowInMode.GameplayPause;

    public void CreateShowInEntry(TextMenu menu, bool inGame) {
        menu.Add(EnumOption("MODOPTIONS_ZKO_SHOWIN", ShowIn, v => ShowIn = v,
            (ShowInMode.Gameplay, "MODOPTIONS_ZKO_SHOWIN_GAMEPLAY"),
            (ShowInMode.GameplayPause, "MODOPTIONS_ZKO_SHOWIN_GAMEPLAYPAUSE"),
            (ShowInMode.Everywhere, "MODOPTIONS_ZKO_SHOWIN_EVERYWHERE")));
    }

    // ---- Position & size --------------------------------------------------------------------

    public OverlayPosition Position { get; set; } = OverlayPosition.BottomLeft;

    public void CreatePositionEntry(TextMenu menu, bool inGame) {
        menu.Add(EnumOption("MODOPTIONS_ZKO_POSITION", Position, v => Position = v,
            (OverlayPosition.TopLeft, "MODOPTIONS_ZKO_POSITION_TOPLEFT"),
            (OverlayPosition.BottomLeft, "MODOPTIONS_ZKO_POSITION_BOTTOMLEFT"),
            (OverlayPosition.TopRight, "MODOPTIONS_ZKO_POSITION_TOPRIGHT"),
            (OverlayPosition.BottomRight, "MODOPTIONS_ZKO_POSITION_BOTTOMRIGHT"),
            (OverlayPosition.Custom, "MODOPTIONS_ZKO_POSITION_CUSTOM")));
    }

    /// <summary>Horizontal edge padding, percent of screen width.</summary>
    public int PaddingX { get; set; } = 2;
    public void CreatePaddingXEntry(TextMenu menu, bool inGame)
        => menu.Add(PercentSlider("MODOPTIONS_ZKO_PADDINGX", 0, 25, 1, PaddingX, v => PaddingX = v));

    /// <summary>Vertical edge padding, percent of screen height ("padded up 5%").</summary>
    public int PaddingY { get; set; } = 5;
    public void CreatePaddingYEntry(TextMenu menu, bool inGame)
        => menu.Add(PercentSlider("MODOPTIONS_ZKO_PADDINGY", 0, 25, 1, PaddingY, v => PaddingY = v));

    /// <summary>Custom position, percent: 0 = left edge, 100 = right edge.</summary>
    public int CustomX { get; set; } = 2;
    public void CreateCustomXEntry(TextMenu menu, bool inGame) {
        TextMenu.Item item = PercentSlider("MODOPTIONS_ZKO_CUSTOMX", 0, 100, 1, CustomX, v => CustomX = v);
        menu.Add(item);
        item.AddDescription(menu, Dialog.Clean("MODOPTIONS_ZKO_CUSTOM_HINT"));
    }

    /// <summary>Custom position, percent: 0 = top edge, 100 = bottom edge.</summary>
    public int CustomY { get; set; } = 95;
    public void CreateCustomYEntry(TextMenu menu, bool inGame)
        => menu.Add(PercentSlider("MODOPTIONS_ZKO_CUSTOMY", 0, 100, 1, CustomY, v => CustomY = v));

    /// <summary>Overlay scale in percent.</summary>
    public int Scale { get; set; } = 100;
    public void CreateScaleEntry(TextMenu menu, bool inGame)
        => menu.Add(PercentSlider("MODOPTIONS_ZKO_SCALE", 30, 300, 10, Scale, v => Scale = v));

    /// <summary>Opacity in percent.</summary>
    public int Opacity { get; set; } = 100;
    public void CreateOpacityEntry(TextMenu menu, bool inGame)
        => menu.Add(PercentSlider("MODOPTIONS_ZKO_OPACITY", 10, 100, 10, Opacity, v => Opacity = v));

    // ---- Colours (hex RRGGBB, editable in the settings file too) ------------------------------

    public string FillColor { get; set; } = "000000";
    public void CreateFillColorEntry(TextMenu menu, bool inGame)
        => AddColorEditor(menu, "MODOPTIONS_ZKO_FILLCOLOR", () => FillColor, v => FillColor = v);

    [SettingName("MODOPTIONS_ZKO_INVERTFILL")]
    public bool InvertFill { get; set; } = false;

    public string OutlineColor { get; set; } = "FFFFFF";
    public void CreateOutlineColorEntry(TextMenu menu, bool inGame)
        => AddColorEditor(menu, "MODOPTIONS_ZKO_OUTLINECOLOR", () => OutlineColor, v => OutlineColor = v);

    [SettingName("MODOPTIONS_ZKO_INVERTOUTLINE")]
    [SettingSubText("MODOPTIONS_ZKO_INVERT_HINT")]
    public bool InvertOutline { get; set; } = false;

    public string PressedColor { get; set; } = "A8B03A";
    public void CreatePressedColorEntry(TextMenu menu, bool inGame)
        => AddColorEditor(menu, "MODOPTIONS_ZKO_PRESSEDCOLOR", () => PressedColor, v => PressedColor = v);

    public string LabelColor { get; set; } = "FFFFFF";
    public void CreateLabelColorEntry(TextMenu menu, bool inGame)
        => AddColorEditor(menu, "MODOPTIONS_ZKO_LABELCOLOR", () => LabelColor, v => LabelColor = v);

    [SettingName("MODOPTIONS_ZKO_INVERTLABELS")]
    public bool InvertLabels { get; set; } = false;

    // ---- Debug -------------------------------------------------------------------------------

    [SettingName("MODOPTIONS_ZKO_DEBUGLAG")]
    [SettingSubText("MODOPTIONS_ZKO_DEBUGLAG_HINT")]
    public bool DebugLagSpikes { get; set; } = false;

    // ---- Hotkey -----------------------------------------------------------------------------

    [SettingName("MODOPTIONS_ZKO_TOGGLEOVERLAY")]
    public ButtonBinding ToggleOverlay { get; set; }

    // ---- Menu helpers -----------------------------------------------------------------------

    private static TextMenu.Option<T> EnumOption<T>(string labelKey, T current, Action<T> set, params (T value, string key)[] values) {
        TextMenu.Option<T> option = new(Dialog.Clean(labelKey));
        foreach ((T value, string key) in values)
            option.Add(Dialog.Clean(key), value, EqualityComparer<T>.Default.Equals(value, current));
        option.Change(set);
        return option;
    }

    private static TextMenu.Slider PercentSlider(string labelKey, int min, int max, int step, int current, Action<int> set) {
        int steps = (max - min) / step;
        int index = Math.Clamp((int) Math.Round((current - min) / (double) step), 0, steps);
        TextMenu.Slider slider = new(Dialog.Clean(labelKey), i => $"{min + i * step}%", 0, steps, index);
        slider.Change(i => set(min + i * step));
        return slider;
    }

    /// <summary>
    /// Collapsible colour editor: a preset picker plus exact R/G/B sliders (0 to 255) that stay
    /// in sync with each other. Hold left/right on a slider to move faster.
    /// </summary>
    private static void AddColorEditor(TextMenu menu, string labelKey, Func<string> get, Action<string> set) {
        Microsoft.Xna.Framework.Color c = ColorUtil.Parse(get(), Microsoft.Xna.Framework.Color.White);
        int[] rgb = { c.R, c.G, c.B };

        TextMenuExt.SubMenu sub = new(Dialog.Clean(labelKey), false);

        TextMenu.Option<int> preset = new(Dialog.Clean("MODOPTIONS_ZKO_PRESET"));
        int currentPreset = ColorUtil.FindPreset(get());
        preset.Add(Dialog.Clean("MODOPTIONS_ZKO_PRESET_CUSTOM"), -1, currentPreset < 0);
        for (int i = 0; i < ColorUtil.Presets.Length; i++)
            preset.Add(ColorUtil.Presets[i].Name, i, i == currentPreset);
        sub.Add(preset);

        string[] channelKeys = { "MODOPTIONS_ZKO_RED", "MODOPTIONS_ZKO_GREEN", "MODOPTIONS_ZKO_BLUE" };
        TextMenuExt.IntSlider[] sliders = new TextMenuExt.IntSlider[3];
        for (int ch = 0; ch < 3; ch++) {
            int channel = ch;
            sliders[ch] = new TextMenuExt.IntSlider(Dialog.Clean(channelKeys[ch]), 0, 255, rgb[ch]);
            sliders[ch].Change(v => {
                rgb[channel] = v;
                string hex = ColorUtil.ToHex(rgb[0], rgb[1], rgb[2]);
                set(hex);
                // Reflect the new colour in the preset picker ("Custom" unless it matches a preset).
                preset.Index = ColorUtil.FindPreset(hex) + 1;
            });
            sub.Add(sliders[ch]);
        }

        preset.Change(p => {
            if (p < 0)
                return; // "Custom": keep the current RGB
            string hex = ColorUtil.Presets[p].Hex;
            set(hex);
            Microsoft.Xna.Framework.Color pc = ColorUtil.Parse(hex, Microsoft.Xna.Framework.Color.White);
            rgb[0] = pc.R; rgb[1] = pc.G; rgb[2] = pc.B;
            for (int ch = 0; ch < 3; ch++)
                sliders[ch].Index = rgb[ch];
        });

        menu.Add(sub);
    }
}
