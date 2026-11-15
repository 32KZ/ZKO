using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monocle;

namespace Celeste.Mod.ZKO;

/// <summary>
/// Draws the key overlay straight onto the back buffer after the whole frame has been composed
/// (called from an Engine.RenderCore postfix). Working on the final image is what makes the
/// "invert" options possible: an inverting blend needs the real pixels underneath.
///
/// All geometry is computed in real screen pixels and drawn as 1px-high rows, so the outline
/// ring and the fill never overlap (overlapping inverted pixels would cancel each other out).
/// </summary>
public static class OverlayRenderer {

    // ---- Layout (from Docs/design.png), in "design units" at 1080p, scale 100% ---------------

    private const float UnitW = 64f;    // width of a 1-wide key
    private const float KeyH = 56f;     // key height
    private const float Gap = 8f;       // gap between keys
    private const float Slant = 0.18f;  // horizontal shift per vertical pixel (italic lean)
    private const float Outline = 3f;   // outline thickness
    private const float Radius = 9f;    // corner rounding

    private const int Cols = 4, Rows = 3;

    private enum KeyId { Dash, Up, Grab, CrouchDash, Left, Down, Right, Esc, Jump }

    private struct KeyDef {
        public KeyId Id;
        public int Col, Row, Span;
        public string Text;      // null = direction key, label comes from the binding
        public float TextSize;   // label height relative to the other keys
        public KeyDef(KeyId id, int col, int row, int span, string text, float textSize = 1f) {
            Id = id; Col = col; Row = row; Span = span; Text = text; TextSize = textSize;
        }
    }

    //  Row 0:  [   DASH (2u)   ] [ W ] [GRB]
    //  Row 1:  [ CD ] [ A ] [ S ] [ D ]
    //  Row 2:  [ ESC ] [       JUMP (3u)      ]
    private static readonly KeyDef[] Layout = {
        new(KeyId.Dash, 0, 0, 2, "DASH"),
        new(KeyId.Up, 2, 0, 1, null),
        new(KeyId.Grab, 3, 0, 1, "GRB"),
        new(KeyId.CrouchDash, 0, 1, 1, "CD"),
        new(KeyId.Left, 1, 1, 1, null),
        new(KeyId.Down, 2, 1, 1, null),
        new(KeyId.Right, 3, 1, 1, null),
        new(KeyId.Esc, 0, 2, 1, "ESC"),
        new(KeyId.Jump, 1, 2, 3, "JUMP", 1.35f), // the design draws JUMP larger
    };

    // Inverting blend: result = src * (1 - dst) + dst * (1 - src).
    // Drawing white (src = 1) gives 1 - dst; drawing white * a gives lerp(dst, 1 - dst, a).
    private static readonly BlendState InvertBlend = new() {
        ColorBlendFunction = BlendFunction.Add,
        ColorSourceBlend = Blend.InverseDestinationColor,
        ColorDestinationBlend = Blend.InverseSourceColor,
        AlphaBlendFunction = BlendFunction.Add,
        AlphaSourceBlend = Blend.Zero,
        AlphaDestinationBlend = Blend.One,
    };

    private static readonly bool[] heldKeys = new bool[Layout.Length];
    private static bool batchOpen;
    private static bool failed;

    /// <summary>
    /// Called if Render threw. Closes our sprite batch so the game's own rendering is unaffected,
    /// logs the error once and switches the overlay off until the game restarts.
    /// </summary>
    public static void ReportError(Exception e) {
        if (batchOpen) {
            batchOpen = false;
            try {
                Draw.SpriteBatch.End();
            } catch {
                // Nothing more we can do here.
            }
        }
        if (failed)
            return;
        failed = true;
        Logger.Log(LogLevel.Error, nameof(ZKOModule), "Overlay render failed; the overlay is disabled until restart:\n" + e);
    }

