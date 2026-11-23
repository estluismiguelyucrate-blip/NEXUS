using System.Drawing.Drawing2D;
using Nexus;

namespace nexus_visual;

public partial class Form1 : Form
{
    private static readonly Color Ink = Color.FromArgb(5, 7, 18);
    private static readonly Color Surface = Color.FromArgb(12, 14, 32);
    private static readonly Color Violet = Color.FromArgb(151, 105, 255);

    public Form1()
    {
        InitializeComponent();
        BuildLauncher();
    }

    private void BuildLauncher()
    {
        Text = "NEXUS // EL SISTEMA";
        MinimumSize = new Size(960, 620);
        Size = new Size(1120, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Ink;
        ForeColor = Color.White;
        Font = new Font("Bahnschrift", 10f);

        var menu = new Panel { Dock = DockStyle.Left, Width = 340, BackColor = Color.FromArgb(7, 8, 21), Padding = new Padding(24) };
        menu.Paint += (_, e) =>
        {
            using var border = new Pen(Color.FromArgb(120, Violet), 1.2f);
            e.Graphics.DrawLine(border, menu.Width - 1, 0, menu.Width - 1, menu.Height);
        };
        Controls.Add(menu);

        menu.Controls.Add(Label("NEXUS", 34, Color.White, true, 4, 32, 280, 48));
        menu.Controls.Add(Label("SISTEMA DEL MONARCA", 10, Violet, true, 7, 84, 280, 22));
        menu.Controls.Add(Label("Tres portales.\nUn cazador. Ninguna salida.", 13, Color.White, true, 7, 145, 285, 58));
        menu.Controls.Add(Label("Explora los territorios y reclama el núcleo.\nSobrevive. Evoluciona.", 9.5f, Color.FromArgb(168, 180, 213), false, 7, 232, 290, 45));

        var start = Button("▶   ENTRAR AL SISTEMA", true, 7, 320, 285, 46);
        start.Click += (_, _) => LaunchGame();
        menu.Controls.Add(start);
        var help = Button("ⓘ   GUÍA DEL CAZADOR", false, 7, 380, 285, 42);
        help.Click += (_, _) => ShowGuide();
        menu.Controls.Add(help);
        var exit = Button("↪   CERRAR", false, 7, 434, 285, 42);
        exit.Click += (_, _) => Close();
        menu.Controls.Add(exit);

        var scene = new Panel { Dock = DockStyle.Fill, BackColor = Ink };
        scene.Paint += (_, e) => DrawPortalScene(e.Graphics, scene.ClientRectangle);
        Controls.Add(scene);
        scene.SendToBack();
        menu.BringToFront();
    }

    private void LaunchGame()
    {
        Hide();
        using var game = new MainForm();
        game.ShowDialog(this);
        Close();
    }

    private void ShowGuide()
    {
        using var dialog = new Form
        {
            Text = "NEXUS // GUÍA DEL CAZADOR",
            Size = new Size(470, 280),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Surface,
            ForeColor = Color.White,
            Font = new Font("Bahnschrift", 10f)
        };
        dialog.Controls.Add(Label("PROTOCOLO DE INCURSIÓN", 16, Violet, true, 24, 24, 390, 30));
        dialog.Controls.Add(Label("WASD o flechas para moverte.\nExplora los nodos adyacentes, recoge suministros,\nsupera al Guardián y vuelve a la salida.", 10, Color.FromArgb(215, 220, 241), false, 24, 72, 395, 94));
        var close = Button("ENTENDIDO", true, 24, 188, 390, 40);
        close.Click += (_, _) => dialog.Close();
        dialog.Controls.Add(close);
        dialog.ShowDialog(this);
    }

    private static Label Label(string text, float size, Color color, bool bold, int x, int y, int width, int height) => new()
    {
        Text = text,
        Font = new Font("Bahnschrift", size, bold ? FontStyle.Bold : FontStyle.Regular),
        ForeColor = color,
        BackColor = Color.Transparent,
        Left = x,
        Top = y,
        Width = width,
        Height = height
    };

    private static Button Button(string text, bool primary, int x, int y, int width, int height)
    {
        var button = new Button
        {
            Text = text,
            Left = x,
            Top = y,
            Width = width,
            Height = height,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Color.FromArgb(92, 56, 191) : Color.FromArgb(16, 20, 43),
            ForeColor = Color.White,
            Font = new Font("Bahnschrift", 9.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = primary ? Color.FromArgb(201, 174, 255) : Color.FromArgb(78, 72, 132);
        button.FlatAppearance.BorderSize = primary ? 2 : 1;
        button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(126, 80, 234) : Color.FromArgb(37, 35, 76);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(69, 44, 148);
        return button;
    }

    private static void DrawPortalScene(Graphics graphics, Rectangle bounds)
    {
        if (bounds.Width < 1 || bounds.Height < 1) return;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (var sky = new LinearGradientBrush(bounds, Color.FromArgb(41, 27, 91), Ink, LinearGradientMode.Vertical))
            graphics.FillRectangle(sky, bounds);

        var random = new Random(17);
        for (int i = 0; i < 130; i++)
        {
            int x = random.Next(bounds.Width), y = random.Next(bounds.Height * 3 / 4);
            using var star = new SolidBrush(Color.FromArgb(random.Next(50, 170), 205, 197, 255));
            graphics.FillEllipse(star, x, y, 2, 2);
        }

        int centerX = bounds.Width * 57 / 100, centerY = bounds.Height * 53 / 100;
        int rx = Math.Max(70, Math.Min(bounds.Width, bounds.Height) / 5), ry = rx * 3 / 2;
        var portal = new Rectangle(centerX - rx, centerY - ry, rx * 2, ry * 2);
        using (var aura = new SolidBrush(Color.FromArgb(23, 142, 82, 255)))
            graphics.FillEllipse(aura, portal.X - 42, portal.Y - 42, portal.Width + 84, portal.Height + 84);
        using (var core = new LinearGradientBrush(portal, Color.FromArgb(190, 48, 21, 117), Color.FromArgb(235, 5, 8, 26), LinearGradientMode.ForwardDiagonal))
            graphics.FillEllipse(core, portal);
        using (var rim = new Pen(Color.FromArgb(230, 175, 126, 255), 4)) graphics.DrawEllipse(rim, portal);
        using (var inner = new Pen(Color.FromArgb(150, 77, 170, 255), 2)) graphics.DrawEllipse(inner, portal.X + 12, portal.Y + 12, portal.Width - 24, portal.Height - 24);

        int floor = bounds.Height * 76 / 100;
        using (var ruins = new SolidBrush(Color.FromArgb(231, 7, 8, 22)))
        {
            for (int i = 0; i < 11; i++)
            {
                int width = 35 + i % 4 * 11, height = bounds.Height / 5 + i % 3 * 21;
                int x = i * bounds.Width / 10, y = floor - height;
                graphics.FillRectangle(ruins, x, y, width, height);
                graphics.FillPolygon(ruins, new[] { new Point(x - 7, y + 8), new Point(x + width / 2, y - 18), new Point(x + width + 7, y + 8) });
            }
            graphics.FillPolygon(ruins, new[] { new Point(0, floor + 36), new Point(bounds.Width / 5, floor - 14), new Point(bounds.Width * 2 / 5, floor + 28), new Point(bounds.Width * 3 / 5, floor - 20), new Point(bounds.Width, floor + 30), new Point(bounds.Width, bounds.Height), new Point(0, bounds.Height) });
        }
        using var horizon = new Pen(Color.FromArgb(128, Violet), 1.5f);
        graphics.DrawLine(horizon, 0, floor + 22, bounds.Width, floor + 22);
    }
}

