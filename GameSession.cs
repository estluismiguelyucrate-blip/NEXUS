using System.Text.Json;

namespace Nexus;

/// <summary>Conecta los mapas y reglas de NEXUS con la interfaz jugable de Windows Forms.</summary>
public sealed class GameSession
{
    private const int NucleoFila = 2, NucleoColumna = 4;
    private static readonly (string Nombre, string Ambiente, int[,] Celdas)[] Mapas =
    {
        ("Ruinas del Umbral", "Torres antiguas · niebla púrpura", new int[,] {
            { 0, 0, 1, 0, 0 }, { 0, 2, 0, 3, 0 }, { 0, 0, 0, 0, 1 }, { 3, 0, 1, 0, 0 }
        }),
        ("Bosque Carmesí", "Raíces vivas · bruma roja", new int[,] {
            { 0, 3, 0, 1, 0 }, { 0, 0, 0, 0, 0 }, { 2, 1, 0, 0, 1 }, { 0, 0, 3, 0, 0 }
        }),
        ("Ciudad Sumergida", "Templos inundados · aguas oscuras", new int[,] {
            { 1, 0, 0, 3, 0 }, { 0, 0, 1, 0, 0 }, { 3, 0, 0, 0, 1 }, { 0, 2, 0, 1, 0 }
        })
    };
    public static IReadOnlyList<string> NombresMapas { get; } = Mapas.Select(m => m.Nombre).ToArray();

    private bool _guardianActivo;
    public Cadete Cadete { get; private set; } = null!;
    public Mapa Mapa { get; private set; } = null!;
    public int MapaId { get; private set; }
    public string NombreMapa => Mapas[MapaId].Nombre;
    public string AmbienteMapa => Mapas[MapaId].Ambiente;
    public Mision Mision { get; private set; } = null!;
    public Personaje? EnemigoActual { get; private set; }
    public bool MisionCompletada { get; private set; }
    public bool Derrotado => Cadete is not null && (Cadete.Derrotado || Cadete.Estabilidad <= 0);
    public bool EnLaBase => Mapa is not null && Mapa.Actual == TipoCelda.Base;

    public void Iniciar(string nombre, int edad, int mapaId = 0)
    {
        if (edad is < 18 or > 60) throw new ArgumentOutOfRangeException(nameof(edad), "La edad permitida es de 18 a 60 años.");
        if (mapaId < 0 || mapaId >= Mapas.Length) throw new ArgumentOutOfRangeException(nameof(mapaId), "Selecciona uno de los mapas disponibles.");
        MapaId = mapaId;
        Cadete = new Cadete(nombre, edad, 100, 100);
        Mapa = new Mapa(Mapas[mapaId].Celdas);
        Mision = new Mision();
        EnemigoActual = null;
        _guardianActivo = false;
        MisionCompletada = false;
        Cadete.Inventario.Agregar(new CelulaEnergia());
    }

    public string Exportar()
    {
        var inventario = Cadete.Inventario.Objetos.Select(o => o switch
        {
            CelulaEnergia => new ObjetoGuardado { Tipo = "celula" },
            Escudo => new ObjetoGuardado { Tipo = "escudo" },
            Analizador => new ObjetoGuardado { Tipo = "analizador" },
            ModuloAtaque modulo => new ObjetoGuardado { Tipo = "modulo", Nombre = modulo.Nombre, Bonus = modulo.Bonus },
            _ => new ObjetoGuardado { Tipo = "desconocido", Nombre = o.Nombre }
        }).ToList();
        bool[,] visitadas = new bool[Mapa.Filas, Mapa.Columnas];
        for (int f = 0; f < Mapa.Filas; f++) for (int c = 0; c < Mapa.Columnas; c++) visitadas[f, c] = Mapa.FueVisitada(f, c);
        return JsonSerializer.Serialize(new PartidaGuardada
        {
            Nombre = Cadete.Nombre, Edad = Cadete.Edad, MapaId = MapaId, Energia = Cadete.Energia, Estabilidad = Cadete.Estabilidad,
            Ataque = Cadete.Ataque, XP = Cadete.XP, Escudo = Cadete.Escudo, Descansos = Cadete.DescansosRestantes,
            AnalisisActivo = Cadete.AnalisisActivo, Fila = Mapa.Fila, Columna = Mapa.Columna,
            Visitadas = Enumerable.Range(0, Mapa.Filas).SelectMany(f => Enumerable.Range(0, Mapa.Columnas).Where(c => visitadas[f, c]).Select(c => $"{f},{c}")).ToList(),
            LlegoAnomalia = Mision.LlegoAnomalia, NucleoEncontrado = Mision.NucleoEncontrado, EntidadSuperada = Mision.EntidadSuperada,
            MisionCompletada = MisionCompletada, Inventario = inventario,
            EnemigoTipo = EnemigoActual?.Tipo, EnemigoNombre = EnemigoActual?.Nombre,
            EnemigoEnergia = EnemigoActual?.Energia ?? 0, EnemigoEnergiaMaxima = EnemigoActual?.EnergiaMax ?? 0,
            EnemigoAtaque = EnemigoActual?.Ataque ?? 0, GuardianActivo = _guardianActivo
        });
    }

