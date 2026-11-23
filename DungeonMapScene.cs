using System.Drawing.Drawing2D;

namespace Nexus;

/// <summary>A clickable, illustrated dungeon scene mapped to the existing 4 × 5 game grid.</summary>
public sealed class DungeonMapScene : Control
{
    private static readonly Color Violet = Color.FromArgb(151, 105, 255);
    private static readonly Color Blue = Color.FromArgb(91, 160, 255);
    private GameSession? _session;
    private Action<int, int>? _move;
    private (int Row, int Column)? _hovered;

    public DungeonMapScene()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.FromArgb(5, 7, 18);
        Cursor = Cursors.Hand;
    }

    public void Bind(GameSession session, Action<int, int> move)
    {
        _session = session;
        _move = move;
        Invalidate();
    }

    public void RefreshScene() => Invalidate();

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        if (Width < 1 || Height < 1) return;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        DrawBackdrop(g);
        if (_session is null) return;

        for (int row = 0; row < _session.Mapa.Filas; row++)
            for (int column = 0; column < _session.Mapa.Columnas; column++)
                DrawZone(g, row, column);

        DrawHoverReadout(g);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var next = HitTest(e.Location);
        if (next == _hovered) return;
        _hovered = next;
        Cursor = next.HasValue ? Cursors.Hand : Cursors.Default;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hovered = null;
        Cursor = Cursors.Default;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Left) return;
        var zone = HitTest(e.Location);
        if (zone.HasValue) _move?.Invoke(zone.Value.Row, zone.Value.Column);
    }

    private PointF Center(int row, int column)
    {
        // A hand-composed route keeps the logical 4 × 5 map, while the art
        // reads as scattered ledges in a dungeon instead of a board.
        float[,] layout =
        {
            { .08f,.19f, .25f,.08f, .43f,.18f, .67f,.08f, .88f,.22f },
            { .73f,.40f, .57f,.37f, .38f,.43f, .22f,.36f, .07f,.54f },
            { .18f,.67f, .36f,.63f, .56f,.70f, .79f,.62f, .92f,.76f },
            { .72f,.89f, .52f,.85f, .31f,.91f, .12f,.83f, .46f,.98f }
        };
        float usableHeight = Math.Max(100, Height - 35);
        return new PointF(18 + layout[row, column * 2] * Math.Max(80, Width - 36), 8 + layout[row, column * 2 + 1] * usableHeight);
    }

    private (int Row, int Column)? HitTest(Point point)
    {
        if (_session is null) return null;
        for (int row = 0; row < _session.Mapa.Filas; row++)
            for (int column = 0; column < _session.Mapa.Columnas; column++)
            {
                PointF center = Center(row, column);
                bool visible = _session.Mapa.FueVisitada(row, column) || IsAdjacentToCurrent(row, column);
                if (!visible) continue;
                float dx = (point.X - center.X) / 40f;
                float dy = (point.Y - center.Y) / 30f;
                if (dx * dx + dy * dy <= 1f) return (row, column);
            }
        return null;
    }

    private void DrawBackdrop(Graphics g)
    {
        var bounds = ClientRectangle;
        using (var sky = new LinearGradientBrush(bounds, Color.FromArgb(30, 23, 72), Color.FromArgb(4, 7, 20), LinearGradientMode.Vertical))
            g.FillRectangle(sky, bounds);

        var random = new Random(27);
        for (int i = 0; i < 78; i++)
        {
            int x = random.Next(Math.Max(1, Width)), y = random.Next(Math.Max(1, Height * 3 / 4));
            int size = random.Next(1, 3);
            using var star = new SolidBrush(Color.FromArgb(random.Next(45, 160), 213, 205, 255));
            g.FillEllipse(star, x, y, size, size);
        }

        int moonRadius = Math.Max(24, Math.Min(Width, Height) / 9);
        int moonX = Width * 79 / 100, moonY = Height / 5;
        using (var moonGlow = new SolidBrush(Color.FromArgb(25, 150, 109, 255)))
            g.FillEllipse(moonGlow, moonX - moonRadius * 2, moonY - moonRadius * 2, moonRadius * 4, moonRadius * 4);
        using (var moon = new LinearGradientBrush(new Rectangle(moonX - moonRadius, moonY - moonRadius, moonRadius * 2, moonRadius * 2), Color.FromArgb(205, 207, 208, 255), Color.FromArgb(74, 67, 137), LinearGradientMode.ForwardDiagonal))
            g.FillEllipse(moon, moonX - moonRadius, moonY - moonRadius, moonRadius * 2, moonRadius * 2);

        int horizon = Height * 56 / 100;
        using (var mountains = new SolidBrush(Color.FromArgb(194, 13, 13, 36)))
        {
            g.FillPolygon(mountains, new[]
            {
                new Point(0, horizon + 30), new Point(Width / 8, horizon - 30), new Point(Width / 4, horizon + 24),
                new Point(Width * 2 / 5, horizon - 43), new Point(Width * 3 / 5, horizon + 18),
                new Point(Width * 4 / 5, horizon - 24), new Point(Width, horizon + 22), new Point(Width, Height), new Point(0, Height)
            });
            DrawTower(g, Width * 18 / 100, horizon + 8, Math.Max(36, Width / 13), Math.Max(62, Height / 3), mountains);
            DrawTower(g, Width * 70 / 100, horizon + 6, Math.Max(30, Width / 16), Math.Max(56, Height / 4), mountains);
        }

        using (var mist = new SolidBrush(Color.FromArgb(19, 122, 91, 223)))
            for (int i = 0; i < 9; i++)
                g.FillEllipse(mist, i * Width / 8 - 25, horizon + (i % 3) * 12, Width / 5, 26);

        using var groundLine = new Pen(Color.FromArgb(66, Violet), 1f);
        g.DrawLine(groundLine, 0, horizon + 12, Width, horizon + 12);
    }

    private static void DrawTower(Graphics g, int centerX, int baseY, int width, int height, Brush brush)
    {
        int left = centerX - width / 2, top = baseY - height;
        g.FillRectangle(brush, left, top, width, height);
        g.FillPolygon(brush, new[] { new Point(left - 9, top + 7), new Point(centerX, top - 18), new Point(left + width + 9, top + 7) });
        using var windows = new SolidBrush(Color.FromArgb(87, 166, 123, 255));
        for (int y = top + 24; y < baseY - 10; y += 25)
            g.FillEllipse(windows, centerX - 2, y, 4, 7);
    }

    private void DrawRoutes(Graphics g)
    {
        if (_session is null) return;
        for (int row = 0; row < _session.Mapa.Filas; row++)
            for (int column = 0; column < _session.Mapa.Columnas; column++)
            {
                if (column + 1 < _session.Mapa.Columnas) DrawRoute(g, row, column, row, column + 1);
                if (row + 1 < _session.Mapa.Filas) DrawRoute(g, row, column, row + 1, column);
            }
    }

    private void DrawRoute(Graphics g, int rowA, int columnA, int rowB, int columnB)
    {
        PointF from = Center(rowA, columnA), to = Center(rowB, columnB);
        bool known = _session!.Mapa.FueVisitada(rowA, columnA) && _session.Mapa.FueVisitada(rowB, columnB);
        using var shadow = new Pen(Color.FromArgb(110, 3, 5, 17), 9f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var energy = new Pen(Color.FromArgb(known ? 142 : 48, known ? 125 : 82, known ? 102 : 78, 214), known ? 2f : 1.4f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            DashStyle = known ? DashStyle.Solid : DashStyle.Dot
        };
        g.DrawLine(shadow, from, to);
        g.DrawLine(energy, from, to);
    }

    private void DrawZone(Graphics g, int row, int column)
    {
        if (_session is null) return;
        PointF center = Center(row, column);
        bool current = row == _session.Mapa.Fila && column == _session.Mapa.Columna;
        bool visited = _session.Mapa.FueVisitada(row, column);
        bool adjacent = IsAdjacentToCurrent(row, column);
        if (!visited && !adjacent) return;
        bool hovered = _hovered is { } cell && cell.Row == row && cell.Column == column;
        TipoCelda type = _session.Mapa.TipoEn(row, column);

        if (current)
        {
            using var halo = new SolidBrush(Color.FromArgb(47, 154, 103, 255));
            g.FillEllipse(halo, center.X - 48, center.Y - 37, 96, 74);
        }

        if (!visited && !current)
        {
            DrawFog(g, center);
            return;
        }

        PointF[] island = Island(center, 40, 18);
        PointF[] shadow = Island(new PointF(center.X, center.Y + 8), 42, 18);
        using (var shade = new SolidBrush(Color.FromArgb(185, 3, 5, 16))) g.FillPolygon(shade, shadow);

        Color topColor = type switch
        {
            TipoCelda.Base => Color.FromArgb(27, 48, 79),
            TipoCelda.Recurso => Color.FromArgb(37, 34, 75),
            TipoCelda.Anomalia => Color.FromArgb(62, 27, 75),
            _ => Color.FromArgb(27, 27, 55)
        };
        using (var surface = new LinearGradientBrush(ZoneBounds(center), ControlPaint.Light(topColor, .15f), topColor, LinearGradientMode.Vertical))
            g.FillPolygon(surface, island);
        using (var rim = new Pen(current ? Color.FromArgb(238, 199, 161, 255) : hovered ? Color.FromArgb(210, 116, 194, 255) : Color.FromArgb(visited ? 130 : 53, 119, 104, 189), current ? 2.5f : 1.3f))
            g.DrawPolygon(rim, island);

        DrawRockEdges(g, center, visited || current);

        switch (type)
        {
            case TipoCelda.Base: DrawBeacon(g, center); break;
            case TipoCelda.Recurso:
                if (row == 2 && column == 4) DrawCore(g, center);
                else DrawCrystal(g, center);
                break;
            case TipoCelda.Anomalia: DrawRift(g, center); break;
            default: DrawRune(g, center); break;
        }

        if (current) DrawHunter(g, center);
    }

    private bool IsAdjacentToCurrent(int row, int column) => _session is not null
        && Math.Abs(row - _session.Mapa.Fila) + Math.Abs(column - _session.Mapa.Columna) == 1;

    private static PointF[] Island(PointF c, float rx, float ry) => new[]
    {
        new PointF(c.X - rx, c.Y - ry * .15f), new PointF(c.X - rx * .66f, c.Y - ry * .76f),
        new PointF(c.X - rx * .12f, c.Y - ry), new PointF(c.X + rx * .56f, c.Y - ry * .72f),
        new PointF(c.X + rx, c.Y - ry * .08f), new PointF(c.X + rx * .74f, c.Y + ry * .58f),
        new PointF(c.X + rx * .12f, c.Y + ry), new PointF(c.X - rx * .64f, c.Y + ry * .66f)
    };

    private static RectangleF ZoneBounds(PointF c) => new(c.X - 36, c.Y - 22, 72, 44);

    private static void DrawRockEdges(Graphics g, PointF c, bool lit)
    {
        using var rock = new Pen(Color.FromArgb(lit ? 126 : 49, 67, 61, 112), 1.4f);
        g.DrawLine(rock, c.X - 18, c.Y + 12, c.X - 24, c.Y + 23);
        g.DrawLine(rock, c.X + 10, c.Y + 15, c.X + 15, c.Y + 25);
        g.DrawLine(rock, c.X - 22, c.Y + 23, c.X - 10, c.Y + 26);
        g.DrawLine(rock, c.X + 14, c.Y + 25, c.X + 27, c.Y + 20);
    }

    private static void DrawFog(Graphics g, PointF c)
    {
        using var mist = new SolidBrush(Color.FromArgb(105, 9, 10, 28));
        g.FillEllipse(mist, c.X - 14, c.Y - 17, 28, 24);
        g.FillEllipse(mist, c.X - 20, c.Y - 11, 23, 18);
        g.FillEllipse(mist, c.X - 2, c.Y - 14, 22, 19);
        using var glimmer = new SolidBrush(Color.FromArgb(110, 168, 141, 255));
        g.FillEllipse(glimmer, c.X - 1.5f, c.Y - 2, 3, 3);
    }

    private static void DrawBeacon(Graphics g, PointF c)
    {
        using var glow = new SolidBrush(Color.FromArgb(58, 89, 179, 255));
        g.FillEllipse(glow, c.X - 23, c.Y - 27, 46, 45);
        using var arch = new Pen(Color.FromArgb(225, 120, 204, 255), 3f);
        g.DrawArc(arch, c.X - 11, c.Y - 19, 22, 27, 180, 180);
        g.DrawLine(arch, c.X - 11, c.Y - 5, c.X - 11, c.Y + 10);
        g.DrawLine(arch, c.X + 11, c.Y - 5, c.X + 11, c.Y + 10);
        using var beam = new Pen(Color.FromArgb(178, 136, 216, 255), 1.5f);
        g.DrawLine(beam, c.X, c.Y - 14, c.X, c.Y - 23);
        using var light = new SolidBrush(Color.FromArgb(245, 208, 237, 255));
        g.FillEllipse(light, c.X - 3, c.Y - 25, 6, 6);
    }

    private static void DrawCrystal(Graphics g, PointF c)
    {
        using var glow = new SolidBrush(Color.FromArgb(45, 149, 111, 255));
        g.FillEllipse(glow, c.X - 21, c.Y - 24, 42, 42);
        using var crystal = new SolidBrush(Color.FromArgb(206, 185, 138, 255));
        g.FillPolygon(crystal, new[] { new PointF(c.X, c.Y - 19), new PointF(c.X + 9, c.Y - 4), new PointF(c.X + 4, c.Y + 11), new PointF(c.X - 5, c.Y + 8), new PointF(c.X - 9, c.Y - 4) });
        using var facet = new Pen(Color.FromArgb(210, 225, 211, 255), 1.2f);
        g.DrawLine(facet, c.X, c.Y - 18, c.X, c.Y + 9);
        g.DrawLine(facet, c.X - 8, c.Y - 4, c.X + 8, c.Y - 4);
    }

    private static void DrawCore(Graphics g, PointF c)
    {
        using var aura = new SolidBrush(Color.FromArgb(72, 110, 145, 255));
        g.FillEllipse(aura, c.X - 26, c.Y - 28, 52, 52);
        using var core = new LinearGradientBrush(new RectangleF(c.X - 10, c.Y - 20, 20, 31), Color.FromArgb(255, 232, 250, 255), Color.FromArgb(125, 112, 255), LinearGradientMode.ForwardDiagonal);
        g.FillPolygon(core, new[] { new PointF(c.X, c.Y - 21), new PointF(c.X + 10, c.Y - 5), new PointF(c.X, c.Y + 11), new PointF(c.X - 10, c.Y - 5) });
    }

    private static void DrawRift(Graphics g, PointF c)
    {
        using var aura = new SolidBrush(Color.FromArgb(60, 187, 74, 255));
        g.FillEllipse(aura, c.X - 23, c.Y - 27, 46, 47);
        using var portal = new Pen(Color.FromArgb(235, 204, 121, 255), 2.5f);
        g.DrawEllipse(portal, c.X - 10, c.Y - 19, 20, 33);
        using var inner = new Pen(Color.FromArgb(169, 103, 217, 255), 1.3f);
        g.DrawEllipse(inner, c.X - 5, c.Y - 14, 10, 23);
        using var spark = new SolidBrush(Color.FromArgb(245, 226, 200, 255));
        g.FillEllipse(spark, c.X - 2, c.Y - 3, 4, 4);
    }

    private static void DrawRune(Graphics g, PointF c)
    {
        using var rune = new Pen(Color.FromArgb(118, 164, 148, 255), 1.4f);
        g.DrawEllipse(rune, c.X - 8, c.Y - 11, 16, 19);
        g.DrawLine(rune, c.X - 5, c.Y + 1, c.X + 5, c.Y + 1);
        g.DrawLine(rune, c.X, c.Y - 7, c.X, c.Y + 7);
    }

    private static void DrawHunter(Graphics g, PointF c)
    {
        using var glow = new SolidBrush(Color.FromArgb(66, 167, 117, 255));
        g.FillEllipse(glow, c.X - 16, c.Y - 23, 32, 36);
        using var silhouette = new SolidBrush(Color.FromArgb(246, 8, 8, 22));
        g.FillEllipse(silhouette, c.X - 4, c.Y - 19, 8, 9);
        g.FillPolygon(silhouette, new[] { new PointF(c.X - 6, c.Y - 10), new PointF(c.X + 6, c.Y - 10), new PointF(c.X + 9, c.Y + 9), new PointF(c.X, c.Y + 14), new PointF(c.X - 9, c.Y + 9) });
        using var blade = new Pen(Color.FromArgb(232, 214, 174, 255), 2.3f);
        g.DrawLine(blade, c.X + 6, c.Y - 2, c.X + 18, c.Y - 17);
        using var aura = new Pen(Color.FromArgb(95, 154, 108, 255), 1.4f);
        g.DrawEllipse(aura, c.X - 28, c.Y - 27, 56, 51);
    }

    private void DrawHoverReadout(Graphics g)
    {
        if (_session is null || !_hovered.HasValue) return;
        var cell = _hovered.Value;
        bool visited = _session.Mapa.FueVisitada(cell.Row, cell.Column);
        string text = visited ? $"ZONA {cell.Row + 1}:{cell.Column + 1}" : "ZONA SIN EXPLORAR";
        using var background = new SolidBrush(Color.FromArgb(215, 7, 10, 28));
        using var edge = new Pen(Color.FromArgb(125, Violet));
        var rectangle = new Rectangle(13, 12, 175, 25);
        g.FillRectangle(background, rectangle);
        g.DrawRectangle(edge, rectangle);
        using var font = new Font("Bahnschrift", 8.5f, FontStyle.Bold);
        TextRenderer.DrawText(g, text, font, new Point(22, 17), Color.FromArgb(225, 216, 255));
    }
}

