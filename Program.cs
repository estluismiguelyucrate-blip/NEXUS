using System;
using System.Collections.Generic;
using System.Text;

namespace Nexus
{
    public enum TipoCelda { Vacio = 0, Recurso = 1, Base = 2, Anomalia = 3 }

    /* ---------------- Utilidades de consola ---------------- */
    public static class Consola
    {
        public static readonly Random Azar = new Random();

        public static void Escribir(string texto, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(texto);
            Console.ResetColor();
        }

        public static string LeerTexto(string etiqueta)
        {
            string valor;
            do
            {
                Console.Write(etiqueta);
                valor = Console.ReadLine() ?? "";
            } while (string.IsNullOrWhiteSpace(valor));
            return valor;
        }

        public static int LeerEntero(string etiqueta, int min, int max)
        {
            while (true)
            {
                Console.Write(etiqueta);
                if (int.TryParse(Console.ReadLine(), out int valor) && valor >= min && valor <= max)
                    return valor;
                Console.WriteLine($"Dato invalido. Ingresa un numero entre {min} y {max}.");
            }
        }
    }

    /* ---------------- PERSONAJES: Personaje -> Cadete, Robot, Enemigo ---------------- */
    public abstract class Personaje
    {
        private int _energia;

        protected Personaje(string nombre, int energiaMax, int energia, int ataque)
        {
            Nombre = nombre;
            EnergiaMax = energiaMax;
            _energia = Math.Max(0, Math.Min(energiaMax, energia));
            Ataque = ataque;
        }

        public string Nombre { get; }
        public int EnergiaMax { get; }
        public int Energia { get { return _energia; } }
        public int Ataque { get; protected set; }
        public bool Derrotado { get { return _energia <= 0; } }
        public abstract string Tipo { get; }

        public void CambiarEnergia(int cantidad)
        {
            _energia = Math.Max(0, Math.Min(EnergiaMax, _energia + cantidad));
        }

        public virtual int RecibirDano(int dano)
        {
            dano = Math.Max(0, dano);
            _energia = Math.Max(0, _energia - dano);
            return dano;
        }

        /* Turno de la entidad. Cada tipo reacciona distinto (polimorfismo) */
        public virtual void Reaccionar(Cadete cadete, bool fueAtacado) { }

        public abstract void Mostrar(bool completo);
    }

    public class Cadete : Personaje
    {
        public const int MAX = 100;
        private int _estabilidad;
        private int _escudo;
        private int _descansos = 2;

        public Cadete(string nombre, int edad, int energia, int estabilidad)
            : base(Validar(nombre, edad), MAX, energia, 12)
        {
            Edad = edad;
            _estabilidad = Math.Max(0, Math.Min(MAX, estabilidad));
            Inventario = new Inventario();
        }

        private static string Validar(string nombre, int edad)
        {
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("El nombre no puede estar vacio.");
            if (edad < 0 || edad > 120) throw new ArgumentOutOfRangeException(nameof(edad), "Edad fuera de rango.");
            return nombre.Trim();
        }

        public override string Tipo { get { return "Cadete"; } }
        public int Edad { get; }
        public int Estabilidad { get { return _estabilidad; } }
        public int XP { get; private set; }
        public bool AnalisisActivo { get; set; }
        public Inventario Inventario { get; }

        public void CambiarEstabilidad(int cantidad)
        {
            _estabilidad = Math.Max(0, Math.Min(MAX, _estabilidad + cantidad));
        }

        public void GanarXP(int cantidad) { XP += cantidad; }
        public void ActivarEscudo(int valor) { _escudo = Math.Max(_escudo, valor); }
        public void MejorarAtaque(int bonus) { Ataque += bonus; }

        public void Descansar()
        {
            if (_descansos <= 0)
            {
                Consola.Escribir("Ya no te quedan descansos.", ConsoleColor.Yellow);
                return;
            }
            _descansos--;
            CambiarEnergia(20);
            CambiarEstabilidad(15);
            Consola.Escribir($"Descansas en la base. Energia +20, Estabilidad +15. Descansos restantes: {_descansos}", ConsoleColor.Green);
        }

