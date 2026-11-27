using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace BiliGamePiP.Controls;

internal static class TechTheme
{
    public static readonly Color BgWindow = Color.FromArgb(14, 16, 24);
    public static readonly Color BgSurface = Color.FromArgb(20, 23, 35);
    public static readonly Color BgSurfaceHover = Color.FromArgb(29, 33, 49);
    public static readonly Color BgInput = Color.FromArgb(17, 20, 31);
    public static readonly Color BorderSubtle = Color.FromArgb(39, 44, 64);
    public static readonly Color BorderBright = Color.FromArgb(62, 70, 102);

    public static readonly Color TextPrimary = Color.FromArgb(241, 244, 252);
    public static readonly Color TextSecondary = Color.FromArgb(152, 160, 186);

    public static readonly Color AccentPink = Color.FromArgb(251, 114, 153);
    public static readonly Color AccentPinkStrong = Color.FromArgb(255, 79, 139);
    public static readonly Color AccentCyan = Color.FromArgb(56, 189, 248);

    public static GraphicsPath CreateRoundedRect(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return path;
        }

        float r = Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2f);
        if (r <= 0.5f)
        {
            path.AddRectangle(rect);
            return path;
        }

        float d = r * 2f;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

public enum TechButtonVariant
{
    Ghost,
    PrimaryGradient,
    CyanAccent
}

public sealed class TechButton : Control
{
    private bool _hover;
    private bool _pressed;

    public TechButtonVariant Variant { get; set; } = TechButtonVariant.Ghost;
    public int CornerRadius { get; set; } = 9;

    public TechButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);
        Cursor = Cursors.Hand;
        ForeColor = TechTheme.TextPrimary;
        Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular);
        Size = new Size(92, 32);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hover = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = false;
        _pressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _pressed = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width <= 1 || Height <= 1) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        if (rect.Width <= 1 || rect.Height <= 1) return;

        using var path = TechTheme.CreateRoundedRect(rect, CornerRadius);

        if (!Enabled)
        {
            using var disBrush = new SolidBrush(Color.FromArgb(26, 29, 42));
            g.FillPath(disBrush, path);
            using var disPen = new Pen(TechTheme.BorderSubtle, 1f);
            g.DrawPath(disPen, path);
        }
        else if (Variant == TechButtonVariant.PrimaryGradient)
        {
            Color c1 = _pressed ? Color.FromArgb(220, 82, 124) : (_hover ? Color.FromArgb(255, 128, 166) : TechTheme.AccentPink);
            Color c2 = _pressed ? Color.FromArgb(224, 58, 114) : (_hover ? Color.FromArgb(255, 95, 152) : TechTheme.AccentPinkStrong);
            using var brush = new LinearGradientBrush(rect, c1, c2, 15f);
            g.FillPath(brush, path);
        }
        else if (Variant == TechButtonVariant.CyanAccent)
        {
            Color bg = _pressed
                ? Color.FromArgb(28, 66, 92)
                : (_hover ? Color.FromArgb(30, 82, 115) : Color.FromArgb(22, 56, 82));
            using var brush = new SolidBrush(bg);
            g.FillPath(brush, path);
            using var pen = new Pen(Color.FromArgb(140, TechTheme.AccentCyan), 1f);
            g.DrawPath(pen, path);
        }
        else
        {
            Color bg = _pressed
                ? Color.FromArgb(35, 40, 58)
                : (_hover ? TechTheme.BgSurfaceHover : TechTheme.BgSurface);
            using var brush = new SolidBrush(bg);
            g.FillPath(brush, path);
            using var pen = new Pen(_hover ? TechTheme.BorderBright : TechTheme.BorderSubtle, 1f);
            g.DrawPath(pen, path);
        }

        Color textColor = !Enabled
            ? TechTheme.TextSecondary
            : (Variant == TechButtonVariant.CyanAccent ? Color.FromArgb(186, 234, 255) : ForeColor);

        TextRenderer.DrawText(
            g,
            Text,
            Font,
            new Rectangle(0, 0, Width, Height),
            textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

public sealed class TechCardPanel : Panel
{
    public int CornerRadius { get; set; } = 12;
    public Color CardBackColor { get; set; } = TechTheme.BgSurface;
    public Color CardBorderColor { get; set; } = TechTheme.BorderSubtle;

    public TechCardPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width <= 1 || Height <= 1) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        if (rect.Width <= 1 || rect.Height <= 1) return;

        using var path = TechTheme.CreateRoundedRect(rect, CornerRadius);
        using var fill = new SolidBrush(CardBackColor);
        using var pen = new Pen(CardBorderColor, 1f);
        g.FillPath(fill, path);
        g.DrawPath(pen, path);
    }
}

