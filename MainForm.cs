using System.Drawing.Drawing2D;
using Microsoft.Data.SqlClient;

namespace Nexus;

public sealed partial class MainForm : Form
{
    private static readonly Color Fondo = Color.FromArgb(14, 18, 16);
    private static readonly Color Panel = Color.FromArgb(27, 32, 28);
    private static readonly Color Turquesa = Color.FromArgb(185, 161, 112);
    private static readonly Color TextoSuave = Color.FromArgb(164, 174, 175);
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
    private readonly Button[,] _tiles = new Button[4,5];

    public MainForm()
    {
        Text = "NEXUS | Expedición";
        MinimumSize = new Size(1040, 700);
        Size = new Size(1250, 790);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Fondo;
        ForeColor = Color.White;
        Font = new Font("Bahnschrift", 10F);
        KeyPreview = true;
        KeyDown += HandleMovementKey;
        ShowMenu();
    }

    private void ShowMenu()
    {
        Controls.Clear();
        var root = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=2, Padding=new Padding(20,18,24,20), BackColor=Fondo };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 69)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31));
        root.RowStyles.Add(new RowStyle(SizeType.Percent,100)); Controls.Add(root);
        var art = new Panel { Dock=DockStyle.Fill, BackColor=Panel, Margin=new Padding(0,8,24,8) }; art.Paint += (_,e) => DrawBackdrop(e.Graphics, art.ClientRectangle); root.Controls.Add(art,0,0);
        art.Controls.Add(MakeLabel("NEXUS", 31, Color.FromArgb(229,224,204), true, 34, 24, 260, 48));
        art.Controls.Add(MakeLabel("BITÁCORA DE CAMPO     /     REGISTRO 04-B", 9, Turquesa, true, 38, 77, 410, 22));
        art.Controls.Add(MakeLabel("La señal llegó\ndesde el otro lado\ndel umbral.", 28, Color.White, true, 38, 143, 450, 132));
        art.Controls.Add(MakeLabel("Una baliza parpadea entre el polvo.\nAlgo respondió desde el cráter.", 12, Color.FromArgb(223,221,208), false, 42, 296, 360, 52));
        art.Paint+=(_,e)=>{using var pen=new Pen(Color.FromArgb(105,185,161,112),1);e.Graphics.DrawLine(pen,40,376,285,376);e.Graphics.DrawLine(pen,40,376,40,400);};
        art.Controls.Add(MakeLabel("SECTOR H3     ·     ENLACE INESTABLE", 9, Turquesa, true, 42, 390, 280, 22));
        art.Controls.Add(MakeLabel("OBJETIVO   /   RECUPERAR EL NÚCLEO", 9, Color.FromArgb(225,221,203), true, 42, 421, 320, 22));
        art.Controls.Add(MakeLabel("Regresa a la baliza de extracción.\nNo pierdas contacto con la base.", 11, TextoSuave, false, 42, 450, 330, 46));
        var right = new Panel { Dock=DockStyle.Fill, BackColor=Fondo, Padding=new Padding(3,54,4,24) }; root.Controls.Add(right,1,0);
        right.Controls.Add(MakeLabel("PUESTO DE\nOBSERVACIÓN 04", 18, Color.White, true, 12, 6, 280, 61));
        right.Controls.Add(MakeLabel("Una operación sigue abierta.", 10, TextoSuave, false, 15, 79, 260, 26));
        var play = MakeButton("INICIAR EXPEDICIÓN   ↗", Turquesa, Fondo, 12, 125, 292, 53); play.FlatAppearance.BorderSize=0; play.Click += (_,_) => NewCadet(); right.Controls.Add(play);
        var cont = MakeButton("CONTINUAR PARTIDA", Color.FromArgb(29,35,31), Color.White, 38, 194, 266, 43); cont.FlatAppearance.BorderColor=Color.FromArgb(88,99,79); cont.Click += async (_,_) => await LoadGame(); right.Controls.Add(cont);
        var db = MakeButton("CONFIGURAR BASE DE DATOS", Color.FromArgb(22,27,25), Color.FromArgb(203,203,186), 12, 253, 292, 40); db.FlatAppearance.BorderSize=0; db.TextAlign=ContentAlignment.MiddleLeft; db.Click += (_,_) => ConnectionSettings(); right.Controls.Add(db);
        right.Controls.Add(MakeLabel("────────────────────────────", 9, Color.FromArgb(70,78,67), false, 14, 317, 270, 20));
        _status = MakeLabel("BASE DE DATOS · SIN COMPROBAR", 9, TextoSuave, false, 15, 345, 270, 24); right.Controls.Add(_status);
        var conn = MakeButton("COMPROBAR CONEXIÓN", Color.FromArgb(29,35,31), Turquesa, 12, 382, 230, 37); conn.FlatAppearance.BorderColor=Color.FromArgb(62,72,61); conn.Click += async (_,_) => await TestSql(); right.Controls.Add(conn);
        var exit = MakeButton("CERRAR REGISTRO", Color.Transparent, TextoSuave, 12, 450, 180, 34); exit.FlatAppearance.BorderSize=0; exit.TextAlign=ContentAlignment.MiddleLeft; exit.Click += (_,_) => Close(); right.Controls.Add(exit);
    }

    private void NewCadet()
    {
        using var dlg = new Form { Text="Nuevo cadete", Size=new Size(390,300), StartPosition=FormStartPosition.CenterParent, BackColor=Panel, ForeColor=Color.White, FormBorderStyle=FormBorderStyle.FixedDialog, MaximizeBox=false, MinimizeBox=false };
        var name = new TextBox { Left=35,Top=60,Width=300,Text="Cadete" };
        var age = new NumericUpDown { Left=35,Top=130,Width=130,Minimum=18,Maximum=60,Value=24 };
        dlg.Controls.Add(MakeLabel("IDENTIFICACIÓN DEL CADETE", 15, Turquesa, true, 35, 20, 320, 28)); dlg.Controls.Add(MakeLabel("Nombre",10,Color.White,false,35,42,100,20)); dlg.Controls.Add(name); dlg.Controls.Add(MakeLabel("Edad",10,Color.White,false,35,106,100,20)); dlg.Controls.Add(age);
        var start=MakeButton("ENTRAR AL SECTOR",Turquesa,Fondo,35,185,300,46); start.Click+=(_,_)=>{try{_nombre=string.IsNullOrWhiteSpace(name.Text)?"Cadete":name.Text.Trim();_session=new GameSession();_session.Iniciar(_nombre,(int)age.Value);_failureReported=false;dlg.DialogResult=DialogResult.OK;dlg.Close();}catch(Exception ex){MessageBox.Show(dlg,ex.Message,"Datos del cadete");}}; dlg.Controls.Add(start);
        if(dlg.ShowDialog(this)==DialogResult.OK) ShowGame();
    }

    private async Task TestSql()
    { try { using var con=await OpenGameDatabase();await EnsureSchema(con);_status.Text="●  SQL SERVER: conexión activa";_status.ForeColor=Turquesa;MessageBox.Show(this,"Conexión correcta y tabla de partidas lista.","SQL Server",MessageBoxButtons.OK,MessageBoxIcon.Information); }catch(Exception ex){_status.Text="●  SQL SERVER: sin conexión";_status.ForeColor=Color.Salmon;MessageBox.Show(this,"No se pudo conectar. Revisa servidor, base y credenciales.\n\n"+ex.Message,"Error de conexión",MessageBoxButtons.OK,MessageBoxIcon.Warning);} }
    private async Task SaveGame(bool notify)
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
            if(notify)MessageBox.Show(this,"Partida guardada en SQL Server.","NEXUS",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }
        catch(Exception ex){MessageBox.Show(this,"No se guardó la partida. Comprueba la conexión SQL Server.\n\n"+ex.Message,"Error al guardar",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    }
    private async Task LoadGame()
    {
        try
        {
            using var con=await OpenGameDatabase();await EnsureSchema(con);
            using var cmd=new SqlCommand("SELECT TOP 1 EstadoJson FROM dbo.PartidaNexus WHERE EstadoJson IS NOT NULL ORDER BY FechaActualizacion DESC",con);
            string? json=(string?)await cmd.ExecuteScalarAsync();
            if(string.IsNullOrWhiteSpace(json)){MessageBox.Show(this,"No hay una partida completa guardada en SQL Server.","NEXUS");return;}
            _session=GameSession.Restaurar(json);_nombre=_session.Cadete.Nombre;_failureReported=false;ShowGame();
        }
        catch(Exception ex){MessageBox.Show(this,"No se pudo cargar. Configura y prueba SQL Server.\n\n"+ex.Message,"SQL Server",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
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
    {using var dlg=new Form{Text="Conexión SQL Server",Size=new Size(590,310),StartPosition=FormStartPosition.CenterParent,BackColor=Panel,ForeColor=Color.White,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false};dlg.Controls.Add(MakeLabel("CADENA DE CONEXIÓN",13,Turquesa,true,24,20,360,28));var box=new TextBox{Left=24,Top=60,Width=520,Height=70,Multiline=true,Text=_connectionString,ScrollBars=ScrollBars.Vertical};dlg.Controls.Add(box);dlg.Controls.Add(MakeLabel("Por defecto usa SQL Server LocalDB. Ajusta Server y Database si tu instancia es distinta.",9,Color.FromArgb(170,189,209),false,24,140,510,45));var save=MakeButton("PROBAR Y USAR",Turquesa,Fondo,24,204,235,44);save.Click+=async(_,_)=>{_connectionString=box.Text.Trim();await TestSql();};dlg.Controls.Add(save);var close=MakeButton("CERRAR",Color.FromArgb(39,56,77),Color.White,280,204,150,44);close.Click+=(_,_)=>dlg.Close();dlg.Controls.Add(close);dlg.ShowDialog(this);}

    private Panel Card()=>new(){Dock=DockStyle.Fill,BackColor=Panel,Margin=new Padding(7)};
    private static Label MakeLabel(string text,float size,Color color,bool bold,int x,int y,int w,int h)=>new(){Text=text,Font=new Font("Bahnschrift",size,bold?FontStyle.Bold:FontStyle.Regular),ForeColor=color,BackColor=Color.Transparent,Left=x,Top=y,Width=w,Height=h};
    private static Button MakeButton(string text,Color back,Color fore,int x,int y,int w,int h){var b=new Button{Text=text,Left=x,Top=y,Width=w,Height=h,BackColor=back,ForeColor=fore,FlatStyle=FlatStyle.Flat,Font=new Font("Bahnschrift",10,FontStyle.Bold),Cursor=Cursors.Hand};b.FlatAppearance.BorderColor=Color.FromArgb(63,76,62);return b;}
    private static void DrawBackdrop(Graphics g,Rectangle bounds)
    {
        g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var sky=new LinearGradientBrush(bounds,Color.FromArgb(37,48,53),Color.FromArgb(13,19,23),LinearGradientMode.Vertical))g.FillRectangle(sky,bounds);
        var random=new Random(41);
        for(int i=0;i<76;i++){int x=random.Next(bounds.Width),y=random.Next(Math.Max(1,bounds.Height/2));int s=random.Next(1,3);int a=random.Next(45,145);using var star=new SolidBrush(Color.FromArgb(a,205,203,184));g.FillEllipse(star,x,y,s,s);}
        int moonX=(int)(bounds.Width*.73), moonY=(int)(bounds.Height*.34), radius=Math.Min(bounds.Width,bounds.Height)/7;
        using(var moon=new LinearGradientBrush(new Rectangle(moonX-radius,moonY-radius,radius*2,radius*2),Color.FromArgb(220,216,191),Color.FromArgb(93,105,101),LinearGradientMode.ForwardDiagonal))g.FillEllipse(moon,moonX-radius,moonY-radius,radius*2,radius*2);
        for(int i=0;i<24;i++)
        {
            int crater=random.Next(radius*2/3),cx=random.Next(moonX-radius/2,moonX+radius/2),cy=random.Next(moonY-radius/2,moonY+radius/2);
            if((cx-moonX)*(cx-moonX)+(cy-moonY)*(cy-moonY)>(radius-crater/2)*(radius-crater/2))continue;
            using var rim=new Pen(Color.FromArgb(32,54,59,56),1);using var floor=new SolidBrush(Color.FromArgb(28,48,53,50));
            g.DrawEllipse(rim,cx-crater/2,cy-crater/2,crater,crater);g.FillEllipse(floor,cx-crater/3,cy-crater/3,Math.Max(1,crater*2/3),Math.Max(1,crater*2/3));
        }
        int ground=(int)(bounds.Height*.60);
        using(var far=new SolidBrush(Color.FromArgb(39,51,52)))g.FillPolygon(far,new[]{new Point(0,ground+20),new Point(bounds.Width/7,ground-50),new Point(bounds.Width/4,ground+2),new Point(bounds.Width*2/5,ground-72),new Point(bounds.Width*3/5,ground+7),new Point(bounds.Width*4/5,ground-42),new Point(bounds.Width,ground+6),new Point(bounds.Width,bounds.Height),new Point(0,bounds.Height)});
        using(var near=new SolidBrush(Color.FromArgb(20,28,30)))g.FillPolygon(near,new[]{new Point(0,ground+56),new Point(bounds.Width/5,ground+15),new Point(bounds.Width*2/5,ground+61),new Point(bounds.Width*3/5,ground+25),new Point(bounds.Width*4/5,ground+70),new Point(bounds.Width,ground+30),new Point(bounds.Width,bounds.Height),new Point(0,bounds.Height)});
        using(var texture=new SolidBrush(Color.FromArgb(20,184,164,125)))
            for(int i=0;i<280;i++){int x=random.Next(bounds.Width),y=random.Next(Math.Max(ground,bounds.Height));int s=random.Next(1,4);g.FillEllipse(texture,x,y,s,s);}
        using(var line=new Pen(Color.FromArgb(88,174,157,107),1))g.DrawLine(line,0,ground+5,bounds.Width,ground+5);
        // Baliza remota sobre la cresta, con una luz de señal tenue.
        int beaconX=(int)(bounds.Width*.81), beaconY=ground+6;
        using(var mast=new Pen(Color.FromArgb(152,166,154),2))g.DrawLine(mast,beaconX,beaconY-45,beaconX,beaconY);
        using(var pulse=new SolidBrush(Color.FromArgb(100,204,166,105)))g.FillEllipse(pulse,beaconX-5,beaconY-49,10,10);
        using(var glow=new SolidBrush(Color.FromArgb(190,215,173,120)))g.FillEllipse(glow,beaconX-2,beaconY-46,4,4);
        using(var tint=new LinearGradientBrush(bounds,Color.FromArgb(8,8,12,12),Color.FromArgb(70,8,12,13),LinearGradientMode.Vertical))g.FillRectangle(tint,bounds);
    }
}