        public override int RecibirDano(int dano)
        {
            if (_escudo > 0 && dano > 0)
            {
                int absorbido = Math.Min(_escudo, dano);
                _escudo -= absorbido;
                dano -= absorbido;
                Consola.Escribir($" El escudo absorbe {absorbido} de dano.", ConsoleColor.Cyan);
            }
            return base.RecibirDano(dano);
        }

        public void Atacar(Personaje objetivo)
        {
            int dano = Ataque + Consola.Azar.Next(0, 6);
            if (AnalisisActivo)
            {
                dano *= 2;
                AnalisisActivo = false;
                Consola.Escribir("¡GOLPE CRITICO!", ConsoleColor.Magenta);
            }
            int real = objetivo.RecibirDano(dano);
            Consola.Escribir($"Atacas a {objetivo.Nombre} y le causas {real} de dano.", ConsoleColor.Green);
        }

        public override void Mostrar(bool completo)
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine($"Cadete: {Nombre}   XP: {XP}");
            Console.WriteLine($"Energia: {Energia}/{MAX}   Estabilidad: {Estabilidad}/{MAX}");
            Console.WriteLine($"Ataque: {Ataque}   Escudo: {_escudo}   Inventario: {Inventario.Cantidad}/{Inventario.Capacidad}");
            Console.WriteLine("========================================");
        }
    }

    public class Robot : Personaje
    {
        private const int Descarga = 12;

        public Robot(string nombre, int energia) : base(nombre, energia, energia, 0) { }

        public override string Tipo { get { return "Robot"; } }

        /* Blindaje: recibe la mitad del dano */
        public override int RecibirDano(int dano) { return base.RecibirDano(dano / 2); }

        public override void Reaccionar(Cadete cadete, bool fueAtacado)
        {
            if (!fueAtacado)
            {
                Consola.Escribir($"{Nombre} te escanea en silencio. No es hostil.", ConsoleColor.Cyan);
                return;
            }
            Consola.Escribir($"{Nombre} activa su SISTEMA DEFENSIVO.", ConsoleColor.Red);
            int real = cadete.RecibirDano(Descarga);
            cadete.CambiarEstabilidad(-5);
            Consola.Escribir($"Descarga electrica: Energia -{real}, Estabilidad -5.", ConsoleColor.Red);
        }

        public override void Mostrar(bool completo)
        {
            Console.WriteLine($"\n[ROBOT] {Nombre}  Energia: {Energia}/{EnergiaMax}");
            Console.WriteLine(completo
                ? $"Blindaje 50%. Si lo atacas, descarga de {Descarga}. No ataca primero."
                : "Datos incompletos. Necesitas un analizador.");
        }
    }

    public class Enemigo : Personaje
    {
        public Enemigo(string nombre, int energia, int ataque) : base(nombre, energia, energia, ataque) { }

        public override string Tipo { get { return "Enemigo"; } }

        public override void Reaccionar(Cadete cadete, bool fueAtacado)
        {
            Consola.Escribir($"👾 {Nombre} te ataca.", ConsoleColor.Red);
            int real = cadete.RecibirDano(Ataque + Consola.Azar.Next(0, 5));
            Consola.Escribir($"Recibes {real} de dano.", ConsoleColor.Red);
        }

        public override void Mostrar(bool completo)
        {
            Console.WriteLine($"\n[ENEMIGO] {Nombre}  Energia: {Energia}/{EnergiaMax}");
            Console.WriteLine(completo
                ? $"Ataque: {Ataque}. Hostil: combate inmediato."
                : "Datos incompletos. Necesitas un analizador.");
        }
    }

    /* ---------------- OBJETOS ---------------- */
    public abstract class Objeto
    {
        protected Objeto(string nombre, string descripcion)
        {
            Nombre = nombre;
            Descripcion = descripcion;
        }

        public string Nombre { get; }
        public string Descripcion { get; }

        /* true = el objeto se consume */
        public abstract bool Usar(Cadete usuario, Personaje? objetivo);

        public override string ToString() { return $"{Nombre} - {Descripcion}"; }
    }

    public class CelulaEnergia : Objeto
    {
        public CelulaEnergia() : base("Celula de energia", "Recupera 25 de energia") { }

        public override bool Usar(Cadete usuario, Personaje? objetivo)
        {
            if (usuario.Energia >= Cadete.MAX)
            {
                Consola.Escribir("Tu energia ya esta al maximo. No se consume.", ConsoleColor.Yellow);
                return false;
            }
            int antes = usuario.Energia;
            usuario.CambiarEnergia(25);
            Consola.Escribir($"Utilizar celula de energia\nEnergia: {antes} -> {usuario.Energia}", ConsoleColor.Green);
            return true;
        }
    }

    public class Escudo : Objeto
    {
        public Escudo() : base("Escudo", "Absorbe 30 de dano") { }

        public override bool Usar(Cadete usuario, Personaje? objetivo)
        {
            usuario.ActivarEscudo(30);
            Consola.Escribir("Escudo activado: absorbera 30 de dano.", ConsoleColor.Cyan);
            return true;
        }
    }

    public class ModuloAtaque : Objeto
    {
        private readonly int _bonus;

        public ModuloAtaque(string nombre = "Modulo de ataque", int bonus = 6)
            : base(nombre, $"Ataque +{bonus}")
        {
            _bonus = bonus;
        }

        public override bool Usar(Cadete usuario, Personaje? objetivo)
        {
            usuario.MejorarAtaque(_bonus);
            Consola.Escribir($"{Nombre} instalado. Ataque: {usuario.Ataque}", ConsoleColor.Green);
            return true;
        }
    }

    public class Analizador : Objeto
    {
        public Analizador() : base("Analizador", "Revela una entidad y duplica tu proximo ataque") { }

        public override bool Usar(Cadete usuario, Personaje? objetivo)
        {
            if (objetivo == null)
            {
                Consola.Escribir("El analizador solo sirve durante un encuentro.", ConsoleColor.Yellow);
                return false;
            }
            usuario.AnalisisActivo = true;
            objetivo.Mostrar(true);
            Consola.Escribir("Punto debil localizado: tu proximo ataque sera critico.", ConsoleColor.Cyan);
            return true;
        }
    }

    /* ---------------- INVENTARIO: agregar -> consultar -> utilizar -> eliminar ---------------- */
    public class Inventario
    {
        public const int Capacidad = 6;
        private readonly List<Objeto> _objetos = new List<Objeto>();

        public int Cantidad { get { return _objetos.Count; } }

        public bool Agregar(Objeto objeto)
        {
            if (_objetos.Count >= Capacidad)
            {
                Consola.Escribir($"Inventario lleno. No puedes llevar {objeto.Nombre}.", ConsoleColor.Yellow);
                return false;
            }
            _objetos.Add(objeto);
            Consola.Escribir($"{objeto.Nombre} agregado al inventario.", ConsoleColor.Green);
            return true;
        }

        public void Consultar()
        {
            Console.WriteLine($"\n--- INVENTARIO ({Cantidad}/{Capacidad}) ---");
            if (_objetos.Count == 0) Console.WriteLine("Esta vacio.");
            for (int i = 0; i < _objetos.Count; i++)
                Console.WriteLine($"{i + 1}. {_objetos[i]}");
        }

        public bool Tiene<T>() where T : Objeto
        {
            foreach (Objeto o in _objetos)
                if (o is T) return true;
            return false;
        }

        public bool Utilizar(int indice, Cadete usuario, Personaje? objetivo)
        {
            if (indice < 0 || indice >= _objetos.Count)
            {
                Consola.Escribir("No posees ese objeto.", ConsoleColor.Yellow);
                return false;
            }
            if (!_objetos[indice].Usar(usuario, objetivo)) return false;
            _objetos.RemoveAt(indice);
            return true;
        }

        public void Eliminar(int indice)
        {
            if (indice < 0 || indice >= _objetos.Count)
            {
                Consola.Escribir("No posees ese objeto.", ConsoleColor.Yellow);
                return;
            }
            Consola.Escribir($"{_objetos[indice].Nombre} eliminado.", ConsoleColor.Yellow);
            _objetos.RemoveAt(indice);
        }
    }

    /* ---------------- Fabrica y encuentros ---------------- */
    public static class Fabrica
    {
        public static Objeto ObjetoAleatorio()
        {
            int n = Consola.Azar.Next(10);
            if (n < 4) return new CelulaEnergia();
            if (n < 6) return new Escudo();
            if (n < 8) return new ModuloAtaque();
            return new Analizador();
        }

        public static Personaje EntidadAleatoria()
        {
            if (Consola.Azar.Next(2) == 0)
                return new Robot("Centinela R-9", Consola.Azar.Next(25, 41));
            return new Enemigo("Sombra Fragmentada", Consola.Azar.Next(25, 41), Consola.Azar.Next(6, 10));
        }
    }

    public static class Encuentro
    {
        /* true si el cadete sobrevive (victoria o escape) */
        public static bool Resolver(Cadete c, Personaje e)
        {
            Consola.Escribir($"\nTe encuentras con: {e.Nombre} ({e.Tipo}).", ConsoleColor.Magenta);

            while (!e.Derrotado && !c.Derrotado)
            {
                Console.WriteLine($"\n{e.Nombre}: {e.Energia}/{e.EnergiaMax}   |   Tu energia: {c.Energia}");
                Console.WriteLine("1. Atacar\n2. Escapar\n3. Utilizar un objeto\n4. Analizar la entidad (5 de energia)");
                int op = Consola.LeerEntero("Que haces?: ", 1, 4);
                bool turnoGastado = true, ataque = false;

                if (op == 1)
                {
                    c.Atacar(e);
                    ataque = true;
                }
                else if (op == 2)
                {
                    if (Consola.Azar.Next(100) < (e is Robot ? 90 : 50))
                    {
                        Consola.Escribir("Logras escapar.", ConsoleColor.Yellow);
                        return true;
                    }
                    Consola.Escribir("No consigues escapar.", ConsoleColor.Red);
                }
                else if (op == 3)
                {
                    if (c.Inventario.Cantidad == 0)
                    {
                        Consola.Escribir("Tu inventario esta vacio.", ConsoleColor.Yellow);
                        turnoGastado = false;
                    }
                    else
                    {
                        c.Inventario.Consultar();
                        int i = Consola.LeerEntero("Numero de objeto (0 cancelar): ", 0, 99);
                        turnoGastado = i > 0 && c.Inventario.Utilizar(i - 1, c, e);
                    }
                }
                else
                {
                    if (c.Energia < 5)
                    {
                        Consola.Escribir("Energia insuficiente para analizar.", ConsoleColor.Yellow);
                        turnoGastado = false;
                    }
                    else
                    {
                        c.CambiarEnergia(-5);
                        e.Mostrar(c.Inventario.Tiene<Analizador>());
                    }
                }

                if (!e.Derrotado && turnoGastado) e.Reaccionar(c, ataque);
            }

            if (c.Derrotado) return false;

            Consola.Escribir($"\n{e.Nombre} ha sido derrotado. +25 XP", ConsoleColor.Green);
            c.GanarXP(25);
            if (Consola.Azar.Next(100) < 50) c.Inventario.Agregar(Fabrica.ObjetoAleatorio());
            return true;
        }
    }

    /* ---------------- MAPA y MISION ---------------- */
    public class Mapa
    {
        private readonly TipoCelda[,] _celdas;
        private readonly bool[,] _visitado;

        public Mapa(int[,] datos)
        {
            _celdas = new TipoCelda[datos.GetLength(0), datos.GetLength(1)];
            _visitado = new bool[datos.GetLength(0), datos.GetLength(1)];
            for (int f = 0; f < Filas; f++)
                for (int c = 0; c < Columnas; c++)
                {
                    _celdas[f, c] = (TipoCelda)datos[f, c];
                    if (_celdas[f, c] == TipoCelda.Base) { Fila = f; Columna = c; }
                }
            _visitado[Fila, Columna] = true;
        }

        public int Filas { get { return _celdas.GetLength(0); } }
        public int Columnas { get { return _celdas.GetLength(1); } }
        public int Fila { get; private set; }
        public int Columna { get; private set; }
        public TipoCelda Actual { get { return _celdas[Fila, Columna]; } }
        public bool Visitada { get { return _visitado[Fila, Columna]; } set { _visitado[Fila, Columna] = value; } }

        /* false si el movimiento sale del mapa */
        public bool Mover(int df, int dc)
        {
            int nf = Fila + df, nc = Columna + dc;
            if (nf < 0 || nf >= Filas || nc < 0 || nc >= Columnas) return false;
            Fila = nf;
            Columna = nc;
            return true;
        }

        public void Dibujar()
        {
            Console.WriteLine("\nMAPA   @=tu  ?=sin explorar  0=vacio 1=recurso 2=base 3=anomalia\n");
            for (int f = 0; f < Filas; f++)
            {
                for (int c = 0; c < Columnas; c++)
                {
                    string texto = "? ";
                    ConsoleColor color = ConsoleColor.DarkGray;
                    if (f == Fila && c == Columna) { texto = "@ "; color = ConsoleColor.Yellow; }
                    else if (_visitado[f, c])
                    {
                        texto = (int)_celdas[f, c] + " ";
                        color = _celdas[f, c] == TipoCelda.Anomalia ? ConsoleColor.Red
                              : _celdas[f, c] == TipoCelda.Recurso ? ConsoleColor.Green
                              : _celdas[f, c] == TipoCelda.Base ? ConsoleColor.Cyan : ConsoleColor.Gray;
                    }
                    Console.ForegroundColor = color;
                    Console.Write(texto);
                    Console.ResetColor();
                }
                Console.WriteLine();
            }
        }
    }

    public class Mision
    {
        public bool LlegoAnomalia { get; set; }
        public bool NucleoEncontrado { get; set; }
        public bool EntidadSuperada { get; set; }
        public bool Lista { get { return LlegoAnomalia && NucleoEncontrado && EntidadSuperada; } }

        private static string Marca(bool ok) { return ok ? "[x]" : "[ ]"; }

        public void Mostrar()
        {
            Console.WriteLine("\n📋 MISION: \"La anomalia perdida\"");
            Console.WriteLine($"{Marca(LlegoAnomalia)} Llegar a la zona de anomalia");
            Console.WriteLine($"{Marca(NucleoEncontrado)} Encontrar el nucleo energetico");
            Console.WriteLine($"{Marca(EntidadSuperada)} Sobrevivir al encuentro con la entidad");
            Console.WriteLine("[ ] Regresar al punto de extraccion");
        }
    }

    /* ---------------- CONTROLADOR ---------------- */
    public class Simulador
    {
        private const int NucleoFila = 2, NucleoColumna = 4;

        private Cadete _cadete = null!;
        private readonly Mapa _mapa = new Mapa(new int[,] {
            { 0, 0, 1, 0, 0 },
            { 0, 2, 0, 3, 0 },
            { 0, 0, 0, 0, 1 },
            { 3, 0, 1, 0, 0 } });
        private readonly Mision _mision = new Mision();

        public void Iniciar()
        {
            Consola.Escribir("========================================", ConsoleColor.Blue);
            Consola.Escribir("     BIENVENIDO AL SIMULADOR NEXUS", ConsoleColor.Blue);
            Consola.Escribir("========================================", ConsoleColor.Blue);
            Console.WriteLine("\nAno 2297. Soy cadete de NEXUS y recibi una nueva mision:");
            Console.WriteLine("investigar una anomalia en una realidad desconocida.");
            Console.WriteLine("Debo explorar, recolectar, enfrentar entidades y regresar con vida.\n");

            string nombre = Consola.LeerTexto("Nombre: ");
            int edad = Consola.LeerEntero("Edad: ", 0, 120);
            int energia = Consola.LeerEntero("Energia inicial (0-100): ", 0, 100);
            int estabilidad = Consola.LeerEntero("Estabilidad inicial (0-100): ", 0, 100);
            _cadete = new Cadete(nombre, edad, energia, estabilidad);

            if (edad < 18 || edad > 60 || energia < 25 || estabilidad < 30)
            {
                Consola.Escribir("\nAcceso denegado: edad 18-60, energia >= 25 y estabilidad >= 30.", ConsoleColor.Red);
                return;
            }

            Consola.Escribir($"\nAcceso concedido, {_cadete.Nombre}. Ingresando a la realidad...", ConsoleColor.Green);
            _cadete.Inventario.Agregar(new CelulaEnergia());
            _mision.Mostrar();
            Jugar();
        }

        private void Jugar()
        {
            while (true)
            {
                if (_cadete.Derrotado || _cadete.Estabilidad <= 0)
                {
                    Consola.Escribir("\nALERTA: tu energia o estabilidad llego a 0. MISION FALLIDA.", ConsoleColor.Red);
                    return;
                }

                _cadete.Mostrar(true);
                Console.WriteLine($"Ubicacion: ({_mapa.Fila},{_mapa.Columna})" + (_mapa.Actual == TipoCelda.Base ? "  [EXTRACCION]" : ""));
                Console.WriteLine("1. Moverse   2. Inventario   3. Atacar   4. Ver mapa");
                Console.WriteLine("5. Ver mision   6. Descansar (base)   7. Extraccion (base)   8. Salir");

                switch (Consola.LeerEntero("Elige una opcion: ", 1, 8))
                {
                    case 1: Mover(); break;
                    case 2: MenuInventario(); break;
                    case 3: Consola.Escribir("\nNo hay ningun enemigo cerca.", ConsoleColor.Yellow); break;
                    case 4: _mapa.Dibujar(); break;
                    case 5: _mision.Mostrar(); break;
                    case 6:
                        if (_mapa.Actual == TipoCelda.Base) _cadete.Descansar();
                        else Consola.Escribir("\nSolo puedes descansar en la base.", ConsoleColor.Yellow);
                        break;
                    case 7:
                        if (_mapa.Actual != TipoCelda.Base)
                            Consola.Escribir("\nDebes estar en el punto de extraccion (celda 2).", ConsoleColor.Yellow);
                        else if (!_mision.Lista)
                        {
                            Consola.Escribir("\nAun faltan objetivos.", ConsoleColor.Yellow);
                            _mision.Mostrar();
                        }
                        else if (Extraer()) return;
                        break;
                    case 8:
                        Consola.Escribir("\nNEXUS cierra la conexion. Sesion finalizada.", ConsoleColor.Green);
                        return;
                }
            }
        }

        private bool Extraer()
        {
            if (!_cadete.Inventario.Agregar(new ModuloAtaque("Modulo desconocido", 10)))
            {
                Consola.Escribir("Libera espacio en el inventario para recibir tu recompensa.", ConsoleColor.Yellow);
                return false;
            }
            _cadete.GanarXP(100);
            Consola.Escribir("\n MISION COMPLETADA\n+100 XP\n+1 modulo desconocido", ConsoleColor.Green);
            Console.WriteLine($"{_cadete.Nombre} regreso con vida. XP total: {_cadete.XP}");
            return true;
        }

        private void Mover()
        {
            Console.Write("Direccion (W=arriba S=abajo A=izquierda D=derecha): ");
            string dir = (Console.ReadLine() ?? "").Trim().ToUpper();
            int df = 0, dc = 0;
            if (dir == "W") df = -1;
            else if (dir == "S") df = 1;
            else if (dir == "A") dc = -1;
            else if (dir == "D") dc = 1;
            else { Consola.Escribir("Direccion invalida. Usa W, A, S o D.", ConsoleColor.Yellow); return; }

            if (!_mapa.Mover(df, dc))
            {
                Consola.Escribir("Limite del mapa: no puedes salir de la realidad.", ConsoleColor.Yellow);
                return;
            }
            _cadete.CambiarEnergia(-2);
            Console.WriteLine($"\nTe desplazas a ({_mapa.Fila},{_mapa.Columna}). Energia -2.");
            Explorar();
        }

        /* Cada zona nueva produce una situacion con consecuencias */
        private void Explorar()
        {
            TipoCelda celda = _mapa.Actual;
            if (celda == TipoCelda.Base) { Consola.Escribir("Zona segura: punto de extraccion.", ConsoleColor.Cyan); return; }
            if (_mapa.Visitada) { Consola.Escribir("Zona ya explorada. Sin novedades.", ConsoleColor.DarkGray); return; }
            _mapa.Visitada = true;

            if (celda == TipoCelda.Recurso)
            {
                Consola.Escribir("\n===  OBJETO ENCONTRADO ===", ConsoleColor.Yellow);
                if (_mapa.Fila == NucleoFila && _mapa.Columna == NucleoColumna)
                {
                    _mision.NucleoEncontrado = true;
                    Consola.Escribir(" Has encontrado el NUCLEO ENERGETICO.", ConsoleColor.Green);
                }
                else EncontrarObjeto();
            }
            else if (celda == TipoCelda.Anomalia)
            {
                Consola.Escribir("\n=== ⚡ ANOMALIA DETECTADA ===", ConsoleColor.Magenta);
                _cadete.CambiarEstabilidad(-12);
                _cadete.CambiarEnergia(-5);
                Console.WriteLine("La realidad se distorsiona. Estabilidad -12, Energia -5.");
                _mision.LlegoAnomalia = true;
                if (!_mision.EntidadSuperada)
                {
                    Consola.Escribir("\n=== ENTIDAD DETECTADA ===", ConsoleColor.Red);
                    if (Encuentro.Resolver(_cadete, new Enemigo("Guardian de la Anomalia", 50, 9)))
                        _mision.EntidadSuperada = true;
                }
            }
            else EventoAleatorio();
        }

        private void EncontrarObjeto()
        {
            Objeto obj = Fabrica.ObjetoAleatorio();
            Console.WriteLine(obj);
            if (!_cadete.Inventario.Agregar(obj))
                _mapa.Visitada = false;   // podras volver cuando liberes espacio
        }

        private void EventoAleatorio()
        {
            int t = Consola.Azar.Next(100);
            if (t < 25) Consola.Escribir("\nNo encuentras nada en esta zona.", ConsoleColor.DarkGray);
            else if (t < 45) { Consola.Escribir("\n=== OBJETO ENCONTRADO ===", ConsoleColor.Yellow); EncontrarObjeto(); }
            else if (t < 65)
            {
                Consola.Escribir("\n=== ENTIDAD DETECTADA ===", ConsoleColor.Red);
                Encuentro.Resolver(_cadete, Fabrica.EntidadAleatoria());
            }
            else if (t < 80)
            {
                Consola.Escribir("\n=== 📡 INFORMACION ENCONTRADA ===", ConsoleColor.Cyan);
                Console.WriteLine("Un registro antiguo dice: 'los guardianes protegen la anomalia'. +5 XP");
                _cadete.GanarXP(5);
            }
            else if (t < 90)
            {
                Consola.Escribir("\n=== PERDIDA DE ENERGIA ===", ConsoleColor.Yellow);
                int perdida = Consola.Azar.Next(5, 13);
                _cadete.CambiarEnergia(-perdida);
                Console.WriteLine($"Una fuga te drena {perdida} de energia.");
            }
            else
            {
                Consola.Escribir("\n=== SISTEMA DANADO ===", ConsoleColor.Red);
                _cadete.CambiarEstabilidad(-8);
                _cadete.CambiarEnergia(-5);
                Console.WriteLine("Tu traje falla. Estabilidad -8, Energia -5.");
            }
        }

        private void MenuInventario()
        {
            Inventario inv = _cadete.Inventario;
            while (true)
            {
                Console.WriteLine("\nINVENTARIO: 1. Consultar  2. Utilizar  3. Eliminar  0. Volver");
                int op = Consola.LeerEntero("Opcion: ", 0, 3);
                if (op == 0) return;
                if (op == 1) { inv.Consultar(); continue; }
                if (inv.Cantidad == 0) { Consola.Escribir("Tu inventario esta vacio.", ConsoleColor.Yellow); continue; }

                inv.Consultar();
                int n = Consola.LeerEntero("Numero de objeto (0 cancelar): ", 0, 99);
                if (n == 0) continue;
                if (op == 2) inv.Utilizar(n - 1, _cadete, null);
                else inv.Eliminar(n - 1);
            }
        }
    }

    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            try
            {
                new Simulador().Iniciar();
            }
            catch (Exception ex)
            {
                Consola.Escribir($"\nError inesperado en NEXUS: {ex.Message}", ConsoleColor.Red);
            }
            Console.WriteLine("\nPresiona una tecla para cerrar...");
            Console.ReadKey();
        }
    }
}