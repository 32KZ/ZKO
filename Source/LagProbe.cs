using System;
using System.Diagnostics;
using Monocle;

namespace Celeste.Mod.ZKO;

/// <summary>
/// Optional diagnostic ("Debug: Show Lag Spikes"). Times every frame and, when one takes too long,
/// reports where the time went: the game's own update, the game's own render, ZKO's code,
/// garbage collection, or somewhere else (GPU / presenting the frame).
/// The report is written to log.txt and shown on screen for a few seconds.
/// </summary>
public static class LagProbe {
    /// <summary>A frame-to-frame gap at or above this counts as a spike (60 fps = 16.7 ms).</summary>
    private const double ThresholdMs = 40.0;
    private const double ShowSeconds = 12.0;

    private static bool running;
    private static long frameStart;
    private static long gameUpdate, gameRender, zkoTime;
    private static int updates;
    private static TimeSpan gcPauseAtStart;
    private static int gen2AtStart;
    private static int spikeCount;
    private static long spikeShownAt;

    /// <summary>Text of the latest spike while it should still be on screen, otherwise null.</summary>
    public static string CurrentText {
        get {
            if (!running || lastText == null)
                return null;
            return Ms(Stopwatch.GetTimestamp() - spikeShownAt) < ShowSeconds * 1000.0 ? lastText : null;
        }
    }
    private static string lastText;

    private static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

    public static void AddUpdate(long gameTicks, long zkoTicks) {
        gameUpdate += gameTicks;
        zkoTime += zkoTicks;
        updates++;
    }

    public static void AddRender(long gameTicks, long zkoTicks) {
        gameRender += gameTicks;
        zkoTime += zkoTicks;
    }

    public static void Stop() {
        running = false;
        lastText = null;
    }

    /// <summary>
    /// Call at the start of every rendered frame. The counters then cover: the previous frame's
    /// render, presenting it, and all updates that ran before this frame.
    /// </summary>
    public static void FrameStart() {
        long now = Stopwatch.GetTimestamp();
        TimeSpan gcPause = GC.GetTotalPauseDuration();
        int gen2 = GC.CollectionCount(2);

        if (running) {
            double gap = Ms(now - frameStart);
            if (gap >= ThresholdMs && Engine.Instance.IsActive) {
                double update = Ms(gameUpdate), render = Ms(gameRender), zko = Ms(zkoTime);
                double other = Math.Max(0.0, gap - update - render - zko);
                double gc = (gcPause - gcPauseAtStart).TotalMilliseconds;
                int fullGcs = gen2 - gen2AtStart;
                spikeCount++;
                lastText =
                    $"ZKO lag probe #{spikeCount}: {gap:0} ms frame in {Engine.Scene?.GetType().Name ?? "no scene"}\n" +
                    $"game update {update:0.0} ms ({updates}x), game render {render:0.0} ms, ZKO {zko:0.00} ms, other {other:0.0} ms\n" +
                    $"of which GC pause {gc:0.0} ms, full GCs {fullGcs}";
                spikeShownAt = now;
                Logger.Log(LogLevel.Info, nameof(ZKOModule), lastText.Replace("\n", " | "));
            }
        }

        running = true;
        frameStart = now;
        gameUpdate = gameRender = zkoTime = 0;
        updates = 0;
        gcPauseAtStart = gcPause;
        gen2AtStart = gen2;
    }
}
