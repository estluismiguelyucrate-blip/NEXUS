using System.Drawing.Drawing2D;
using System.Diagnostics;

namespace Nexus;

/// <summary>A clickable, illustrated dungeon scene mapped to the existing 4 × 5 game grid.</summary>
public sealed class DungeonMapScene : Control
{
    private static readonly Color Violet = Color.FromArgb(151, 105, 255);
    private static readonly Color Blue = Color.FromArgb(91, 160, 255);
    private GameSession? _session;
    private Action<int, int>? _move;
    private Action<string>? _action;
    private readonly HashSet<Keys> _keys = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private Bitmap? _terrainCache;
    private PointF _player;
    private PointF _enemyPosition;
    private float _facing = 1;
    private float _walkPhase;
    private float _attackFlash;
    private float _dashCooldown;
    private float _enemyAttackCooldown = 1.1f;
    private PointF? _destination;
    private int _lastZoneRow = -1, _lastZoneColumn = -1;

    public DungeonMapScene()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.FromArgb(5, 7, 18);
        Cursor = Cursors.Hand;
        TabStop = true;
        _timer.Tick += (_, _) => Step();
        _timer.Start();
        VisibleChanged += (_, _) => { if (Visible) _timer.Start(); else _timer.Stop(); };
        Disposed += (_, _) => { _timer.Dispose(); _terrainCache?.Dispose(); };
    }

    public void Bind(GameSession session, Action<int, int> move, Action<string> action)
    {
        _session = session;
        _move = move;
        _action = action;
        RebuildTerrainCache();
        _lastZoneRow = session.Mapa.Fila;
        _lastZoneColumn = session.Mapa.Columna;
        _player = Center(_lastZoneRow, _lastZoneColumn);
        _enemyPosition = _player;
        Focus();
        Invalidate();
    }

    public void RefreshScene() => Invalidate();

    public void SetKey(Keys key, bool pressed)
    {
        if (pressed)
        {
            if (!_keys.Add(key)) return;
            if (key is Keys.W or Keys.A or Keys.S or Keys.D or Keys.Up or Keys.Down or Keys.Left or Keys.Right) _destination = null;
            if (key is Keys.Space or Keys.J) Attack();
            if (key == Keys.Q && _session?.EnemigoActual is not null) _action?.Invoke(_session.Escapar());
            if (key == Keys.ShiftKey && _dashCooldown <= 0) _dashCooldown = .55f;
        }
        else _keys.Remove(key);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        if (Width < 1 || Height < 1) return;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        DrawBackdrop(g);
        if (_session is null) return;
        PointF camera = Camera();
        GraphicsState state = g.Save();
        g.TranslateTransform(-camera.X, -camera.Y);
        if (_terrainCache is not null) g.DrawImageUnscaled(_terrainCache, 0, 0);
        DrawRoutes(g);
        for (int row = 0; row < _session.Mapa.Filas; row++)
            for (int column = 0; column < _session.Mapa.Columnas; column++)
                DrawZone(g, row, column);
        if (_destination is PointF destination) DrawDestination(g, destination);
        DrawPlayer(g);
        if (_session.EnemigoActual is not null) DrawEnemy(g, _enemyPosition);
        g.Restore(state);
        DrawHoverReadout(g);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Cursor = Cursors.Cross;
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        Cursor = Cursors.Default;
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        Focus();
        if (_session is null || e.Button != MouseButtons.Left) return;
        PointF camera = Camera();
        float worldWidth = _session.Mapa.Columnas * 280, worldHeight = _session.Mapa.Filas * 220;
        var target = new PointF(Math.Clamp(e.X + camera.X, 12, worldWidth - 12), Math.Clamp(e.Y + camera.Y, 12, worldHeight - 12));
        if (!IsBlocked(target.X, target.Y)) _destination = target;
        Invalidate();
    }

    private PointF Center(int row, int column)
    {
        const float roomWidth = 280, roomHeight = 220;
        return new PointF(column * roomWidth + roomWidth / 2, row * roomHeight + roomHeight / 2);
    }

    private void Step()
    {
        if (_session is null || IsDisposed) return;
        float delta = Math.Clamp((float)_clock.Elapsed.TotalSeconds, .001f, .05f);
        _clock.Restart();
        _attackFlash = Math.Max(0, _attackFlash - delta);
        _dashCooldown = Math.Max(0, _dashCooldown - delta);
        if (_session.EnemigoActual is not null) UpdateEnemy(delta);

        float dx = Axis(Keys.A, Keys.Left, Keys.D, Keys.Right);
        float dy = Axis(Keys.W, Keys.Up, Keys.S, Keys.Down);
        if (dx == 0 && dy == 0 && _destination is PointF target)
        {
            float tx = target.X - _player.X, ty = target.Y - _player.Y;
            float distance = MathF.Sqrt(tx * tx + ty * ty);
            if (distance < 8) _destination = null;
            else { dx = tx / distance; dy = ty / distance; }
        }
        if (dx == 0 && dy == 0)
        {
            if (_session.EnemigoActual is not null) Invalidate();
            return;
        }
        if (dx != 0 && dy != 0) { dx *= .7071f; dy *= .7071f; }
        if (dx != 0) _facing = dx;
        float speed = _dashCooldown > .30f ? 570 : 245;
        _walkPhase += delta * speed / 18f;

        MoveAxis(dx * speed * delta, 0);
        MoveAxis(0, dy * speed * delta);
        Invalidate();
    }

    private float Axis(Keys negA, Keys negB, Keys posA, Keys posB) =>
        (_keys.Contains(negA) || _keys.Contains(negB) ? -1 : 0) + (_keys.Contains(posA) || _keys.Contains(posB) ? 1 : 0);

    private void MoveAxis(float dx, float dy)
    {
        if (_session is null) return;
        const float roomWidth = 280, roomHeight = 220;
        float nx = Math.Clamp(_player.X + dx, 12, _session.Mapa.Columnas * roomWidth - 12);
        float ny = Math.Clamp(_player.Y + dy, 12, _session.Mapa.Filas * roomHeight - 12);
        if (IsBlocked(nx, ny)) return;
        int column = Math.Clamp((int)(nx / roomWidth), 0, _session.Mapa.Columnas - 1);
        int row = Math.Clamp((int)(ny / roomHeight), 0, _session.Mapa.Filas - 1);
        if (row != _lastZoneRow || column != _lastZoneColumn)
        {
            // Cross one doorway at a time; diagonals cannot skip a room.
            if (Math.Abs(row - _lastZoneRow) + Math.Abs(column - _lastZoneColumn) != 1)
            {
                if (dx != 0) nx = _player.X; else ny = _player.Y;
                column = Math.Clamp((int)(nx / roomWidth), 0, _session.Mapa.Columnas - 1);
                row = Math.Clamp((int)(ny / roomHeight), 0, _session.Mapa.Filas - 1);
            }
            if (row != _lastZoneRow || column != _lastZoneColumn)
            {
                _move?.Invoke(row, column);
                if (_session.Mapa.Fila == row && _session.Mapa.Columna == column)
                {
                    _lastZoneRow = row; _lastZoneColumn = column;
                    _enemyPosition = Center(row, column);
                    _enemyAttackCooldown = 1.1f;
                }
                else
                {
                    if (dx != 0) nx = dx > 0 ? (_lastZoneColumn + 1) * roomWidth - 13 : _lastZoneColumn * roomWidth + 13;
                    if (dy != 0) ny = dy > 0 ? (_lastZoneRow + 1) * roomHeight - 13 : _lastZoneRow * roomHeight + 13;
                }
            }
        }
        _player = new PointF(nx, ny);
    }

    private void Attack()
    {
        if (_session?.EnemigoActual is null || _attackFlash > 0) return;
        _attackFlash = .3f;
        _action?.Invoke(_session.AtacarEnTiempoReal());
        if (_session.EnemigoActual is null) _enemyAttackCooldown = 0;
    }

    private void UpdateEnemy(float delta)
    {
        if (_session?.EnemigoActual is null || _session.EnemigoActual is Robot) return;
        float dx = _player.X - _enemyPosition.X, dy = _player.Y - _enemyPosition.Y;
        float distance = MathF.Sqrt(dx * dx + dy * dy);
        if (distance > 45)
        {
            float step = Math.Min(distance - 44, 105 * delta);
            float nextX = _enemyPosition.X + dx / distance * step;
            float nextY = _enemyPosition.Y + dy / distance * step;
            if (!IsBlocked(nextX, _enemyPosition.Y)) _enemyPosition.X = nextX;
            if (!IsBlocked(_enemyPosition.X, nextY)) _enemyPosition.Y = nextY;
            return;
        }
        _enemyAttackCooldown -= delta;
        if (_enemyAttackCooldown > 0) return;
        _enemyAttackCooldown = 1.25f;
        string result = _dashCooldown > 0 ? "Esquivaste el golpe del enemigo." : _session.RecibirAtaqueEnTiempoReal();
        if (!string.IsNullOrWhiteSpace(result)) _action?.Invoke(result);
    }

    private PointF Camera()
    {
        float worldWidth = _session!.Mapa.Columnas * 280, worldHeight = _session.Mapa.Filas * 220;
        float x = worldWidth <= Width ? -(Width - worldWidth) / 2f : Math.Clamp(_player.X - Width / 2f, 0, worldWidth - Width);
        float y = worldHeight <= Height ? -(Height - worldHeight) / 2f : Math.Clamp(_player.Y - Height / 2f, 0, worldHeight - Height);
        return new PointF(x, y);
    }

    private void DrawTerrain(Graphics g)
    {
        if (_session is null) return;
        const int roomWidth = 280, roomHeight = 220;
        int width = roomWidth * _session.Mapa.Columnas, height = roomHeight * _session.Mapa.Filas;
        Color tint = _session.MapaId switch
        {
            1 => Color.FromArgb(56, 48, 37),
            2 => Color.FromArgb(26, 72, 105),
            _ => Color.FromArgb(45, 37, 85)
        };
        using var floor = new LinearGradientBrush(new Rectangle(0, 0, width, height), Color.FromArgb(105, tint), Color.FromArgb(17, 5, 8, 21), LinearGradientMode.Vertical);
        g.FillRectangle(floor, 0, 0, width, height);
        var random = new Random(931 + _session.MapaId * 91);
        using var stone = new SolidBrush(Color.FromArgb(92, 31, 27, 48));
        using var glint = new SolidBrush(Color.FromArgb(66, 176, 138, 255));
        for (int i = 0; i < 420; i++)
        {
            int x = random.Next(width), y = random.Next(height), s = random.Next(3, 13);
            g.FillEllipse(stone, x, y, s, s / 2f);
            if (i % 6 == 0) g.FillEllipse(glint, x + 2, y + 2, 2, 2);
        }
        for (int row = 0; row < _session.Mapa.Filas; row++)
            for (int col = 0; col < _session.Mapa.Columnas; col++)
            {
                PointF c = Center(row, col);
                if (_session.MapaId == 1) DrawTree(g, c.X + ((row + col) % 2 == 0 ? -92 : 94), c.Y - 38);
                else if (_session.MapaId == 2) DrawTemple(g, c.X + 94, c.Y - 25);
                else DrawRuin(g, c.X - 94, c.Y - 28);
            }
    }

    private void RebuildTerrainCache()
    {
        if (_session is null) return;
        _terrainCache?.Dispose();
        _terrainCache = new Bitmap(_session.Mapa.Columnas * 280, _session.Mapa.Filas * 220);
        using Graphics terrain = Graphics.FromImage(_terrainCache);
        terrain.SmoothingMode = SmoothingMode.AntiAlias;
        DrawTerrain(terrain);
    }

    private bool IsBlocked(float x, float y)
    {
        if (_session is null) return false;
        for (int row = 0; row < _session.Mapa.Filas; row++)
            for (int col = 0; col < _session.Mapa.Columnas; col++)
            {
                PointF center = Center(row, col);
                if (_session.MapaId == 0)
                {
                    float ruinX = center.X - 94, ruinY = center.Y - 28;
                    if (Math.Abs(x - ruinX) < 35 && Math.Abs(y - ruinY) < 34) return true;
                }
                else if (_session.MapaId == 1)
                {
                    float treeX = center.X + ((row + col) % 2 == 0 ? -92 : 94), treeY = center.Y - 38;
                    float dx = x - treeX, dy = y - treeY;
                    if (dx * dx + dy * dy < 48 * 48) return true;
                }
                else
                {
                    float templeX = center.X + 94, templeY = center.Y - 25;
                    if (Math.Abs(x - (templeX - 28)) < 10 && y > templeY - 17 && y < templeY + 40) return true;
                    if (Math.Abs(x - (templeX + 28)) < 10 && y > templeY - 17 && y < templeY + 40) return true;
                }
            }
        return false;
    }

    private static void DrawRuin(Graphics g, float x, float y)
    {
        using var ruin = new SolidBrush(Color.FromArgb(218, 15, 12, 35));
        using var edge = new Pen(Color.FromArgb(91, 135, 110, 189), 1.6f);
        g.FillPolygon(ruin, new[] { new PointF(x - 44, y + 49), new PointF(x - 34, y - 36), new PointF(x - 17, y - 48), new PointF(x - 4, y - 26), new PointF(x + 1, y + 46) });
        g.FillPolygon(ruin, new[] { new PointF(x + 5, y + 47), new PointF(x + 11, y - 21), new PointF(x + 31, y - 43), new PointF(x + 45, y - 11), new PointF(x + 49, y + 48) });
        g.DrawLine(edge, x - 33, y - 33, x - 18, y - 43);
        g.DrawLine(edge, x + 12, y - 18, x + 30, y - 38);
    }

    private static void DrawTree(Graphics g, float x, float y)
    {
        using var trunk = new SolidBrush(Color.FromArgb(220, 47, 23, 38));
        using var crownShadow = new SolidBrush(Color.FromArgb(125, 12, 19, 30));
        using var crown = new SolidBrush(Color.FromArgb(205, 42, 47, 58));
        g.FillEllipse(crownShadow, x - 42, y - 24, 84, 76);
        g.FillRectangle(trunk, x - 7, y - 8, 14, 46);
        g.FillEllipse(crown, x - 34, y - 57, 68, 70);
        g.FillEllipse(crown, x - 49, y - 39, 50, 51);
        g.FillEllipse(crown, x - 1, y - 42, 52, 54);
        using var leaf = new Pen(Color.FromArgb(128, 166, 83, 116), 2f);
        g.DrawArc(leaf, x - 30, y - 53, 60, 52, 20, 125);
    }

    private static void DrawTemple(Graphics g, float x, float y)
    {
        using var stone = new SolidBrush(Color.FromArgb(217, 10, 29, 48));
        using var edge = new Pen(Color.FromArgb(112, 103, 189, 226), 1.6f);
        g.FillRectangle(stone, x - 35, y - 17, 13, 57); g.FillRectangle(stone, x + 22, y - 17, 13, 57);
        g.FillPolygon(stone, new[] { new PointF(x - 45, y - 17), new PointF(x, y - 52), new PointF(x + 45, y - 17) });
        g.DrawLine(edge, x - 40, y - 15, x, y - 46); g.DrawLine(edge, x, y - 46, x + 40, y - 15);
        using var water = new Pen(Color.FromArgb(126, 74, 160, 220), 2f);
        g.DrawArc(water, x - 56, y + 37, 112, 28, 0, 180);
    }

    private void DrawPlayer(Graphics g)
    {
        PointF p = _player;
        bool moving = _keys.Any(k => k is Keys.W or Keys.A or Keys.S or Keys.D or Keys.Up or Keys.Down or Keys.Left or Keys.Right);
        float bob = moving ? MathF.Sin(_walkPhase) * 2.4f : 0;
        using var shadow = new SolidBrush(Color.FromArgb(140, 0, 0, 8));
        g.FillEllipse(shadow, p.X - 22, p.Y + 14, 44, 14);
        using var aura = new SolidBrush(Color.FromArgb(78, 138, 91, 255));
        g.FillEllipse(aura, p.X - 29, p.Y - 36 + bob, 58, 58);
        using var cloak = new SolidBrush(Color.FromArgb(250, 7, 8, 24));
        using var edge = new Pen(Color.FromArgb(174, 161, 119, 255), 1.5f);
        g.FillEllipse(cloak, p.X - 9, p.Y - 34 + bob, 18, 19);
        var coat = new[] { new PointF(p.X - 10, p.Y - 17 + bob), new PointF(p.X + 10, p.Y - 17 + bob), new PointF(p.X + 20, p.Y + 13 + bob), new PointF(p.X, p.Y + 19 + bob), new PointF(p.X - 20, p.Y + 13 + bob) };
        g.FillPolygon(cloak, coat); g.DrawPolygon(edge, coat);
        float swing = _attackFlash > 0 ? (float)Math.Sin((_attackFlash / .3) * Math.PI) * 21 : 0;
        using var bladeGlow = new Pen(Color.FromArgb(70, Violet), 7f);
        using var blade = new Pen(Color.FromArgb(230, 211, 190, 255), 2.2f);
        float handX = p.X + _facing * 7, handY = p.Y - 2 + bob;
        g.DrawLine(bladeGlow, handX, handY, handX + _facing * (17 + swing), handY - 15 - swing * .4f);
        g.DrawLine(blade, handX, handY, handX + _facing * (17 + swing), handY - 15 - swing * .4f);
    }

    private void DrawBackdrop(Graphics g)
    {
        var bounds = ClientRectangle;
        int mapId = _session?.MapaId ?? 0;
        Color skyTop = mapId switch { 1 => Color.FromArgb(73, 22, 47), 2 => Color.FromArgb(15, 57, 87), _ => Color.FromArgb(42, 28, 91) };
        Color atmosphere = mapId switch { 1 => Color.FromArgb(113, 52, 81), 2 => Color.FromArgb(72, 163, 201), _ => Color.FromArgb(150, 109, 255) };
        using (var sky = new LinearGradientBrush(bounds, skyTop, Color.FromArgb(4, 7, 20), LinearGradientMode.Vertical))
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
        using (var moonGlow = new SolidBrush(Color.FromArgb(35, atmosphere)))
            g.FillEllipse(moonGlow, moonX - moonRadius * 2, moonY - moonRadius * 2, moonRadius * 4, moonRadius * 4);
        using (var moon = new LinearGradientBrush(new Rectangle(moonX - moonRadius, moonY - moonRadius, moonRadius * 2, moonRadius * 2), Color.FromArgb(205, 207, 208, 255), Color.FromArgb(105, atmosphere), LinearGradientMode.ForwardDiagonal))
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

        using (var mist = new SolidBrush(Color.FromArgb(28, atmosphere)))
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
        using var shadow = new Pen(Color.FromArgb(185, 3, 5, 17), 58f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var stone = new Pen(Color.FromArgb(known ? 170 : 112, 37, 31, 66), 47f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var path = new Pen(Color.FromArgb(known ? 210 : 145, 30, 28, 55), 34f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var energy = new Pen(Color.FromArgb(known ? 195 : 92, known ? 151 : 113, known ? 112 : 100, 231), known ? 2.2f : 1.4f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            DashStyle = known ? DashStyle.Solid : DashStyle.Dot
        };
        g.DrawLine(shadow, from, to);
        g.DrawLine(stone, from, to);
        g.DrawLine(path, from, to);
        g.DrawLine(energy, from, to);
    }

    private static void DrawDestination(Graphics g, PointF target)
    {
        float pulse = 5 + MathF.Abs(MathF.Sin(Environment.TickCount64 / 180f)) * 4;
        using var glow = new SolidBrush(Color.FromArgb(45, 115, 214, 255));
        using var ring = new Pen(Color.FromArgb(220, 177, 228, 255), 2f);
        g.FillEllipse(glow, target.X - 22, target.Y - 22, 44, 44);
        g.DrawEllipse(ring, target.X - pulse, target.Y - pulse, pulse * 2, pulse * 2);
        g.DrawLine(ring, target.X - 14, target.Y, target.X + 14, target.Y);
        g.DrawLine(ring, target.X, target.Y - 14, target.X, target.Y + 14);
    }

    private void DrawZone(Graphics g, int row, int column)
    {
        if (_session is null) return;
        PointF center = Center(row, column);
        bool current = row == _session.Mapa.Fila && column == _session.Mapa.Columna;
        bool visited = _session.Mapa.FueVisitada(row, column);
        bool adjacent = IsAdjacentToCurrent(row, column);
        if (!visited && !adjacent) return;
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

        PointF[] island = Island(center, 76, 40);
        PointF[] shadow = Island(new PointF(center.X, center.Y + 15), 80, 43);
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
        using (var rim = new Pen(current ? Color.FromArgb(238, 199, 161, 255) : Color.FromArgb(visited ? 130 : 53, 119, 104, 189), current ? 2.5f : 1.3f))
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

    private static RectangleF ZoneBounds(PointF c) => new(c.X - 72, c.Y - 43, 144, 86);

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

    private void DrawEnemy(Graphics g, PointF c)
    {
        using var aura = new SolidBrush(Color.FromArgb(83, 185, 40, 221));
        g.FillEllipse(aura, c.X - 27, c.Y - 31, 54, 56);
        using var shadow = new SolidBrush(Color.FromArgb(245, 10, 5, 23));
        g.FillEllipse(shadow, c.X - 12, c.Y - 28, 24, 23);
        g.FillPolygon(shadow, new[] { new PointF(c.X - 20, c.Y - 9), new PointF(c.X + 20, c.Y - 9), new PointF(c.X + 25, c.Y + 21), new PointF(c.X, c.Y + 12), new PointF(c.X - 25, c.Y + 21) });
        using var eyes = new Pen(Color.FromArgb(244, 255, 75, 200), 2.8f);
        g.DrawLine(eyes, c.X - 8, c.Y - 17, c.X - 3, c.Y - 15);
        g.DrawLine(eyes, c.X + 3, c.Y - 15, c.X + 8, c.Y - 17);
        var enemy = _session?.EnemigoActual;
        if (enemy is null) return;
        RectangleF bar = new(c.X - 31, c.Y - 43, 62, 5);
        using var back = new SolidBrush(Color.FromArgb(190, 31, 13, 42)); g.FillRectangle(back, bar);
        using var fill = new SolidBrush(Color.FromArgb(232, 235, 55, 197));
        g.FillRectangle(fill, bar.X, bar.Y, bar.Width * Math.Clamp(enemy.Energia, 0, enemy.EnergiaMax) / Math.Max(1, enemy.EnergiaMax), bar.Height);
    }

    private void DrawHoverReadout(Graphics g)
    {
        if (_session is null) return;
        string text = _session.EnemigoActual is not null ? "J / ESPACIO ATACAR   ·   SHIFT ESQUIVAR   ·   Q ESCAPAR" : "WASD / CLIC MOVERSE   ·   SHIFT IMPULSO   ·   EXPLORA EL MAPA";
        using var background = new SolidBrush(Color.FromArgb(215, 7, 10, 28));
        using var edge = new Pen(Color.FromArgb(125, Violet));
        var rectangle = new Rectangle(13, 12, 310, 25);
        g.FillRectangle(background, rectangle);
        g.DrawRectangle(edge, rectangle);
        using var font = new Font("Bahnschrift", 8.5f, FontStyle.Bold);
        TextRenderer.DrawText(g, text, font, new Point(22, 17), Color.FromArgb(225, 216, 255));
    }
}

