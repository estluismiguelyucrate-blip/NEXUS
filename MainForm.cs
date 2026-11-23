using System.Drawing.Drawing2D;
using Microsoft.Data.SqlClient;

namespace Nexus;

public sealed partial class MainForm : Form
{
    private static readonly Color Fondo = Color.FromArgb(5, 7, 18);
    private static readonly Color Panel = Color.FromArgb(11, 14, 30);
    private static readonly Color Turquesa = Color.FromArgb(111, 103, 255);
    private static readonly Color TextoSuave = Color.FromArgb(166, 180, 211);
    private Image? _nexusLogo;
    private string _connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=NEXUS;Trusted_Connection=True;TrustServerCertificate=True;";
    private GameSession? _session;
    private string _nombre = "Cadete";
    private Label _status = null!;
    private Label _stats = null!;
    private Label _log = null!;
    private Label _objectives = null!;
    private Label _equipment = null!;
    private Panel _meter = null!;
    private Panel _encounterPanel = null!;
    private DungeonMapScene? _mapScene;

    public MainForm()
    {
        Text = "NEXUS // SISTEMA DEL MONARCA";
        MinimumSize = new Size(1040, 680);
        Size = new Size(1220, 760);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Fondo;
        ForeColor = Color.White;
        Font = new Font("Bahnschrift", 10F);
        string logoPath = Path.Combine(AppContext.BaseDirectory, "assets", "nexus-logo.png");
        if (File.Exists(logoPath)) _nexusLogo = Image.FromFile(logoPath);
        string iconPath = Path.Combine(AppContext.BaseDirectory, "assets", "nexus.ico");
        if (File.Exists(iconPath)) Icon = new Icon(iconPath);
        FormClosed += (_, _) => _nexusLogo?.Dispose();
        KeyPreview = true;
        KeyDown += HandleMovementKey;
        KeyUp += HandleMovementKeyUp;
        ShowMenu();
    }

