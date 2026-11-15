using System;
using Monocle;

namespace Celeste.Mod.ZKO;

public class ZKOModule : EverestModule {
    public static ZKOModule Instance { get; private set; }

    public override Type SettingsType => typeof(ZKOModuleSettings);
    public static ZKOModuleSettings Settings => (ZKOModuleSettings) Instance._Settings;

    public ZKOModule() {
        Instance = this;
#if DEBUG
        Logger.SetLogLevel(nameof(ZKOModule), LogLevel.Verbose);
#else
        Logger.SetLogLevel(nameof(ZKOModule), LogLevel.Info);
#endif
    }

    public override void Load() {
        On.Monocle.Engine.RenderCore += Engine_RenderCore;
        On.Monocle.Engine.Update += Engine_Update;
    }

    public override void Unload() {
        On.Monocle.Engine.RenderCore -= Engine_RenderCore;
        On.Monocle.Engine.Update -= Engine_Update;
    }

    private static void Engine_Update(On.Monocle.Engine.orig_Update orig, Engine self, Microsoft.Xna.Framework.GameTime gameTime) {
        orig(self, gameTime);
        ZKOModuleSettings settings = Settings;
        if (settings?.ToggleOverlay != null && settings.ToggleOverlay.Pressed) {
            settings.Show = !settings.Show;
            Instance.SaveSettings();
        }
    }

    private static void Engine_RenderCore(On.Monocle.Engine.orig_RenderCore orig, Engine self) {
        orig(self);
        try {
            OverlayRenderer.Render();
        } catch (Exception e) {
            // Never take the game down because of the overlay.
            OverlayRenderer.ReportError(e);
        }
    }
}