public sealed class TechAddressBar : Control
{
    private readonly TextBox _inner;
    private bool _focused;

    public event EventHandler<string>? Submitted;

    public override string Text
    {
        get => _inner.Text;
        set => _inner.Text = value;
    }

    public string PlaceholderText
    {
        get => _inner.PlaceholderText;
        set => _inner.PlaceholderText = value;
    }

    public TechAddressBar()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        Height = 32;
        Cursor = Cursors.IBeam;

        _inner = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = TechTheme.BgInput,
            ForeColor = TechTheme.TextPrimary,
            Font = new Font("Microsoft YaHei UI", 9.2f),
            Anchor = AnchorStyles.Left | AnchorStyles.Right
        };

        _inner.GotFocus += (_, _) => { _focused = true; Invalidate(); };
        _inner.LostFocus += (_, _) => { _focused = false; Invalidate(); };
        _inner.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                Submitted?.Invoke(this, _inner.Text);
            }
        };

        Controls.Add(_inner);
        LayoutInner();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        LayoutInner();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _inner.Focus();
    }

    private void LayoutInner()
    {
        int h = _inner.PreferredHeight;
        int y = Math.Max(2, (Height - h) / 2);
        _inner.Bounds = new Rectangle(30, y, Math.Max(40, Width - 42), h);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width <= 1 || Height <= 1) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        if (rect.Width <= 1 || rect.Height <= 1) return;

        using var path = TechTheme.CreateRoundedRect(rect, Height / 2f - 1f);
        using var fill = new SolidBrush(TechTheme.BgInput);
        using var pen = new Pen(_focused ? TechTheme.AccentPink : TechTheme.BorderSubtle, _focused ? 1.4f : 1f);
        g.FillPath(fill, path);
        g.DrawPath(pen, path);

        // Minimalist link/globe glyph
        TextRenderer.DrawText(
            g,
            "⌕",
            new Font("Segoe UI Symbol", 10.5f),
            new Rectangle(8, 0, 20, Height),
            _focused ? TechTheme.AccentPink : TechTheme.TextSecondary,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

public sealed class TechSlider : Control
{
    private int _minimum = 40;
    private int _maximum = 100;
    private int _value = 100;
    private bool _dragging;

    public event EventHandler? ValueChanged;

    public int Minimum
    {
        get => _minimum;
        set { _minimum = value; Invalidate(); }
    }

    public int Maximum
    {
        get => _maximum;
        set { _maximum = value; Invalidate(); }
    }

    public int Value
    {
        get => _value;
        set
        {
            int clamped = Math.Clamp(value, _minimum, _maximum);
            if (clamped != _value)
            {
                _value = clamped;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public TechSlider()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        Cursor = Cursors.Hand;
        Height = 28;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            _dragging = true;
            UpdateFromMouse(e.X);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging)
        {
            UpdateFromMouse(e.X);
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragging = false;
    }

    private void UpdateFromMouse(int x)
    {
        int pad = 9;
        int trackW = Math.Max(1, Width - pad * 2);
        double ratio = Math.Clamp((x - pad) / (double)trackW, 0.0, 1.0);
        Value = (int)Math.Round(_minimum + ratio * (_maximum - _minimum));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width <= 1 || Height <= 1) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int pad = 9;
        int trackH = 6;
        int trackY = (Height - trackH) / 2;
        int trackW = Math.Max(1, Width - pad * 2);

        var trackRect = new RectangleF(pad, trackY, trackW, trackH);
        using (var trackPath = TechTheme.CreateRoundedRect(trackRect, 3f))
        using (var trackBrush = new SolidBrush(Color.FromArgb(40, 46, 66)))
        {
            g.FillPath(trackBrush, trackPath);
        }

        float ratio = _maximum > _minimum ? (_value - _minimum) / (float)(_maximum - _minimum) : 0f;
        float fillW = Math.Max(6f, trackW * ratio);
        var fillRect = new RectangleF(pad, trackY, fillW, trackH);
        using (var fillPath = TechTheme.CreateRoundedRect(fillRect, 3f))
        using (var fillBrush = new LinearGradientBrush(trackRect, TechTheme.AccentPink, TechTheme.AccentCyan, 0f))
        {
            g.FillPath(fillBrush, fillPath);
        }

        float thumbX = pad + trackW * ratio;
        float thumbR = 7.5f;
        var thumbRect = new RectangleF(thumbX - thumbR, Height / 2f - thumbR, thumbR * 2, thumbR * 2);
        using var glowBrush = new SolidBrush(Color.FromArgb(65, TechTheme.AccentPink));
        g.FillEllipse(glowBrush, thumbRect.X - 3, thumbRect.Y - 3, thumbRect.Width + 6, thumbRect.Height + 6);
        using var thumbBrush = new SolidBrush(Color.White);
        using var thumbPen = new Pen(TechTheme.AccentPink, 2f);
        g.FillEllipse(thumbBrush, thumbRect);
        g.DrawEllipse(thumbPen, thumbRect);
    }
}

public sealed class TechProgressBar : Control
{
    private int _percent;

    public int Percent
    {
        get => _percent;
        set
        {
            int clamped = Math.Clamp(value, 0, 100);
            if (clamped != _percent)
            {
                _percent = clamped;
                Invalidate();
            }
        }
    }

    public TechProgressBar()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        Height = 10;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width <= 1 || Height <= 1) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new RectangleF(0, 0, Width, Height);
        using (var bgPath = TechTheme.CreateRoundedRect(rect, Height / 2f))
        using (var bgBrush = new SolidBrush(Color.FromArgb(34, 39, 56)))
        {
            g.FillPath(bgBrush, bgPath);
        }

        if (_percent > 0)
        {
            float w = Math.Max(Height, Width * (_percent / 100f));
            var fillRect = new RectangleF(0, 0, w, Height);
            using var fillPath = TechTheme.CreateRoundedRect(fillRect, Height / 2f);
            using var fillBrush = new LinearGradientBrush(rect, TechTheme.AccentPink, TechTheme.AccentCyan, 0f);
            g.FillPath(fillBrush, fillPath);
        }
    }
}

public sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public DarkMenuRenderer() : base(new DarkColorTable())
    {
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = TechTheme.TextPrimary;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        int y = e.Item.Height / 2;
        using var pen = new Pen(TechTheme.BorderSubtle);
        e.Graphics.DrawLine(pen, 10, y, e.Item.Width - 10, y);
    }

    private sealed class DarkColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => TechTheme.BgSurface;
        public override Color MenuBorder => TechTheme.BorderBright;
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => Color.FromArgb(44, 50, 74);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(44, 50, 74);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(44, 50, 74);
        public override Color ImageMarginGradientBegin => TechTheme.BgSurface;
        public override Color ImageMarginGradientMiddle => TechTheme.BgSurface;
        public override Color ImageMarginGradientEnd => TechTheme.BgSurface;
    }
}