    private void ShowMenu()
    {
        if (_mapScene is not null) { _mapScene.Dispose(); _mapScene = null; }
        Controls.Clear();
        var root = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=2, RowCount=1, BackColor=Fondo };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 356));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var menu = new Panel { Dock=DockStyle.Fill, BackColor=Color.FromArgb(8,7,19), Padding=new Padding(24,16,24,18) };
        menu.Paint += (_,e) =>
        {
            using var edge = new Pen(Color.FromArgb(75,153,92,255));
            e.Graphics.DrawLine(edge, menu.Width-1, 0, menu.Width-1, menu.Height);
        };
        root.Controls.Add(menu,0,0);
        if (_nexusLogo is not null)
            menu.Controls.Add(new PictureBox { Image=_nexusLogo, SizeMode=PictureBoxSizeMode.Zoom, BackColor=Color.Transparent, Left=4, Top=8, Width=menu.ClientSize.Width-8, Height=205, Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right });
        menu.Controls.Add(MakeLabel("DESPIERTA   ·   EVOLUCIONA", 8.5f, Color.FromArgb(211,194,255), true, 8, 205, 295, 20));
        menu.Controls.Add(MakeLabel("EL SISTEMA\nTE HA ELEGIDO", 20, Color.White, true, 8, 235, 300, 74));
        menu.Controls.Add(MakeLabel("Tres portales han aparecido.\nEntra al calabozo y reclama tu poder.", 9.5f, TextoSuave, false, 10, 315, 295, 42));

        var play = MenuButton("▶   INICIAR JUEGO", 365, true); play.Click += (_,_) => NewCadet(); menu.Controls.Add(play);
        var cont = MenuButton("↻   CONTINUAR PARTIDA", 409, false); cont.Click += async (_,_) => await LoadGame(); menu.Controls.Add(cont);
        var settings = MenuButton("⚙   CONFIGURACIÓN", 453, false); settings.Click += (_,_) => ConnectionSettings(); menu.Controls.Add(settings);
        var info = MenuButton("ⓘ   INFORMACIÓN", 497, false); info.Click += (_,_) => ShowInformation(); menu.Controls.Add(info);
        var exit = MenuButton("↪   SALIR", 541, false); exit.Click += (_,_) => Close(); menu.Controls.Add(exit);

        _status = MakeLabel("●  SISTEMA EN LÍNEA  ·  SQL SIN VERIFICAR", 8, Color.FromArgb(157,147,186), false, 9, menu.Height-52, 300, 20);
        _status.Anchor=AnchorStyles.Left|AnchorStyles.Bottom; menu.Controls.Add(_status);
        menu.Resize += (_,_) => { _status.Top=menu.ClientSize.Height-42; };

        var scene = new Panel { Dock=DockStyle.Fill, BackColor=Fondo, Margin=Padding.Empty };
        scene.Paint += (_,e) => DrawMenuBackdrop(e.Graphics, scene.ClientRectangle);
        root.Controls.Add(scene,1,0);

        var profile = MenuCard(scene, 24, 22, 330, 86);
        profile.Controls.Add(MakeLabel("◈", 29, Color.FromArgb(197,152,255), true, 17, 18, 48, 44));
        profile.Controls.Add(MakeLabel("NIVEL 01   /   DESPERTAR", 8.5f, Color.FromArgb(204,184,255), true, 70, 12, 235, 19));
        profile.Controls.Add(MakeLabel("CAZADOR  ·  SIN REGISTRO", 10, Color.White, true, 70, 33, 235, 22));
        profile.Controls.Add(MakeLabel("ENERGÍA", 7.5f, TextoSuave, false, 70, 59, 56, 15));
        profile.Controls.Add(MakeLabel("100 / 100", 7.5f, Color.White, true, 226, 59, 78, 15));
        profile.Paint += (_,e) =>
        {
            using var bar = new SolidBrush(Color.FromArgb(45,37,69)); e.Graphics.FillRectangle(bar,70,77,233,3);
            using var fill = new LinearGradientBrush(new Rectangle(70,77,233,3),Color.FromArgb(162,91,255),Color.FromArgb(74,147,255),LinearGradientMode.Horizontal); e.Graphics.FillRectangle(fill,70,77,233,3);
        };

        var quest = MenuCard(scene, Math.Max(24,scene.ClientSize.Width-296), 25, 270, 104, true);
        quest.Anchor=AnchorStyles.Top|AnchorStyles.Right;
        quest.Controls.Add(MakeLabel("▣  MISIÓN DEL SISTEMA", 9, Color.FromArgb(255,208,107), true, 15, 12, 235, 20));
        quest.Controls.Add(MakeLabel("Selecciona un portal", 9, Color.White, true, 15, 38, 240, 21));
        quest.Controls.Add(MakeLabel("Derrota al jefe del calabozo y\nregresa al punto de extracción.", 8, TextoSuave, false, 15, 62, 240, 35));

        var caption = MakeLabel("3 PORTALES   /   UNA INCURSIÓN", 8.5f, Color.FromArgb(223,207,255), true, 28, scene.Height-177, 340, 18);
        caption.Anchor=AnchorStyles.Left|AnchorStyles.Bottom; scene.Controls.Add(caption);
        var mission = MenuCard(scene, 22, 0, 340, 126);
        mission.Anchor=AnchorStyles.Left|AnchorStyles.Bottom;
        mission.Top=scene.ClientSize.Height-153;
        mission.Controls.Add(MakeLabel("PORTAL DE RANGO E", 12, Color.White, true, 16, 10, 300, 22));
        mission.Controls.Add(MakeLabel("CALABOZO ACTIVO   ·   SECTOR 01", 8, Color.FromArgb(210,199,232), false, 16, 34, 300, 17));
        mission.Controls.Add(MakeLabel("Elige una zona y comienza la incursión.", 8.5f, TextoSuave, false, 16, 53, 302, 19));
        var enterPortal = MakeButton("ELEGIR PORTAL   ›", Color.FromArgb(104,58,213), Color.White, 15, 79, 288, 34);
        enterPortal.FlatAppearance.BorderColor = Color.FromArgb(194,151,255);
        enterPortal.FlatAppearance.BorderSize = 1;
        enterPortal.FlatAppearance.MouseOverBackColor = Color.FromArgb(138,83,242);
        enterPortal.Click += (_,_) => NewCadet();
        mission.Controls.Add(enterPortal);
        scene.Resize += (_,_) => { caption.Top=scene.ClientSize.Height-177; mission.Top=scene.ClientSize.Height-153; quest.Left=Math.Max(24,scene.ClientSize.Width-296); };

        var hint = MakeLabel("WASD / FLECHAS  ·  MOVERSE     ESC  ·  PAUSA", 7.5f, Color.FromArgb(190,179,220), false, 25, scene.ClientSize.Height-20, 420, 16);
        hint.Anchor=AnchorStyles.Left|AnchorStyles.Bottom; scene.Controls.Add(hint);
    }

    private Button MenuButton(string text, int top, bool primary)
    {
        var button=MakeButton(text, primary?Color.FromArgb(112,65,222):Color.FromArgb(19,16,38), Color.White, 4, top, 280, 38);
        button.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
        button.TextAlign=ContentAlignment.MiddleLeft;
        button.Padding=new Padding(17,0,0,0);
        button.FlatAppearance.BorderColor=primary?Color.FromArgb(219,182,255):Color.FromArgb(84,64,127);
        button.FlatAppearance.BorderSize=primary?2:1;
        button.FlatAppearance.MouseOverBackColor=primary?Color.FromArgb(145,89,255):Color.FromArgb(42,29,73);
        return button;
    }

    private Panel MenuCard(Control parent, int left, int top, int width, int height, bool gold=false)
    {
        var card=new Panel { Left=left, Top=top, Width=width, Height=height, BackColor=Color.FromArgb(224,10,8,25), Anchor=AnchorStyles.Top|AnchorStyles.Left };
        card.Paint+=(_,e)=>
        {
            using var pen=new Pen(gold?Color.FromArgb(170,255,194,91):Color.FromArgb(155,153,92,255),1.2f);
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            e.Graphics.DrawRectangle(pen,0,0,card.Width-1,card.Height-1);
            using var accent=new SolidBrush(gold?Color.FromArgb(255,194,91):Color.FromArgb(153,92,255));
            e.Graphics.FillRectangle(accent,0,0,3,card.Height);
        };
        parent.Controls.Add(card);
        return card;
    }

    private void ShowInformation()
    {
        ShowSystemMessage("Explora las Ruinas del Umbral, el Bosque Carmesí o la Ciudad Sumergida. Reúne suministros, supera al Guardián y regresa a la baliza.\n\nWASD o flechas: moverse. Shift: impulso evasivo. J o Espacio: atacar. Esc: pausa.","Guía del cazador",MessageBoxButtons.OK,MessageBoxIcon.Information);
    }

    private static void DrawMenuBackdrop(Graphics g, Rectangle bounds)
    {
        if(bounds.Width<1||bounds.Height<1)return;
        g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var sky=new LinearGradientBrush(bounds,Color.FromArgb(46,31,105),Color.FromArgb(5,5,17),LinearGradientMode.Vertical))g.FillRectangle(sky,bounds);
        var random=new Random(41);
        for(int i=0;i<170;i++)
        {
            int x=random.Next(bounds.Width),y=random.Next(Math.Max(1,bounds.Height*3/4)),s=random.Next(1,3);
            using var star=new SolidBrush(Color.FromArgb(random.Next(45,175),220,206,255));g.FillEllipse(star,x,y,s,s);
        }
        int radius=Math.Max(44,Math.Min(bounds.Width,bounds.Height)/7), moonX=(int)(bounds.Width*.69),moonY=(int)(bounds.Height*.25);
        using(var glow=new SolidBrush(Color.FromArgb(25,174,120,255)))g.FillEllipse(glow,moonX-radius*2,moonY-radius*2,radius*4,radius*4);
        using(var moon=new LinearGradientBrush(new Rectangle(moonX-radius,moonY-radius,radius*2,radius*2),Color.FromArgb(235,222,255),Color.FromArgb(99,86,177),LinearGradientMode.ForwardDiagonal))g.FillEllipse(moon,moonX-radius,moonY-radius,radius*2,radius*2);
        DrawPortal(g,(int)(bounds.Width*.45),(int)(bounds.Height*.57),Math.Max(48,Math.Min(bounds.Width,bounds.Height)/6));
        for(int i=0;i<14;i++)
        {
            int x=(i*137+50)%Math.Max(1,bounds.Width),w=36+(i*17)%45,h=bounds.Height/5+(i*43)%(Math.Max(1,bounds.Height/3));int y=bounds.Height*61/100-h;
            using var ruin=new SolidBrush(Color.FromArgb(210,12,8,35));g.FillRectangle(ruin,x,y,w,h);
            g.FillPolygon(ruin,new[]{new Point(x-8,y+8),new Point(x+w/2,y-19),new Point(x+w+8,y+8)});
            using var window=new SolidBrush(Color.FromArgb(90,172,104,255));
            for(int wy=y+24;wy<bounds.Height*60/100;wy+=34)for(int wx=x+9;wx<x+w-6;wx+=17)g.FillRectangle(window,wx,wy,4,9);
        }
        int ground=(int)(bounds.Height*.66);
        using(var hills=new SolidBrush(Color.FromArgb(242,13,8,31)))g.FillPolygon(hills,new[]{new Point(0,ground+24),new Point(bounds.Width/7,ground-53),new Point(bounds.Width/4,ground+8),new Point(bounds.Width*2/5,ground-85),new Point(bounds.Width*3/5,ground+10),new Point(bounds.Width*4/5,ground-46),new Point(bounds.Width,ground+4),new Point(bounds.Width,bounds.Height),new Point(0,bounds.Height)});
        using(var groundGlow=new Pen(Color.FromArgb(120,153,92,255),1.2f))g.DrawLine(groundGlow,0,ground+12,bounds.Width,ground+12);
        DrawMenuHunter(g,(int)(bounds.Width*.67),ground+22,Math.Min(bounds.Height/2,245));
        using(var shade=new LinearGradientBrush(bounds,Color.FromArgb(12,4,4,10),Color.FromArgb(125,4,3,14),LinearGradientMode.Vertical))g.FillRectangle(shade,bounds);
    }

    private static void DrawPortal(Graphics g,int centerX,int centerY,int radius)
    {
        var portalBounds=new Rectangle(centerX-radius,centerY-radius*3/2,radius*2,radius*3);
        using(var aura=new SolidBrush(Color.FromArgb(28,139,73,255)))g.FillEllipse(aura,portalBounds.X-radius/2,portalBounds.Y-radius/3,portalBounds.Width+radius,portalBounds.Height+radius*2/3);
        using(var interior=new LinearGradientBrush(portalBounds,Color.FromArgb(205,25,9,66),Color.FromArgb(225,5,8,25),LinearGradientMode.ForwardDiagonal))g.FillEllipse(interior,portalBounds);
        using(var rim=new Pen(Color.FromArgb(205,162,112,255),3.2f))g.DrawEllipse(rim,portalBounds);
        using(var inner=new Pen(Color.FromArgb(145,77,174,255),1.4f))g.DrawEllipse(inner,portalBounds.X+8,portalBounds.Y+8,portalBounds.Width-16,portalBounds.Height-16);
        using(var flare=new Pen(Color.FromArgb(100,189,128,255),2))
        {
            g.DrawArc(flare,portalBounds.X-5,portalBounds.Y+portalBounds.Height/5,portalBounds.Width+10,portalBounds.Height*3/5,205,108);
            g.DrawArc(flare,portalBounds.X+4,portalBounds.Y+portalBounds.Height/5,portalBounds.Width-8,portalBounds.Height*3/5,20,100);
        }
        using(var core=new LinearGradientBrush(new Rectangle(centerX-radius/5,centerY-radius,Math.Max(3,radius*2/5),radius*2),Color.FromArgb(0,180,127,255),Color.FromArgb(95,58,139,255),LinearGradientMode.Vertical))
            g.FillEllipse(core,centerX-radius/5,centerY-radius,Math.Max(3,radius*2/5),radius*2);
        using(var sparks=new SolidBrush(Color.FromArgb(185,198,151,255)))
            for(int i=0;i<10;i++)
            {
                double angle=i*Math.PI/5;
                int x=centerX+(int)(Math.Cos(angle)*(radius+6));
                int y=centerY+(int)(Math.Sin(angle)*(radius*1.35));
                g.FillEllipse(sparks,x-2,y-2,4,4);
            }
    }

    private static void DrawMenuHunter(Graphics g,int x,int y,int height)
    {
        if(height<50)return;
        int head=Math.Max(15,height/9),body=Math.Max(45,height/2);
        using var silhouette=new SolidBrush(Color.FromArgb(245,5,5,16));
        using var edge=new Pen(Color.FromArgb(190,153,92,255),2.2f);
        g.FillEllipse(silhouette,x-head/2,y-height+4,head,head+8);
        var cloak=new[]{new Point(x-head/2,y-height+head),new Point(x-head,y-height+head+18),new Point(x-body/2,y-10),new Point(x-body/4,y),new Point(x+body/3,y),new Point(x+body/2,y-14),new Point(x+head,y-height+head+18),new Point(x+head/2,y-height+head)};
        g.FillPolygon(silhouette,cloak);g.DrawLines(edge,new[]{cloak[1],cloak[2],cloak[3],cloak[4],cloak[5],cloak[6]});
        using var blade=new Pen(Color.FromArgb(225,183,106,255),3);
        g.DrawLine(blade,x+body/3,y-height/3,x+body,y-height/3-50);
        using var bladeGlow=new Pen(Color.FromArgb(65,186,94,255),9);g.DrawLine(bladeGlow,x+body/3,y-height/3,x+body,y-height/3-50);
        using var feet=new Pen(Color.FromArgb(230,6,4,17),Math.Max(8,height/17));g.DrawLine(feet,x-body/5,y-3,x-body/4,y+25);g.DrawLine(feet,x+body/5,y-3,x+body/4,y+25);
    }

    private void NewCadet()
    {
        using var dlg = CreateSystemDialog("Registro del cazador",new Size(430,390));
        var name = new TextBox { Left=35,Top=60,Width=340,Text="Cadete" };
        var age = new NumericUpDown { Left=35,Top=130,Width=130,Minimum=18,Maximum=60,Value=24 };
        var map = new ComboBox { Left=35,Top=200,Width=340,DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat };
        StyleSystemInput(name);StyleSystemInput(age);StyleSystemInput(map);
        foreach(string mapName in GameSession.NombresMapas) map.Items.Add(mapName);
        map.SelectedIndex=0;
        dlg.Controls.Add(MakeLabel("REGISTRO DEL CAZADOR", 15, Turquesa, true, 35, 20, 320, 28));
        dlg.Controls.Add(MakeLabel("Nombre",10,Color.White,false,35,42,100,20)); dlg.Controls.Add(name);
        dlg.Controls.Add(MakeLabel("Edad",10,Color.White,false,35,106,100,20)); dlg.Controls.Add(age);
        dlg.Controls.Add(MakeLabel("Territorio",10,Color.White,false,35,176,120,20)); dlg.Controls.Add(map);
        var start=MakeButton("ACEPTAR Y DESPERTAR",Turquesa,Fondo,35,260,340,46); start.Click+=(_,_)=>{try{_nombre=string.IsNullOrWhiteSpace(name.Text)?"Cadete":name.Text.Trim();_session=new GameSession();_session.Iniciar(_nombre,(int)age.Value,map.SelectedIndex);_failureReported=false;dlg.DialogResult=DialogResult.OK;dlg.Close();}catch(Exception ex){ShowSystemMessage(ex.Message,"Datos del cazador",owner:dlg);}}; dlg.Controls.Add(start);
        dlg.Controls.Add(MakeLabel("Cada portal conserva su propio mapa y progreso.",8.5f,TextoSuave,false,35,318,340,22));
        if(dlg.ShowDialog(this)==DialogResult.OK) PlayAwakeningCinematic(ShowGame);
    }

    private void PlayAwakeningCinematic(Action onComplete)
    {
        using var scene = new Form
        {
            Text = "NEXUS // EL DESPERTAR",
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(1040, 600),
            BackColor = Fondo,
            KeyPreview = true,
            ShowInTaskbar = false
        };
        var canvas = new Panel { Dock = DockStyle.Fill, BackColor = Fondo };
        scene.Controls.Add(canvas);
        var skip = MakeButton("SALTAR  ›", Color.FromArgb(28,20,51), Color.White, 0, 0, 142, 38);
        skip.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
        skip.Left = scene.ClientSize.Width - skip.Width - 28;
        skip.Top = scene.ClientSize.Height - skip.Height - 22;
        scene.Resize += (_, _) => { skip.Left = scene.ClientSize.Width - skip.Width - 28; skip.Top = scene.ClientSize.Height - skip.Height - 22; };
        scene.Controls.Add(skip);

        string[] titles = { "LA NOCHE EN QUE TODO CAMBIÓ", "EL DESPERTAR", "TRES PORTALES", "TU PRIMERA INCURSIÓN" };
        string[] lines =
        {
            "Una grieta violeta rasgó el cielo. Algo cruzó desde el otro lado.",
            "Herido y sin fuerzas, escuchaste una voz: el Sistema te eligió.",
            "Las Ruinas del Umbral, el Bosque Carmesí y la Ciudad Sumergida quedaron conectados.",
            "Sobrevive, derrota al Guardián y recupera el núcleo para cerrar la brecha."
        };
        var clock = new System.Windows.Forms.Timer { Interval = 40 };
        double elapsed = 0;
        bool finished = false;
        void Finish()
        {
            if (finished) return;
            finished = true;
            clock.Stop();
            scene.DialogResult = DialogResult.OK;
            scene.Close();
        }
        skip.Click += (_, _) => Finish();
        scene.KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) Finish(); };
        canvas.Paint += (_, e) =>
        {
            var g = e.Graphics;
            DrawMenuBackdrop(g, canvas.ClientRectangle);
            float beat = (float)(.5 + .5 * Math.Sin(elapsed * 2.4));
            int halo = (int)(130 + beat * 55);
            using (var glow = new SolidBrush(Color.FromArgb(30 + (int)(beat * 24), 146, 74, 255)))
                g.FillEllipse(glow, canvas.Width / 2 - halo, canvas.Height / 2 - halo, halo * 2, halo * 2);
            using (var veil = new SolidBrush(Color.FromArgb(108, 3, 3, 12))) g.FillRectangle(veil, canvas.ClientRectangle);
            int phase = Math.Min(3, (int)(elapsed / 4.25));
            float phaseTime = (float)(elapsed % 4.25);
            int alpha = Math.Clamp((int)(Math.Min(1, Math.Min(phaseTime / .55, (4.25 - phaseTime) / .55)) * 255), 0, 255);
            using var titleFont = new Font("Bahnschrift", 23, FontStyle.Bold);
            using var bodyFont = new Font("Bahnschrift", 13, FontStyle.Regular);
            using var titleBrush = new SolidBrush(Color.FromArgb(alpha, 235, 222, 255));
            using var bodyBrush = new SolidBrush(Color.FromArgb(alpha, 220, 216, 235));
            var titleRect = new RectangleF(80, canvas.Height - 185, canvas.Width - 160, 42);
            var bodyRect = new RectangleF(120, canvas.Height - 132, canvas.Width - 240, 58);
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(titles[phase], titleFont, titleBrush, titleRect, format);
            g.DrawString(lines[phase], bodyFont, bodyBrush, bodyRect, format);
            using var rule = new Pen(Color.FromArgb(alpha, 153, 92, 255), 2);
            g.DrawLine(rule, canvas.Width / 2 - 140, (int)titleRect.Bottom + 3, canvas.Width / 2 + 140, (int)titleRect.Bottom + 3);
        };
        clock.Tick += (_, _) => { elapsed += clock.Interval / 1000d; canvas.Invalidate(); if (elapsed >= 17) Finish(); };
        scene.Shown += (_, _) => clock.Start();
        scene.FormClosed += (_, _) => clock.Dispose();
        scene.ShowDialog(this);
        onComplete();
    }

    private async Task TestSql(IWin32Window? owner = null)
    {
        try
        {
            using var con=await OpenGameDatabase();await EnsureSchema(con);
            _status.Text="●  SQL SERVER: conexión activa";_status.ForeColor=Turquesa;
            ShowSystemMessage("Conexión activa. El archivo de partidas del Sistema está disponible.","SQL Server",MessageBoxButtons.OK,MessageBoxIcon.Information,owner);
        }
        catch(Exception ex)
        {
            _status.Text="●  SQL SERVER: sin conexión";_status.ForeColor=Color.Salmon;
            ShowSystemMessage("No fue posible conectar. Revisa el servidor, la base de datos y las credenciales.\n\n"+ex.Message,"Conexión no disponible",MessageBoxButtons.OK,MessageBoxIcon.Warning,owner);
        }
    }
    private async Task SaveGame(bool notify, IWin32Window? owner = null)
    {
        if(_session is null)return;
        try
        {
            using var con=await OpenGameDatabase();await EnsureSchema(con);
            using var cmd=con.CreateCommand();
            cmd.CommandText="IF EXISTS (SELECT 1 FROM dbo.PartidaNexus WHERE NombreCadete=@nombre) UPDATE dbo.PartidaNexus SET Fila=@fila,Columna=@columna,Energia=@energia,Estabilidad=@estabilidad,XP=@xp,EstadoJson=@estado,FechaActualizacion=SYSUTCDATETIME() WHERE NombreCadete=@nombre ELSE INSERT dbo.PartidaNexus(NombreCadete,Fila,Columna,Energia,Estabilidad,XP,EstadoJson) VALUES(@nombre,@fila,@columna,@energia,@estabilidad,@xp,@estado);";
            cmd.Parameters.AddWithValue("@nombre",_session.Cadete.Nombre);cmd.Parameters.AddWithValue("@fila",_session.Mapa.Fila);cmd.Parameters.AddWithValue("@columna",_session.Mapa.Columna);
            cmd.Parameters.AddWithValue("@energia",_session.Cadete.Energia);cmd.Parameters.AddWithValue("@estabilidad",_session.Cadete.Estabilidad);cmd.Parameters.AddWithValue("@xp",_session.Cadete.XP);
            cmd.Parameters.Add("@estado",System.Data.SqlDbType.NVarChar,-1).Value=_session.Exportar();await cmd.ExecuteNonQueryAsync();
            if(notify)ShowSystemMessage("Progreso sincronizado en SQL Server.","Partida guardada",MessageBoxButtons.OK,MessageBoxIcon.Information,owner);
        }
        catch(Exception ex){ShowSystemMessage("No se guardó la partida. Comprueba la conexión SQL Server.\n\n"+ex.Message,"Error de guardado",MessageBoxButtons.OK,MessageBoxIcon.Warning,owner);}
    }
    private async Task LoadGame()
    {
        try
        {
            using var con=await OpenGameDatabase();await EnsureSchema(con);
            using var cmd=new SqlCommand("SELECT TOP 1 EstadoJson FROM dbo.PartidaNexus WHERE EstadoJson IS NOT NULL ORDER BY FechaActualizacion DESC",con);
            string? json=(string?)await cmd.ExecuteScalarAsync();
            if(string.IsNullOrWhiteSpace(json)){ShowSystemMessage("No hay una partida sincronizada disponible para recuperar.","Sin partidas",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
            _session=GameSession.Restaurar(json);_nombre=_session.Cadete.Nombre;_failureReported=false;ShowGame();
        }
        catch(Exception ex){ShowSystemMessage("No se pudo recuperar la partida. Configura y prueba SQL Server.\n\n"+ex.Message,"Error de recuperación",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    }

    private static async Task EnsureSchema(SqlConnection con)
    {
        const string sql="IF OBJECT_ID('dbo.PartidaNexus','U') IS NULL CREATE TABLE dbo.PartidaNexus(PartidaId int IDENTITY PRIMARY KEY,NombreCadete nvarchar(80) NOT NULL UNIQUE,Fila int NOT NULL,Columna int NOT NULL,Energia int NOT NULL,Estabilidad int NOT NULL,XP int NOT NULL,EstadoJson nvarchar(max) NULL,FechaActualizacion datetime2 NOT NULL DEFAULT SYSUTCDATETIME()); IF COL_LENGTH('dbo.PartidaNexus','EstadoJson') IS NULL ALTER TABLE dbo.PartidaNexus ADD EstadoJson nvarchar(max) NULL;";
        using var cmd=new SqlCommand(sql,con);await cmd.ExecuteNonQueryAsync();
    }

    private async Task<SqlConnection> OpenGameDatabase()
    {
        var builder=new SqlConnectionStringBuilder(_connectionString);
        if(string.IsNullOrWhiteSpace(builder.InitialCatalog))throw new InvalidOperationException("Indica el nombre de la base de datos en la cadena de conexión.");
        string database=builder.InitialCatalog;
        var adminBuilder=new SqlConnectionStringBuilder(_connectionString){InitialCatalog="master"};
        using(var admin=new SqlConnection(adminBuilder.ConnectionString))
        {
            await admin.OpenAsync();
            using var exists=new SqlCommand("SELECT CASE WHEN DB_ID(@database) IS NULL THEN 0 ELSE 1 END",admin);
            exists.Parameters.AddWithValue("@database",database);
            if(Convert.ToInt32(await exists.ExecuteScalarAsync())==0)
            {
                using var create=new SqlCommand("DECLARE @sql nvarchar(max)=N'CREATE DATABASE '+QUOTENAME(@database); EXEC(@sql);",admin);
                create.Parameters.AddWithValue("@database",database);await create.ExecuteNonQueryAsync();
            }
        }
        var connection=new SqlConnection(_connectionString);
        try{await connection.OpenAsync();return connection;}
        catch{await connection.DisposeAsync();throw;}
    }

    private void ConnectionSettings()
    {
        using var dlg=CreateSystemDialog("Conexión SQL Server",new Size(590,310));
        dlg.Controls.Add(MakeLabel("CONECTAR AL ARCHIVO DE PARTIDAS",13,Turquesa,true,24,20,480,28));
        var box=new TextBox{Left=24,Top=60,Width=520,Height=70,Multiline=true,Text=_connectionString,ScrollBars=ScrollBars.Vertical};StyleSystemInput(box);dlg.Controls.Add(box);
        dlg.Controls.Add(MakeLabel("Usa SQL Server LocalDB por defecto. Puedes cambiar el servidor y la base sin alterar tu progreso.",9,Color.FromArgb(170,189,209),false,24,140,510,45));
        var save=MakeButton("PROBAR Y CONECTAR",Turquesa,Fondo,24,204,235,44);save.Click+=async(_,_)=>{_connectionString=box.Text.Trim();await TestSql(dlg);};dlg.Controls.Add(save);
        var close=MakeButton("CERRAR",Color.FromArgb(24,31,59),Color.White,280,204,150,44);close.Click+=(_,_)=>dlg.Close();dlg.Controls.Add(close);dlg.ShowDialog(this);
    }

    private Panel Card()
    {
        var card=new Panel{Dock=DockStyle.Fill,BackColor=Panel,Margin=new Padding(7)};
        card.Paint+=(_,e)=>
        {
            if(card.Width<2||card.Height<2)return;
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using var border=new Pen(Color.FromArgb(190,153,92,255),1.2f);
            e.Graphics.DrawRectangle(border,0,0,card.Width-1,card.Height-1);
        };
        return card;
    }
    private static Label MakeLabel(string text,float size,Color color,bool bold,int x,int y,int w,int h)=>new(){Text=text,Font=new Font("Bahnschrift",size,bold?FontStyle.Bold:FontStyle.Regular),ForeColor=color,BackColor=Color.Transparent,Left=x,Top=y,Width=w,Height=h};
    private static Button MakeButton(string text,Color back,Color fore,int x,int y,int w,int h){var b=new Button{Text=text,Left=x,Top=y,Width=w,Height=h,BackColor=back,ForeColor=fore,FlatStyle=FlatStyle.Flat,Font=new Font("Bahnschrift",10,FontStyle.Bold),Cursor=Cursors.Hand};b.FlatAppearance.BorderColor=Color.FromArgb(122,83,199);b.FlatAppearance.MouseOverBackColor=Color.FromArgb(Math.Min(255,back.R+18),Math.Min(255,back.G+12),Math.Min(255,back.B+24));b.FlatAppearance.MouseDownBackColor=Color.FromArgb(95,55,167);return b;}
    private static void DrawBackdrop(Graphics g,Rectangle bounds)
    {
        g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var sky=new LinearGradientBrush(bounds,Color.FromArgb(43,31,105),Color.FromArgb(5,6,19),LinearGradientMode.Vertical))g.FillRectangle(sky,bounds);
        var random=new Random(41);
        for(int i=0;i<92;i++){int x=random.Next(bounds.Width),y=random.Next(Math.Max(1,bounds.Height/2));int s=random.Next(1,3);int a=random.Next(55,180);using var star=new SolidBrush(Color.FromArgb(a,221,207,255));g.FillEllipse(star,x,y,s,s);}
        int moonX=(int)(bounds.Width*.73), moonY=(int)(bounds.Height*.34), radius=Math.Min(bounds.Width,bounds.Height)/7;
        using(var moonGlow=new SolidBrush(Color.FromArgb(22,177,111,255)))g.FillEllipse(moonGlow,moonX-radius*2,moonY-radius*2,radius*4,radius*4);
        using(var moon=new LinearGradientBrush(new Rectangle(moonX-radius,moonY-radius,radius*2,radius*2),Color.FromArgb(222,210,255),Color.FromArgb(93,83,170),LinearGradientMode.ForwardDiagonal))g.FillEllipse(moon,moonX-radius,moonY-radius,radius*2,radius*2);
        for(int i=0;i<24;i++)
        {
            int crater=random.Next(radius*2/3),cx=random.Next(moonX-radius/2,moonX+radius/2),cy=random.Next(moonY-radius/2,moonY+radius/2);
            if((cx-moonX)*(cx-moonX)+(cy-moonY)*(cy-moonY)>(radius-crater/2)*(radius-crater/2))continue;
            using var rim=new Pen(Color.FromArgb(45,88,68,142),1);using var floor=new SolidBrush(Color.FromArgb(30,71,47,125));
            g.DrawEllipse(rim,cx-crater/2,cy-crater/2,crater,crater);g.FillEllipse(floor,cx-crater/3,cy-crater/3,Math.Max(1,crater*2/3),Math.Max(1,crater*2/3));
        }
        int ground=(int)(bounds.Height*.60);
        using(var far=new SolidBrush(Color.FromArgb(29,27,74)))g.FillPolygon(far,new[]{new Point(0,ground+20),new Point(bounds.Width/7,ground-50),new Point(bounds.Width/4,ground+2),new Point(bounds.Width*2/5,ground-72),new Point(bounds.Width*3/5,ground+7),new Point(bounds.Width*4/5,ground-42),new Point(bounds.Width,ground+6),new Point(bounds.Width,bounds.Height),new Point(0,bounds.Height)});
        using(var ruin=new SolidBrush(Color.FromArgb(16,12,42)))
        {
            int x=bounds.Width*57/100, baseY=ground+10, towerW=Math.Max(26,bounds.Width/24), towerH=Math.Max(100,bounds.Height/4);
            g.FillRectangle(ruin,x,baseY-towerH,towerW,towerH);
            g.FillPolygon(ruin,new[]{new Point(x-12,baseY-towerH),new Point(x+towerW/2,baseY-towerH-25),new Point(x+towerW+12,baseY-towerH)});
            g.FillRectangle(ruin,x+towerW+26,baseY-towerH*3/4,towerW*2/3,towerH*3/4);
            g.FillPolygon(ruin,new[]{new Point(x+towerW+18,baseY-towerH*3/4),new Point(x+towerW+26+towerW/3,baseY-towerH*3/4-18),new Point(x+towerW+34+towerW*2/3,baseY-towerH*3/4)});
            using var window=new SolidBrush(Color.FromArgb(170,137,80,255));
            for(int i=0;i<3;i++)g.FillRectangle(window,x+7+i*8,baseY-towerH+35,3,12);
        }
        using(var near=new SolidBrush(Color.FromArgb(12,8,27)))g.FillPolygon(near,new[]{new Point(0,ground+56),new Point(bounds.Width/5,ground+15),new Point(bounds.Width*2/5,ground+61),new Point(bounds.Width*3/5,ground+25),new Point(bounds.Width*4/5,ground+70),new Point(bounds.Width,ground+30),new Point(bounds.Width,bounds.Height),new Point(0,bounds.Height)});
        using(var texture=new SolidBrush(Color.FromArgb(20,153,92,255)))
            for(int i=0;i<280;i++){int x=random.Next(bounds.Width),y=random.Next(Math.Max(ground,bounds.Height));int s=random.Next(1,4);g.FillEllipse(texture,x,y,s,s);}
        using(var line=new Pen(Color.FromArgb(110,153,92,255),1))g.DrawLine(line,0,ground+5,bounds.Width,ground+5);
        // Baliza remota sobre la cresta, con una luz de señal tenue.
        int beaconX=(int)(bounds.Width*.81), beaconY=ground+6;
        using(var mast=new Pen(Color.FromArgb(152,196,178,255),2))g.DrawLine(mast,beaconX,beaconY-45,beaconX,beaconY);
        using(var pulse=new SolidBrush(Color.FromArgb(130,153,92,255)))g.FillEllipse(pulse,beaconX-5,beaconY-49,10,10);
        using(var glow=new SolidBrush(Color.FromArgb(235,221,200,255)))g.FillEllipse(glow,beaconX-2,beaconY-46,4,4);
        using(var tint=new LinearGradientBrush(bounds,Color.FromArgb(8,8,12,12),Color.FromArgb(70,8,12,13),LinearGradientMode.Vertical))g.FillRectangle(tint,bounds);
    }
}