    public static GameSession Restaurar(string json)
    {
        var guardado = JsonSerializer.Deserialize<PartidaGuardada>(json) ?? throw new InvalidOperationException("El guardado está vacío.");
        var session = new GameSession();
        int mapaId = guardado.MapaId >= 0 && guardado.MapaId < Mapas.Length ? guardado.MapaId : 0;
        session.Iniciar(guardado.Nombre, guardado.Edad, mapaId);
        session.Cadete.CambiarEnergia(guardado.Energia - session.Cadete.Energia);
        session.Cadete.CambiarEstabilidad(guardado.Estabilidad - session.Cadete.Estabilidad);
        session.Cadete.MejorarAtaque(guardado.Ataque - session.Cadete.Ataque);
        session.Cadete.GanarXP(guardado.XP);
        session.Cadete.ActivarEscudo(guardado.Escudo);
        session.Cadete.RestaurarDescansos(guardado.Descansos);
        session.Cadete.AnalisisActivo = guardado.AnalisisActivo;
        bool[,] visitadas = new bool[session.Mapa.Filas, session.Mapa.Columnas];
        foreach (string coordenada in guardado.Visitadas)
        {
            int[] partes = coordenada.Split(',').Select(int.Parse).ToArray();
            if (partes.Length == 2 && partes[0] >= 0 && partes[0] < session.Mapa.Filas && partes[1] >= 0 && partes[1] < session.Mapa.Columnas)
                visitadas[partes[0], partes[1]] = true;
        }
        session.Mapa.RestaurarEstado(guardado.Fila, guardado.Columna, visitadas);
        session.Mision.LlegoAnomalia = guardado.LlegoAnomalia;
        session.Mision.NucleoEncontrado = guardado.NucleoEncontrado;
        session.Mision.EntidadSuperada = guardado.EntidadSuperada;
        session.MisionCompletada = guardado.MisionCompletada;
        // El cadete nuevo ya recibió una célula inicial; se reemplaza por la lista persistida.
        for (int i = session.Cadete.Inventario.Cantidad - 1; i >= 0; i--) session.Cadete.Inventario.Eliminar(i);
        foreach (var objeto in guardado.Inventario)
        {
            Objeto? restaurado = objeto.Tipo switch
            {
                "celula" => new CelulaEnergia(), "escudo" => new Escudo(), "analizador" => new Analizador(),
                "modulo" => new ModuloAtaque(objeto.Nombre ?? "Módulo de ataque", objeto.Bonus), _ => null
            };
            if (restaurado is not null) session.Cadete.Inventario.Agregar(restaurado);
        }
        if (guardado.EnemigoTipo == "Robot")
        {
            var robot = new Robot(guardado.EnemigoNombre ?? "Robot", guardado.EnemigoEnergiaMaxima);
            robot.CambiarEnergia(guardado.EnemigoEnergia - robot.Energia);
            session.EnemigoActual = robot;
        }
        else if (guardado.EnemigoTipo == "Enemigo")
        {
            var enemigo = new Enemigo(guardado.EnemigoNombre ?? "Entidad", guardado.EnemigoEnergiaMaxima, guardado.EnemigoAtaque);
            enemigo.CambiarEnergia(guardado.EnemigoEnergia - enemigo.Energia);
            session.EnemigoActual = enemigo;
        }
        session._guardianActivo = guardado.GuardianActivo;
        return session;
    }

    public string Mover(int fila, int columna)
    {
        if (EnemigoActual is not null) return "Resuelve el encuentro antes de moverte.";
        int df = fila - Mapa.Fila, dc = columna - Mapa.Columna;
        if (Math.Abs(df) + Math.Abs(dc) != 1) return "Solo puedes avanzar a una zona adyacente.";
        if (!Mapa.Mover(df, dc)) return "El borde del sector bloquea el paso.";
        Cadete.CambiarEnergia(-2);
        if (Mapa.Visitada) return "Región ya explorada. Desplazamiento: energía −2.";
        Mapa.Visitada = true;
        return ExplorarActual();
    }

