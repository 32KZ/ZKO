using System;
using System.Diagnostics;
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
        bool probe = Settings?.DebugLagSpikes ?? false;
        long t0 = probe ? Stopwatch.GetTimestamp() : 0;

        orig(self, gameTime);

        long t1 = probe ? Stopwatch.GetTimestamp() : 0;
        ZKOModuleSettings settings = Settings;
        if (settings?.ToggleOverlay != null && settings.ToggleOverlay.Pressed) {
            settings.Show = !settings.Show;
            Instance.SaveSettings();
        }
        if (probe)
            LagProbe.AddUpdate(t1 - t0, Stopwatch.GetTimestamp() - t1);
    }

    private static void Engine_RenderCore(On.Monocle.Engine.orig_RenderCore orig, Engine self) {
        bool probe = Settings?.DebugLagSpikes ?? false;
        long t0 = 0;
        if (probe) {
            LagProbe.FrameStart();
            t0 = Stopwatch.GetTimestamp();
        } else {
            LagProbe.Stop();
        }

        orig(self);

        long t1 = probe ? Stopwatch.GetTimestamp() : 0;
        try {
            OverlayRenderer.Render();
            if (probe)
                OverlayRenderer.RenderLagProbe();
        } catch (Exception e) {
            // Never take the game down because of the overlay.
            OverlayRenderer.ReportError(e);
        }
        if (probe)
            LagProbe.AddRender(t1 - t0, Stopwatch.GetTimestamp() - t1);
    }
}