    private static void Begin(BlendState blend, SamplerState sampler, Matrix matrix) {
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, blend, sampler, DepthStencilState.None, RasterizerState.CullNone, null, matrix);
        batchOpen = true;
    }

    private static void End() {
        batchOpen = false;
        Draw.SpriteBatch.End();
    }

    // ---- Visibility -------------------------------------------------------------------------

    private static bool ShouldShow(ZKOModuleSettings s) {
        if (failed || s == null || !s.Show)
            return false;
        Scene scene = Engine.Scene;
        if (scene == null || scene is GameLoader || Dialog.Language == null)
            return false;

        if (scene is Level level) {
            if (level.Paused)
                return s.ShowIn != ShowInMode.Gameplay;
            return true;
        }
        return s.ShowIn == ShowInMode.Everywhere;
    }

    // ---- Input ------------------------------------------------------------------------------

    // Same stick threshold Celeste uses for its own movement axes.
    private const float StickThreshold = 0.3f;

    /// <summary>True while any keyboard key, button or stick direction bound to the action is held.</summary>
    private static bool BindingHeld(Binding binding)
        => binding != null && binding.Check(Input.Gamepad, StickThreshold);

    private static bool Held(KeyId id) {
        Settings gs = Settings.Instance;
        switch (id) {
            case KeyId.Dash: return Input.Dash?.Check ?? false;
            case KeyId.CrouchDash: return Input.CrouchDash?.Check ?? false;
            case KeyId.Grab: return Input.Grab?.Check ?? false;
            case KeyId.Jump: return Input.Jump?.Check ?? false;
            case KeyId.Esc: return (Input.Pause?.Check ?? false) || (Input.ESC?.Check ?? false);
            // Directions read the raw bindings instead of Input.MoveX/MoveY: those only report the
            // newest of two opposite keys and get flipped by Mirror Mode, and an overlay should
            // show what is physically held.
            case KeyId.Left: return gs != null && (BindingHeld(gs.Left) || BindingHeld(gs.LeftMoveOnly));
            case KeyId.Right: return gs != null && (BindingHeld(gs.Right) || BindingHeld(gs.RightMoveOnly));
            case KeyId.Up: return gs != null && (BindingHeld(gs.Up) || BindingHeld(gs.UpMoveOnly));
            case KeyId.Down: return gs != null && (BindingHeld(gs.Down) || BindingHeld(gs.DownMoveOnly));
        }
        return false;
    }

    /// <summary>The keyboard key to print on a direction key (Keys.None if only a controller is bound).</summary>
    private static Keys DirectionKey(KeyId id) {
        Settings gs = Settings.Instance;
        if (gs == null)
            return Keys.None;
        Binding main, moveOnly;
        switch (id) {
            case KeyId.Left: main = gs.Left; moveOnly = gs.LeftMoveOnly; break;
            case KeyId.Right: main = gs.Right; moveOnly = gs.RightMoveOnly; break;
            case KeyId.Up: main = gs.Up; moveOnly = gs.UpMoveOnly; break;
            case KeyId.Down: main = gs.Down; moveOnly = gs.DownMoveOnly; break;
            default: return Keys.None;
        }
        Keys key = KeyLabels.FirstKey(main);
        return key != Keys.None ? key : KeyLabels.FirstKey(moveOnly);
    }

    // ---- Render -----------------------------------------------------------------------------

    private struct Colors {
        public Color Fill, Outline, Pressed, Label, InvertWhite;
    }

    public static void Render() {
        ZKOModuleSettings s = ZKOModule.Settings;
        if (!ShouldShow(s))
            return;

        GraphicsDevice gd = Engine.Instance.GraphicsDevice;
        gd.SetRenderTarget(null);
        gd.Viewport = Engine.Viewport;

        float screenW = Engine.ViewWidth, screenH = Engine.ViewHeight;
        float px = screenW / 1920f * Math.Clamp(s.Scale, 10, 500) / 100f; // design unit -> screen pixel

        float totalW = Cols * UnitW + (Cols - 1) * Gap;
        float totalH = Rows * KeyH + (Rows - 1) * Gap;
        float boxW = (totalW + Slant * totalH) * px; // bounding box incl. the lean
        float boxH = totalH * px;

        float padX = screenW * s.PaddingX / 100f, padY = screenH * s.PaddingY / 100f;
        float ox, oy;
        switch (s.Position) {
            case OverlayPosition.TopLeft: ox = padX; oy = padY; break;
            case OverlayPosition.TopRight: ox = screenW - padX - boxW; oy = padY; break;
            case OverlayPosition.BottomRight: ox = screenW - padX - boxW; oy = screenH - padY - boxH; break;
            case OverlayPosition.Custom:
                ox = (screenW - boxW) * Math.Clamp(s.CustomX, 0, 100) / 100f;
                oy = (screenH - boxH) * Math.Clamp(s.CustomY, 0, 100) / 100f;
                break;
            default: ox = padX; oy = screenH - padY - boxH; break; // BottomLeft
        }
        ox = MathF.Round(ox);
        oy = MathF.Round(oy);

        float alpha = Math.Clamp(s.Opacity, 0, 100) / 100f;
        Colors col = new() {
            Fill = ColorUtil.Parse(s.FillColor, Color.Black) * alpha,
            Outline = ColorUtil.Parse(s.OutlineColor, Color.White) * alpha,
            Pressed = ColorUtil.Parse(s.PressedColor, new Color(0xA8, 0xB0, 0x3A)) * alpha,
            Label = ColorUtil.Parse(s.LabelColor, Color.White) * alpha,
            InvertWhite = Color.White * alpha,
        };

        for (int i = 0; i < Layout.Length; i++)
            heldKeys[i] = Held(Layout[i].Id);

        // Pass 1: normal (alpha-blended) shapes. Pass 2: inverted shapes.
        for (int pass = 0; pass < 2; pass++) {
            bool invertPass = pass == 1;
            if (invertPass && !s.InvertFill && !s.InvertOutline)
                continue;
            Begin(invertPass ? InvertBlend : BlendState.AlphaBlend, SamplerState.PointClamp, Matrix.Identity);
            for (int i = 0; i < Layout.Length; i++)
                DrawKeyShape(Layout[i], heldKeys[i], ox, oy, px, totalH, s, col, invertPass);
            End();
        }

        // Pass 3: labels, skewed to match the keys' lean.
        // Skew matrix maps (x, y) -> (x - Slant * y, y); positions are pre-shifted by +Slant * y.
        Matrix skew = Matrix.Identity;
        skew.M21 = -Slant;
        Begin(s.InvertLabels ? InvertBlend : BlendState.AlphaBlend, SamplerState.LinearClamp, skew);
        Color labelColor = s.InvertLabels ? col.InvertWhite : col.Label;
        for (int i = 0; i < Layout.Length; i++)
            DrawKeyLabel(Layout[i], ox, oy, px, totalH, labelColor);
        End();
    }

    /// <summary>Draws the latest lag probe report (see LagProbe) near the top of the screen.</summary>
    public static void RenderLagProbe() {
        string text = LagProbe.CurrentText;
        Scene scene = Engine.Scene;
        if (failed || text == null || scene == null || scene is GameLoader || Dialog.Language == null)
            return;

        GraphicsDevice gd = Engine.Instance.GraphicsDevice;
        gd.SetRenderTarget(null);
        gd.Viewport = Engine.Viewport;

        float scale = Engine.ViewWidth / 1920f;
        Begin(BlendState.AlphaBlend, SamplerState.LinearClamp, Matrix.CreateScale(scale));
        ActiveFont.DrawOutline(text, new Vector2(960f, 60f), new Vector2(0.5f, 0f), Vector2.One * 0.5f, Color.White, 2f, Color.Black);
        End();
    }

    private static void KeyRect(KeyDef k, float px, out float x, out float y, out float w, out float h) {
        x = k.Col * (UnitW + Gap) * px;
        y = k.Row * (KeyH + Gap) * px;
        w = (k.Span * UnitW + (k.Span - 1) * Gap) * px;
        h = KeyH * px;
    }

    /// <summary>How far to pull a row in from the side for rounded corners.</summary>
    private static float CornerTrim(float rowCenter, float height, float radius) {
        if (radius <= 0f)
            return 0f;
        float d = rowCenter < radius ? radius - rowCenter
            : rowCenter > height - radius ? rowCenter - (height - radius)
            : 0f;
        if (d <= 0f)
            return 0f;
        d = Math.Min(d, radius);
        return radius - MathF.Sqrt(radius * radius - d * d);
    }

    private static void DrawKeyShape(KeyDef k, bool held, float ox, float oy, float px, float totalH,
        ZKOModuleSettings s, Colors col, bool invertPass) {
        KeyRect(k, px, out float kx, out float ky, out float kw, out float kh);
        float t = MathF.Max(1f, MathF.Round(Outline * px));
        float r = Radius * px;
        float ri = MathF.Max(0f, r - t);
        float totalHpx = totalH * px;

        // Which parts belong to this pass? Parts of the other pass are drawn as "nothing".
        bool fillInverted = s.InvertFill && !held; // the pressed colour is never inverted
        bool drawOutline = s.InvertOutline == invertPass;
        bool drawFill = fillInverted == invertPass;
        if (!drawOutline && !drawFill)
            return;
        Vector4 outlineColor = !drawOutline ? Vector4.Zero : (invertPass ? col.InvertWhite : col.Outline).ToVector4();
        Vector4 fillColor = !drawFill ? Vector4.Zero : (invertPass ? col.InvertWhite : (held ? col.Pressed : col.Fill)).ToVector4();

        int rows = Math.Max(1, (int) MathF.Round(kh));
        int top = (int) oy + (int) MathF.Round(ky);
        for (int py = 0; py < rows; py++) {
            float rowCenter = py + 0.5f;
            float lean = Slant * (totalHpx - (ky + rowCenter)); // rows further up lean further right
            float left = ox + kx + lean;
            float right = left + kw;
            float trim = CornerTrim(rowCenter, rows, r);

            rowBounds[0] = left + trim;
            rowBounds[3] = right - trim;
            if (rowCenter > t && rowCenter < rows - t) {
                float trimIn = CornerTrim(rowCenter - t, rows - 2 * t, ri);
                rowBounds[1] = Math.Max(rowBounds[0], left + t + trimIn);
                rowBounds[2] = Math.Min(rowBounds[3], right - t - trimIn);
                if (rowBounds[2] < rowBounds[1])
                    rowBounds[1] = rowBounds[2] = (rowBounds[1] + rowBounds[2]) / 2f;
            } else {
                // Top / bottom edge of the outline: the whole row is outline.
                rowBounds[1] = rowBounds[2] = rowBounds[3];
            }
            rowColors[0] = outlineColor;
            rowColors[1] = fillColor;
            rowColors[2] = outlineColor;
            DrawRow(top + py);
        }
    }

    // Scratch buffers for DrawRow: 4 boundaries (left to right) and the 3 colours between them.
    private static readonly float[] rowBounds = new float[4];
    private static readonly Vector4[] rowColors = new Vector4[3];

    /// <summary>
    /// Draws one 1px-high row described by rowBounds / rowColors (premultiplied colours).
    /// Whole pixels inside a segment are drawn as one rect; a pixel that straddles a boundary is
    /// drawn once with the coverage-weighted mix of what it contains. That anti-aliases the
    /// slanted edges and guarantees no pixel is drawn twice, which the inverting blend relies on.
    /// </summary>
    private static void DrawRow(int y) {
        const int n = 3;
        int x = (int) MathF.Floor(rowBounds[0]);
        int end = (int) MathF.Ceiling(rowBounds[n]);
        int seg = 0;
        while (x < end) {
            while (seg < n - 1 && rowBounds[seg + 1] <= x)
                seg++;

            if (rowBounds[seg] <= x && rowBounds[seg + 1] >= x + 1) {
                // Run of pixels fully inside this segment.
                int runEnd = Math.Min(end, (int) MathF.Floor(rowBounds[seg + 1]));
                if (rowColors[seg] != Vector4.Zero)
                    Draw.Rect(x, y, runEnd - x, 1, new Color(rowColors[seg]));
                x = runEnd;
                continue;
            }

            // Pixel shared by several segments (or the outside): mix by coverage.
            Vector4 mix = Vector4.Zero;
            for (int i = seg; i < n && rowBounds[i] < x + 1; i++) {
                float overlap = Math.Min(rowBounds[i + 1], x + 1) - Math.Max(rowBounds[i], x);
                if (overlap > 0f)
                    mix += rowColors[i] * overlap;
            }
            if (mix != Vector4.Zero)
                Draw.Rect(x, y, 1, 1, new Color(mix));
            x++;
        }
    }

    private static void DrawKeyLabel(KeyDef k, float ox, float oy, float px, float totalH, Color color) {
        KeyRect(k, px, out float kx, out float ky, out float kw, out float kh);
        float totalHpx = totalH * px;
        float cy = oy + ky + kh / 2f;
        // Centre of the key on screen, then pre-shift by +Slant * cy so the skew matrix lands it there.
        float leanAtCenter = Slant * (totalHpx - (ky + kh / 2f));
        float cx = ox + kx + leanAtCenter + kw / 2f;
        Vector2 center = new(cx + Slant * cy, cy);

        string text = k.Text;
        if (text == null) {
            // Direction key: show the bound key, or an arrow for arrow keys / no keyboard binding.
            Keys bound = DirectionKey(k.Id);
            if (bound == Keys.None || KeyLabels.IsArrow(bound)) {
                DrawArrow(k.Id, center, kh * 0.42f, color);
                return;
            }
            text = KeyLabels.Label(bound);
        }

        Vector2 size = ActiveFont.Measure(text);
        if (size.X <= 0f || size.Y <= 0f)
            return;
        float scale = Math.Min(kh * 0.62f * k.TextSize / size.Y, kw * 0.78f / size.X);
        ActiveFont.Draw(text, center, new Vector2(0.5f, 0.5f), Vector2.One * scale, color);
    }

    /// <summary>Solid triangle made of 1px rows, centred on <paramref name="c"/>.</summary>
    private static void DrawArrow(KeyId dir, Vector2 c, float size, Color color) {
        int half = Math.Max(2, (int) MathF.Round(size / 2f));
        int cx = (int) MathF.Round(c.X), cy = (int) MathF.Round(c.Y);
        switch (dir) {
            case KeyId.Up:
            case KeyId.Down:
                // Triangle `half` rows tall around the centre, widest at the base.
                for (int i = 0; i < half * 2; i++) {
                    float frac = (i + 0.5f) / (half * 2);           // 0 at tip, 1 at base
                    int w = Math.Max(1, (int) MathF.Round(frac * half * 2));
                    int y = dir == KeyId.Up ? cy - half + i : cy + half - 1 - i;
                    Draw.Rect(cx - w / 2f, y, w, 1, color);
                }
                break;
            default:
                // Left / right: each row's length shrinks towards the top and bottom tips.
                for (int i = -half; i < half; i++) {
                    float frac = 1f - Math.Abs(i + 0.5f) / half;   // 1 in the middle row
                    int len = Math.Max(1, (int) MathF.Round(frac * half * 2));
                    float x = dir == KeyId.Right ? cx - half : cx + half - len;
                    Draw.Rect(x, cy + i, len, 1, color);
                }
                break;
        }
    }
}