    private string ExplorarActual()
    {
        switch (Mapa.Actual)
        {
            case TipoCelda.Base:
                return "Baliza de extracción. Puedes descansar y revisar tu equipo.";
            case TipoCelda.Recurso:
                if (Mapa.Fila == NucleoFila && Mapa.Columna == NucleoColumna)
                {
                    Mision.NucleoEncontrado = true;
                    Cadete.GanarXP(10);
                    return "Núcleo energético asegurado. +10 XP.";
                }
                if (Cadete.Inventario.Agregar(Fabrica.ObjetoAleatorio())) return "Alijo recuperado. El objeto se añadió al inventario.";
                Mapa.Visitada = false;
                return "Hay suministros, pero el inventario está lleno. Libera un espacio y vuelve.";
            case TipoCelda.Anomalia:
                Cadete.CambiarEstabilidad(-12);
                Cadete.CambiarEnergia(-5);
                Mision.LlegoAnomalia = true;
                if (!Mision.EntidadSuperada)
                {
                    EnemigoActual = new Enemigo("Guardián de la Anomalía", 50, 9);
                    _guardianActivo = true;
                    return "Distorsión detectada: estabilidad −12, energía −7. El Guardián bloquea el paso.";
                }
                return "Anomalía registrada. El Guardián ya fue superado.";
            default:
                return EventoAleatorio();
        }
    }

    private string EventoAleatorio()
    {
        int t = Consola.Azar.Next(100);
        if (t < 25) return "Sin señales en esta región.";
        if (t < 45)
        {
            if (Cadete.Inventario.Agregar(Fabrica.ObjetoAleatorio())) return "Hallazgo recuperado y añadido al inventario.";
            Mapa.Visitada = false;
            return "El inventario está lleno. El hallazgo seguirá aquí hasta que vuelvas.";
        }
        if (t < 65)
        {
            EnemigoActual = Fabrica.EntidadAleatoria();
            _guardianActivo = false;
            return $"Contacto hostil: {EnemigoActual.Nombre}. Prepárate para combatir o escapar.";
        }
        if (t < 80) { Cadete.GanarXP(5); return "Registro de campo recuperado. +5 XP."; }
        if (t < 90)
        {
            int perdida = Consola.Azar.Next(5, 13); Cadete.CambiarEnergia(-perdida);
            return $"Fuga del traje. Energía −{perdida}.";
        }
        Cadete.CambiarEstabilidad(-8); Cadete.CambiarEnergia(-5);
        return "El traje sufrió daños. Energía −5, estabilidad −8.";
    }

    public string Atacar()
    {
        if (EnemigoActual is null) return "No hay ninguna entidad en el sector.";
        int antes = EnemigoActual.Energia;
        Cadete.Atacar(EnemigoActual);
        string mensaje = $"Ataque: {EnemigoActual.Nombre} perdió {antes - EnemigoActual.Energia} de energía.";
        return ResolverRespuesta(mensaje, true);
    }

    public string AtacarEnTiempoReal()
    {
        if (EnemigoActual is null) return "No hay ninguna entidad en el sector.";
        int antes = EnemigoActual.Energia;
        Cadete.Atacar(EnemigoActual);
        string mensaje = $"Golpe: {EnemigoActual.Nombre} perdió {antes - EnemigoActual.Energia} de energía.";
        if (!EnemigoActual.Derrotado) return mensaje;
        bool guard = _guardianActivo;
        Cadete.GanarXP(25);
        if (guard) Mision.EntidadSuperada = true;
        EnemigoActual = null; _guardianActivo = false;
        string botin = Consola.Azar.Next(100) < 50 && Cadete.Inventario.Agregar(Fabrica.ObjetoAleatorio()) ? " Encontraste suministros." : "";
        return $"{mensaje} {(guard ? "Guardián eliminado" : "Entidad superada")}. +25 XP.{botin}";
    }

    public string RecibirAtaqueEnTiempoReal()
    {
        if (EnemigoActual is null || EnemigoActual is Robot) return "";
        int energiaAntes = Cadete.Energia, estabilidadAntes = Cadete.Estabilidad;
        EnemigoActual.Reaccionar(Cadete, true);
        int dano = energiaAntes - Cadete.Energia, estabilidad = estabilidadAntes - Cadete.Estabilidad;
        return $"El enemigo golpea: energía −{dano}, estabilidad −{estabilidad}.";
    }

    public string Analizar()
    {
        if (EnemigoActual is null) return "No hay una entidad que analizar.";
        if (Cadete.Energia < 5) return "Energía insuficiente para analizar (costo: 5).";
        Cadete.CambiarEnergia(-5); Cadete.AnalisisActivo = true;
        return ResolverRespuesta($"Punto débil localizado en {EnemigoActual.Nombre}. Tu próximo ataque será crítico. Energía −5.", false);
    }

    public string Escapar()
    {
        if (EnemigoActual is null) return "No hay ningún encuentro activo.";
        int probabilidad = EnemigoActual is Robot ? 90 : 50;
        if (Consola.Azar.Next(100) < probabilidad)
        {
            bool guard = _guardianActivo; EnemigoActual = null; _guardianActivo = false;
            if (guard) Mision.EntidadSuperada = true;
            return "Lograste escapar del encuentro.";
        }
        return ResolverRespuesta("No conseguiste escapar.", false);
    }

