namespace Nexus;

public sealed partial class MainForm
{
    private bool _failureReported;

    private void ShowGame()
    {
        if (_session is null) return;
        Controls.Clear();
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(22,16,22,18), ColumnCount=2, RowCount=2, BackColor=Fondo };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,67)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,72)); root.RowStyles.Add(new RowStyle(SizeType.Percent,100)); Controls.Add(root);

        var header = new Panel { Dock=DockStyle.Fill }; root.Controls.Add(header,0,0); root.SetColumnSpan(header,2);
        header.Controls.Add(MakeLabel("NEXUS",15,Turquesa,true,4,8,84,28));
        header.Controls.Add(MakeLabel("SISTEMA DEL MONARCA",17,Color.White,true,96,2,370,30));
        header.Controls.Add(MakeLabel($"MISIÓN ACTIVA    /    {_session.NombreMapa.ToUpperInvariant()}",9,TextoSuave,false,98,34,520,22));
        var back=MakeButton("Ⅱ  PAUSA",Color.FromArgb(17,23,48),Color.White,0,12,142,40);
        back.Anchor=AnchorStyles.Top|AnchorStyles.Right;back.Left=ClientSize.Width-204;back.Click+=(_,_)=>ShowPause();header.Controls.Add(back);

        var mapPanel=Card();root.Controls.Add(mapPanel,0,1);
        mapPanel.Controls.Add(MakeLabel(_session.NombreMapa.ToUpperInvariant(),11,Turquesa,true,22,17,440,26));
        mapPanel.Controls.Add(MakeLabel($"{_session.AmbienteMapa}   ·   EXPLORACIÓN EN TIEMPO REAL",9,TextoSuave,false,22,43,510,22));
        mapPanel.Controls.Add(MakeLabel("⌂ EXTRACCIÓN     ◈ BOTÍN     ◉ PORTAL     ▲ CAZADOR",9,TextoSuave,false,22,72,510,23));
        _mapScene=new DungeonMapScene{Left=22,Top=105,Width=570,Height=410,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};
        _mapScene.Bind(_session,MoveTo,Act);mapPanel.Controls.Add(_mapScene);
        mapPanel.Controls.Add(MakeLabel("EXPLORACIÓN",9,TextoSuave,true,28,523,120,24));
        mapPanel.Controls.Add(MakeLabel("WASD MOVER   ·   SHIFT DASH   ·   J / ESPACIO ATACAR   ·   Q HUIR",8.5f,Color.FromArgb(218,211,242),false,145,523,450,24));
        _log=MakeLabel("El Sistema te observa. Explora el calabozo y reclama el núcleo.",10,Color.FromArgb(235,226,255),false,28,556,610,42);mapPanel.Controls.Add(_log);

        var side=Card();root.Controls.Add(side,1,1);
        side.Controls.Add(MakeLabel("ESTADO DEL CAZADOR",9,Turquesa,true,20,17,260,22));
        side.Controls.Add(MakeLabel(_session.Cadete.Nombre,16,Color.White,true,20,38,280,30));
        _stats=MakeLabel("",9,Color.FromArgb(231,226,246),false,20,76,285,42);side.Controls.Add(_stats);
        _meter=new Panel{Left=20,Top=121,Width=285,Height=39,BackColor=Color.Transparent};
        _meter.Paint+=(_,e)=>PaintMeters(e.Graphics,_meter.ClientRectangle);
        side.Controls.Add(_meter);
        side.Controls.Add(MakeLabel("OBJETIVOS DEL SISTEMA",9,Turquesa,true,20,166,240,20));
        _objectives=MakeLabel("",9,Color.FromArgb(223,218,239),false,20,188,286,82);side.Controls.Add(_objectives);
        side.Controls.Add(MakeLabel("INVENTARIO DEL CAZADOR",9,Turquesa,true,20,280,240,20));
        _equipment=MakeLabel("",9,Color.FromArgb(208,199,230),false,20,302,286,56);side.Controls.Add(_equipment);
        var inventory=MakeButton("MOCHILA",Color.FromArgb(20,24,51),Color.White,20,363,128,36);inventory.Click+=(_,_)=>OpenInventory();side.Controls.Add(inventory);
        var rest=MakeButton("DESCANSAR",Color.FromArgb(20,24,51),Color.White,158,363,128,36);rest.Click+=(_,_)=>Act(_session.Descansar());side.Controls.Add(rest);
        var extract=MakeButton("EXTRACCIÓN",Color.FromArgb(20,24,51),Turquesa,20,407,128,36);extract.Click+=(_,_)=>{Act(_session.Extraer());if(_session.MisionCompletada)ShowSystemMessage("Núcleo recuperado y Guardián superado. Recompensa: +100 XP y un módulo desconocido.","Misión completada",MessageBoxButtons.OK,MessageBoxIcon.Information);};side.Controls.Add(extract);
        var save=MakeButton("GUARDAR",Color.FromArgb(20,24,51),Color.White,158,407,128,36);save.Click+=async(_,_)=>await SaveGame(true);side.Controls.Add(save);
        side.Controls.Add(MakeLabel("OBJETIVO HOSTIL",9,Turquesa,true,20,455,260,20));
        _encounterPanel=new Panel{Left=16,Top=478,Width=278,Height=156,BackColor=Color.FromArgb(9,14,31)};side.Controls.Add(_encounterPanel);
        RefreshBoard();
    }

    private void MoveTo(int row,int column)
    {
        if(_session is null)return;
        Act(_session.Mover(row,column));
    }

    private void RefreshBoard()
    {
        if(_session is null)return;var game=_session;
        _mapScene?.RefreshScene();
        Cadete cadete=game.Cadete;
        _stats.Text=$"ENERGÍA  {cadete.Energia,3}/{Cadete.MAX}        ESTABILIDAD  {cadete.Estabilidad,3}/{Cadete.MAX}\nATAQUE {cadete.Ataque}   ESCUDO {cadete.Escudo}   XP {cadete.XP}   ZONA {game.Mapa.Fila+1}:{game.Mapa.Columna+1}";
        _meter?.Invalidate();
        _objectives.Text=$"{Check(game.Mision.LlegoAnomalia)}  Investigar la anomalía\n{Check(game.Mision.NucleoEncontrado)}  Recuperar el núcleo\n{Check(game.Mision.EntidadSuperada)}  Superar al Guardián\n{Check(game.Mision.Lista&&game.EnLaBase)}  Regresar a extracción";
        var items=cadete.Inventario.Objetos;
        _equipment.Text=items.Count==0?"Sin objetos en el equipo.":string.Join("\n",items.Take(3).Select((obj,i)=>$"{i+1}. {obj.Nombre}"))+(items.Count>3?$"\n+ {items.Count-3} objeto(s)":"");
        RefreshEncounter();
    }

    private static string Check(bool done)=>done?"✓":"○";

    private void PaintMeters(Graphics g,Rectangle bounds)
    {
        if(_session is null)return;
        g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        int width=Math.Max(1,bounds.Width-4);
        using var back=new SolidBrush(Color.FromArgb(43,34,70));
        using var energy=new SolidBrush(Color.FromArgb(111,103,255));
        using var stability=new SolidBrush(Color.FromArgb(61,171,255));
        g.FillRectangle(back,0,3,width,7);g.FillRectangle(back,0,22,width,7);
        g.FillRectangle(energy,0,3,width*_session.Cadete.Energia/Cadete.MAX,7);
        g.FillRectangle(stability,0,22,width*_session.Cadete.Estabilidad/Cadete.MAX,7);
    }

    private void Act(string message)
    {
        if(_log is not null)_log.Text=message;
        if(_session?.Derrotado==true&&!_failureReported){_failureReported=true;ShowSystemMessage("La energía o estabilidad llegó a cero. La incursión terminó. Recupera fuerzas y vuelve a intentarlo.","Cazador derrotado",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        RefreshBoard();
    }

    private void RefreshEncounter()
    {
        _encounterPanel.Controls.Clear();var enemy=_session?.EnemigoActual;
        if(enemy is null)
        {
            string message=_session?.MisionCompletada==true?"Misión concluida.\nVuelve al puesto de mando.":"Sin amenazas detectadas.\nEl calabozo permanece en silencio.";
            _encounterPanel.Controls.Add(MakeLabel(message,9,TextoSuave,false,12,20,250,50));return;
        }
        _encounterPanel.Controls.Add(MakeLabel($"{enemy.Nombre.ToUpperInvariant()}\n{enemy.Tipo}  ·  ENERGÍA {enemy.Energia}/{enemy.EnergiaMax}",9,Color.White,true,12,7,250,45));
        var attack=MakeButton("ATACAR",Color.FromArgb(105,31,83),Color.White,12,58,112,34);attack.Click+=(_,_)=>Act(_session!.Atacar());_encounterPanel.Controls.Add(attack);
        var analyze=MakeButton("ANALIZAR",Color.FromArgb(26,31,68),Color.White,137,58,122,34);analyze.Click+=(_,_)=>Act(_session!.Analizar());_encounterPanel.Controls.Add(analyze);
        var use=MakeButton("USAR OBJETO",Color.FromArgb(26,31,68),Color.White,12,103,112,34);use.Click+=(_,_)=>OpenInventory();_encounterPanel.Controls.Add(use);
        var flee=MakeButton("ESCAPAR",Color.FromArgb(26,31,68),Color.White,137,103,122,34);flee.Click+=(_,_)=>Act(_session!.Escapar());_encounterPanel.Controls.Add(flee);
    }

    private void OpenInventory()
    {
        if(_session is null)return;var inventory=_session.Cadete.Inventario;
        using var dlg=CreateSystemDialog("Inventario del cazador",new Size(540,390));
        dlg.Controls.Add(MakeLabel($"EQUIPO DEL CAZADOR   /   {inventory.Cantidad} DE {Inventario.Capacidad} ESPACIOS",12,Turquesa,true,22,18,450,28));
        var list=new ListBox{Left=22,Top=55,Width=475,Height=210,Font=new Font("Bahnschrift",10)};StyleSystemInput(list);
        foreach(var item in inventory.Objetos)list.Items.Add($"{item.Nombre}   —   {item.Descripcion}");dlg.Controls.Add(list);
        var use=MakeButton("USAR SELECCIÓN",Turquesa,Fondo,22,282,210,42);use.Click+=(_,_)=>{if(list.SelectedIndex<0){ShowSystemMessage("Selecciona un objeto del inventario para continuar.","Inventario",owner:dlg);return;}Act(_session.UsarObjeto(list.SelectedIndex));dlg.DialogResult=DialogResult.OK;dlg.Close();};dlg.Controls.Add(use);
        var drop=MakeButton("DESCARTAR",Color.FromArgb(45,51,47),Color.White,245,282,130,42);drop.Click+=(_,_)=>{if(list.SelectedIndex<0){ShowSystemMessage("Selecciona primero el objeto que deseas retirar.","Inventario",owner:dlg);return;}if(ShowSystemMessage("Este objeto se retirará del inventario de campo.","Confirmar descarte",MessageBoxButtons.YesNo,MessageBoxIcon.Question,dlg)==DialogResult.Yes){_session.DescartarObjeto(list.SelectedIndex);dlg.DialogResult=DialogResult.OK;dlg.Close();}};dlg.Controls.Add(drop);
        var done=MakeButton("CERRAR",Color.FromArgb(26,31,58),Color.White,390,282,107,42);done.Click+=(_,_)=>dlg.Close();dlg.Controls.Add(done);dlg.ShowDialog(this);RefreshBoard();
    }

    private void HandleMovementKey(object? sender,KeyEventArgs e)
    {
        if(_session is null)return;
        if(e.KeyCode==Keys.Escape){ShowPause();e.Handled=true;return;}
        if(e.KeyCode is not (Keys.W or Keys.A or Keys.S or Keys.D or Keys.Up or Keys.Down or Keys.Left or Keys.Right or Keys.Space or Keys.J or Keys.Q or Keys.ShiftKey))return;
        _mapScene?.SetKey(e.KeyCode,true);e.Handled=true;e.SuppressKeyPress=true;
    }

    private void HandleMovementKeyUp(object? sender,KeyEventArgs e) => _mapScene?.SetKey(e.KeyCode,false);

    private void ShowPause()
    {
        using var dlg=CreateSystemDialog("Sistema en pausa",new Size(460,430));
        var frame=new Panel{Dock=DockStyle.Fill,BackColor=Panel,Padding=new Padding(24)};
        frame.Paint+=(_,e)=>{using var pen=new Pen(Color.FromArgb(210,111,103,255),1.4f);e.Graphics.DrawRectangle(pen,0,0,frame.Width-1,frame.Height-1);};
        dlg.Controls.Add(frame);
        frame.Controls.Add(MakeLabel("SISTEMA EN PAUSA",25,Color.White,true,45,30,300,42));
        frame.Controls.Add(MakeLabel($"La incursión está suspendida.\nTerritorio: {_session?.NombreMapa}.",10,TextoSuave,false,48,80,300,50));
        var resume=MakeButton("▶   CONTINUAR",Turquesa,Color.White,48,150,315,48);resume.Click+=(_,_)=>{dlg.DialogResult=DialogResult.OK;dlg.Close();};frame.Controls.Add(resume);
        var save=MakeButton("GUARDAR PARTIDA",Color.FromArgb(20,24,51),Color.White,48,211,315,42);save.Click+=async(_,_)=>await SaveGame(true,dlg);frame.Controls.Add(save);
        var menu=MakeButton("REGRESAR AL MENÚ",Color.FromArgb(25,19,48),Color.White,48,265,315,42);menu.Click+=async(_,_)=>{await SaveGame(false,dlg);dlg.Close();ShowMenu();};frame.Controls.Add(menu);
        var quit=MakeButton("SALIR DEL JUEGO",Color.FromArgb(21,17,37),TextoSuave,48,319,315,34);quit.Click+=(_,_)=>Close();frame.Controls.Add(quit);
        dlg.ShowDialog(this);
    }
}