    public string UsarObjeto(int indice)
    {
        if (indice < 0 || indice >= Cadete.Inventario.Cantidad) return "Selecciona un objeto válido.";
        var objeto = Cadete.Inventario.Objetos[indice];
        int energiaAntes = Cadete.Energia;
        bool usado = Cadete.Inventario.Utilizar(indice, Cadete, EnemigoActual);
        if (!usado) return $"No se pudo usar {objeto.Nombre}; permanece en el inventario.";
        string mensaje = objeto is CelulaEnergia
            ? $"{objeto.Nombre}: energía +{Cadete.Energia - energiaAntes}."
            : $"{objeto.Nombre} utilizado.";
        return EnemigoActual is null ? mensaje : ResolverRespuesta(mensaje, false);
    }

    public bool DescartarObjeto(int indice)
    {
        if (indice < 0 || indice >= Cadete.Inventario.Cantidad) return false;
        Cadete.Inventario.Eliminar(indice);
        return true;
    }

    private string ResolverRespuesta(string mensaje, bool fueAtacado)
    {
        if (EnemigoActual is null) return mensaje;
        if (EnemigoActual.Derrotado)
        {
            bool guard = _guardianActivo;
            Cadete.GanarXP(25);
            if (guard) Mision.EntidadSuperada = true;
            EnemigoActual = null; _guardianActivo = false;
            string botin = Consola.Azar.Next(100) < 50 && Cadete.Inventario.Agregar(Fabrica.ObjetoAleatorio()) ? " Encontraste suministros." : "";
            return $"{mensaje} {(guard ? "Guardián eliminado" : "Entidad superada")}. +25 XP.{botin}";
        }
        int energiaAntes = Cadete.Energia, estabilidadAntes = Cadete.Estabilidad;
        EnemigoActual.Reaccionar(Cadete, fueAtacado);
        int dano = energiaAntes - Cadete.Energia, cordura = estabilidadAntes - Cadete.Estabilidad;
        string reaccion = dano > 0 || cordura > 0 ? $" Recibes daño: energía −{dano}, estabilidad −{cordura}." : "";
        if (Cadete.Derrotado || Cadete.Estabilidad <= 0) return mensaje + reaccion + " El cadete ya no puede continuar.";
        return mensaje + reaccion;
    }

    public string Descansar()
    {
        if (!EnLaBase) return "Solo puedes descansar en la baliza.";
        if (Cadete.DescansosRestantes <= 0) return "No quedan descansos disponibles en la base.";
        int energia = Cadete.Energia, estabilidad = Cadete.Estabilidad;
        Cadete.Descansar();
        return $"Descanso de campo. Energía +{Cadete.Energia - energia}; estabilidad +{Cadete.Estabilidad - estabilidad}. Quedan {Cadete.DescansosRestantes}.";
    }

    public string Extraer()
    {
        if (!EnLaBase) return "Debes volver a la baliza de extracción.";
        if (!Mision.Lista) return "La misión aún tiene objetivos pendientes.";
        if (!Cadete.Inventario.Agregar(new ModuloAtaque("Módulo desconocido", 10))) return "Inventario lleno: libera espacio para recibir la recompensa.";
        Cadete.GanarXP(100); MisionCompletada = true;
        return "Extracción completada. +100 XP, +1 módulo desconocido.";
    }
}

public sealed class PartidaGuardada
{
    public string Nombre { get; set; } = "Cadete";
    public int Edad { get; set; }
    public int MapaId { get; set; }
    public int Energia { get; set; }
    public int Estabilidad { get; set; }
    public int Ataque { get; set; }
    public int XP { get; set; }
    public int Escudo { get; set; }
    public int Descansos { get; set; }
    public bool AnalisisActivo { get; set; }
    public int Fila { get; set; }
    public int Columna { get; set; }
    public List<string> Visitadas { get; set; } = new();
    public bool LlegoAnomalia { get; set; }
    public bool NucleoEncontrado { get; set; }
    public bool EntidadSuperada { get; set; }
    public bool MisionCompletada { get; set; }
    public List<ObjetoGuardado> Inventario { get; set; } = new();
    public string? EnemigoTipo { get; set; }
    public string? EnemigoNombre { get; set; }
    public int EnemigoEnergia { get; set; }
    public int EnemigoEnergiaMaxima { get; set; }
    public int EnemigoAtaque { get; set; }
    public bool GuardianActivo { get; set; }
}

public sealed class ObjetoGuardado
{
    public string Tipo { get; set; } = "";
    public string? Nombre { get; set; }
    public int Bonus { get; set; }
}


