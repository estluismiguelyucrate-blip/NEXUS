# NEXUS — Manual técnico y preparación para el examen H3

**Proyecto:** NEXUS 2.0  
**Lenguaje:** C#  
**Punto de entrada:** `Program.Main` en `Program.cs`  
**Fecha de esta edición:** 12 de octubre de 2026

## Cómo estudiar con este manual

Primero sigue el flujo del juego y ubica las clases en `Program.cs`. Después explica cada ejemplo con tus palabras y predice su resultado antes de leer la respuesta. Los ejemplos que describen código existente se identifican como **NEXUS**; los añadidos para enseñar un concepto que el proyecto no implementa se identifican como **complementarios**. Los números aleatorios hacen que algunas partidas tengan resultados distintos.

Para ejecutar el proyecto se necesita el SDK de .NET 10. Desde la carpeta raíz: `dotnet run --project "NEXUS 2.0.csproj"`. También se puede abrir `NEXUS 2.0.slnx` en una versión de Visual Studio compatible con ese SDK. El proyecto no declara paquetes externos.

## 1. Arquitectura y programación orientada a objetos

### Propósito y funcionamiento

NEXUS es un juego de aventura de texto. La persona jugadora crea un `Cadete`, se desplaza en una cuadrícula, explora celdas, recoge y usa objetos, puede luchar o escapar, y debe completar tres objetivos antes de regresar a la base: llegar a una anomalía, encontrar el núcleo energético y superar una entidad. La extracción solo está disponible en la celda de base.

`Program.Main` configura la codificación de consola, crea un `Simulador` y llama a `Iniciar`. `Simulador.Iniciar` solicita nombre, edad, energía y estabilidad; rechaza cadetes fuera de los requisitos narrativos y, si son válidos, da una célula de energía y entra en `Jugar`. Ese método repite el menú principal hasta que la misión termina, el cadete queda fuera de combate o se sale de la sesión.

### Archivos y responsabilidades

| Archivo | Responsabilidad |
|---|---|
| `Program.cs` | Define todas las clases y reglas del juego en el espacio de nombres `Nexo`. Incluye `Main`. |
| `NEXUS 2.0.csproj` | Configura una aplicación ejecutable, `net10.0`, referencias implícitas y tipos anulables. |
| `NEXUS 2.0.slnx` | Agrupa el proyecto para abrirlo en Visual Studio. Su ruta apunta al `.csproj` en la raíz. |
| `README.md` | Presentación y comandos de ejecución. |
| `docs/` | Manual técnico y diagramas. |

El código C# está concentrado en un único archivo. Las secciones comentadas separan utilidades de consola, personajes, objetos, inventario, encuentros, mapa, misión, controlador y punto de entrada. Esa organización facilita estudiar el ejemplo, aunque al crecer el sistema convendría separar las clases en archivos.

### Clases principales

- `Consola`: concentra entrada, validación de enteros y escritura con color.
- `Personaje`: clase abstracta con estado compartido, energía, ataque y operaciones comunes.
- `Cadete`, `Robot` y `Enemigo`: especializan un personaje para jugador, entidad neutral y hostil.
- `Objeto`: contrato abstracto para objetos utilizables. `CelulaEnergia`, `Escudo`, `ModuloAtaque` y `Analizador` aportan efectos concretos.
- `Inventario`: administra una lista limitada de objetos, su consulta, uso y eliminación.
- `Fabrica`: crea aleatoriamente objetos o entidades.
- `Encuentro`: ejecuta los turnos de combate y determina victoria, escape o derrota.
- `Mapa`: almacena tipos de celda, coordenadas y celdas visitadas.
- `Mision`: conserva los tres objetivos y calcula si están completos.
- `Simulador`: coordina la sesión, el mapa, el cadete, eventos, movimiento y extracción.
- `Program`: configura y arranca la aplicación.

![Diagrama de clases de NEXUS](diagrama-clases.svg)

El diagrama y su fuente Mermaid también están en [`diagramas.md`](diagramas.md). Las flechas de herencia muestran una relación “es-un”; el inventario de un cadete y la lista de objetos muestran “tiene-un/contiene”.

### Estructurada y orientada a objetos

En programación estructurada, una solución puede organizarse alrededor de procedimientos y estructuras de datos. En una versión hipotética del combate, una función central recibiría un tipo numérico de enemigo y tendría varios `if` para decidir cuánto daño aplicar y cómo reaccionar.

NEXUS organiza datos y operaciones en clases: `Cadete.Atacar`, `Personaje.RecibirDano`, `Robot.Reaccionar` e `Inventario.Utilizar`. Al recorrer entidades a través de una referencia `Personaje`, el programa puede enviar el mismo mensaje y permitir que cada clase concreta lo responda. Esto reduce condicionales que escogerían manualmente la conducta por tipo. No significa que toda función tenga que pertenecer a un objeto: `Fabrica` y `Consola` son clases estáticas porque agrupan operaciones globales sin representar una instancia del mundo.

## 2. Fundamentos de clases y objetos

### Clase, objeto e instanciación

Una **clase** describe un tipo; un **objeto** es una instancia con estado concreto. `Cadete` declara los datos y operaciones disponibles para un cadete. En `Simulador.Iniciar`, `new Cadete(nombre, edad, energia, estabilidad)` crea una instancia. En `Program.Main`, `new Simulador().Iniciar()` crea otra instancia y luego invoca su operación de inicio.

Dos cadetes de la misma clase pueden tener energías o inventarios distintos porque cada objeto posee sus propios campos de instancia. En cambio, `Consola.Azar` es estático y se comparte como miembro de la clase `Consola`.

### Campos, propiedades y métodos

En `Personaje`, `_energia` es un campo privado que guarda el valor. `Energia` es una propiedad de solo lectura pública que permite consultarlo sin dejar que cualquier clase lo cambie directamente. `CambiarEnergia` es un método que modifica el campo con límites entre cero y energía máxima.

Otros ejemplos de campos son `Cadete._estabilidad`, `Cadete._escudo` e `Inventario._objetos`. Ejemplos de propiedades son `Cadete.XP` (lectura pública y escritura privada), `Mapa.Fila` (lectura pública y escritura privada) y `Personaje.Ataque` (lectura pública y escritura protegida). Los métodos expresan acciones como `Descansar`, `Agregar`, `Mover` y `Resolver`.

Un campo almacena estado directamente. Una propiedad controla cómo se expone o modifica un valor. Un método representa una operación y puede validar, coordinar cambios o devolver un resultado.

### Constructores y sobrecarga

Los constructores inicializan objetos. El constructor de `Cadete` valida nombre y edad, llama al constructor base para inicializar los datos compartidos de `Personaje`, limita la estabilidad inicial y crea un `Inventario`.

```csharp
public Cadete(string nombre, int edad, int energia, int estabilidad)
    : base(Validar(nombre, edad), MAX, energia, 12)
{
    Edad = edad;
    _estabilidad = Math.Max(0, Math.Min(MAX, estabilidad));
    Inventario = new Inventario();
}
```

En `ModuloAtaque`, el constructor tiene valores opcionales para `nombre` y `bonus`; eso permite omitir argumentos, pero **no es sobrecarga**. En el código actual no hay dos constructores con el mismo nombre de clase y distintas listas de parámetros. La sobrecarga es tener esas firmas distintas en una misma clase.

**Complementario: sobrecarga de constructores.** Este patrón permite construir una coordenada con el origen por defecto o con valores explícitos:

```csharp
public class Coordenada
{
    public int Fila { get; }
    public int Columna { get; }

    public Coordenada() : this(0, 0) { }
    public Coordenada(int fila, int columna)
    {
        Fila = fila;
        Columna = columna;
    }
}
```

El primero delega en el segundo. El uso de `this(0, 0)` aquí es una llamada a otro constructor de la misma clase.

### Encapsulamiento y acceso

Encapsular es proteger el estado y ofrecer operaciones controladas. `Personaje._energia` e `Inventario._objetos` son privados: el resto del programa no puede asignarles un valor directamente. Para la energía se ofrece `CambiarEnergia`; para el inventario, `Agregar`, `Utilizar` y `Eliminar`. Así se conservan límites como no superar la capacidad ni recuperar más que la energía máxima.

| Modificador | Uso en NEXUS |
|---|---|
| `public` | Miembros accesibles desde el resto del programa, por ejemplo `Inventario.Agregar`. |
| `private` | Detalles internos como `Cadete._escudo` o `Simulador.Explorar`. |
| `protected` | `Ataque` tiene `protected set`, de modo que una clase derivada puede ajustarlo. |
| `internal` | No aparece de forma explícita en el código. La clase `Program` no declara modificador, así que usa el acceso predeterminado de nivel de tipo. |

Los accesores de propiedades también limitan cambios. `XP { get; private set; }` se puede leer desde otras clases, pero solo `Cadete` puede asignarlo. `Inventario { get; }` evita reemplazar el inventario después de construir el cadete, aunque el objeto `Inventario` aún ofrece métodos de mutación.

### `this` y `base`

El código de NEXUS usa `base`, pero no usa la palabra clave `this`. `Cadete` invoca `base(...)` para inicializar la parte `Personaje`; `Cadete.RecibirDano` llama a `base.RecibirDano(dano)` después de aplicar el escudo. `Robot.RecibirDano` también delega el cálculo final de energía a la implementación base, después de reducir el daño.

`base` selecciona un miembro de la clase base. `this` haría referencia al objeto actual, por ejemplo `this.Nombre = nombre` para distinguir una propiedad del parámetro con el mismo nombre. En la clase complementaria `Coordenada`, `this(0, 0)` delega a otro constructor de la clase actual.

## 3. Herencia, polimorfismo e interfaces

### Clases base y derivadas

`Cadete`, `Robot` y `Enemigo` derivan de `Personaje`. Comparten nombre, energía, ataque, estado derrotado y operaciones generales. Cada subtipo agrega o modifica lo necesario: un cadete tiene estabilidad, XP e inventario; un robot reduce a la mitad el daño recibido y reacciona con una descarga si es atacado; un enemigo ataca al cadete durante su turno.

`Objeto` también es abstracta. Los cuatro tipos concretos comparten nombre y descripción, y cada uno implementa su manera de `Usar`. Esto permite al inventario guardar `List<Objeto>` y tratar los distintos objetos mediante la misma abstracción.

### `virtual`, `override` y despacho dinámico

`Personaje.RecibirDano` y `Personaje.Reaccionar` son virtuales: tienen una implementación base que las derivadas pueden sustituir. `Cadete.RecibirDano`, `Robot.RecibirDano` y las reacciones de `Robot` y `Enemigo` están marcadas `override`. Cuando `Encuentro.Resolver` llama `e.Reaccionar(c, ataque)`, la implementación elegida depende del tipo real del objeto `e`, aunque la variable se declare `Personaje`.

**Ejemplo NEXUS:** el valor aleatorio de una entidad puede asignarse a `Personaje e`. Si resulta ser `Robot`, la llamada `e.RecibirDano(13)` se dirige a `Robot.RecibirDano`, que calcula `13 / 2` como división entera (6) y pasa ese resultado a `Personaje.RecibirDano`. Si es `Enemigo`, usa directamente el daño de la implementación base. Este comportamiento en ejecución es polimorfismo.

### Clases y métodos abstractos

`Personaje` es abstracta porque representa características comunes y deja detalles concretos a sus derivadas. No se puede hacer `new Personaje(...)`. `Tipo` y `Mostrar` son abstractos: cada clase concreta debe proporcionar su implementación. `Objeto` tampoco se instancia; sus derivadas deben definir `Usar`.

Una clase abstracta puede combinar estado y código compartido con operaciones pendientes. Una interfaz define un contrato de miembros sin expresar una relación de “es-un” ni obligar a compartir almacenamiento de instancia.

### Interfaces: ausencia y ejemplo complementario

NEXUS no declara ni implementa interfaces en `Program.cs`. Las clases abstractas `Personaje` y `Objeto` cumplen un rol parecido para sus jerarquías, pero no son interfaces.

**Complementario:** se podría añadir una capacidad transversal de reparación para objetos reparables:

```csharp
public interface IReparable
{
    int Reparar(int cantidad);
}

public class RobotReparable : Robot, IReparable
{
    public RobotReparable(string nombre, int energia) : base(nombre, energia) { }

    public int Reparar(int cantidad)
    {
        int antes = Energia;
        CambiarEnergia(Math.Max(0, cantidad));
        return Energia - antes;
    }
}
```

La interfaz expresa qué operación se ofrece; la clase decide cómo llevarla a cabo. El ejemplo ilustra el contrato y no forma parte del proyecto entregado.

### Herencia, composición e implementación

- **Herencia:** `Robot : Personaje` significa que un robot **es un** personaje y hereda su estado/comportamiento.
- **Composición:** `Cadete` crea y conserva un `Inventario`. Un cadete **tiene un** inventario. El inventario no es un tipo de personaje.
- **Implementación de interfaz:** una clase declara que ofrece una capacidad contractual, por ejemplo `RobotReparable : IReparable`. La clase puede implementar varias interfaces, mientras que C# solo permite heredar de una clase base directa.

En NEXUS, almacenar objetos como `Objeto` en el inventario permite diversidad por polimorfismo sin derivar `Inventario` de cada tipo de objeto.

## 4. Colecciones y LINQ

### `List<T>` en el proyecto

`Inventario` declara `private readonly List<Objeto> _objetos = new List<Objeto>();`. `List<T>` es una colección genérica: aquí todos los elementos se pueden usar como `Objeto`, aunque cada uno tenga un subtipo concreto. `readonly` impide reemplazar la referencia de lista después de inicializarla; no impide agregar o quitar elementos.

`Agregar` comprueba `Capacidad` (6) antes de llamar a `Add`. `Consultar` recorre la lista con un `for`; `Tiene<T>` la recorre con `foreach` y usa `is T`; `Utilizar` valida el índice, ejecuta el objeto y lo quita con `RemoveAt` solo si `Usar` devuelve `true`; `Eliminar` valida y retira el elemento. Estos recorridos son ejemplos directos de manipulación y consulta de objetos.

### Recorrido tradicional y lambda

NEXUS recorre el inventario con `for` y `foreach`; no usa expresiones lambda. Una lambda es una función breve que puede pasarse como argumento, por ejemplo `o => o.Nombre`.

**Complementario: LINQ y lambdas.** Para listar descripciones ordenadas de los objetos que son consumibles, se podría usar:

```csharp
List<Objeto> objetos = new List<Objeto>
{
    new CelulaEnergia(), new Escudo(), new Analizador()
};

var nombres = objetos
    .Where(o => o is CelulaEnergia || o is Analizador)
    .Select(o => o.Nombre)
    .OrderBy(nombre => nombre);

foreach (string nombre in nombres)
    Console.WriteLine(nombre);
```

`Where` filtra; `Select` transforma cada objeto en su nombre; `OrderBy` ordena. Con los objetos creados, el resultado ordenado es `Analizador` y después `Celula de energia`. El ejemplo requiere `using System.Linq;` si las directivas globales del proyecto no están disponibles.

### LINQ comparado con un recorrido explícito

El mismo filtro se puede expresar con un `foreach` que agregue nombres a una lista auxiliar y luego los ordene. El bucle deja visibles los pasos y puede ser más claro para lógica con varios efectos; LINQ expresa una transformación y un filtrado de forma compacta. En ambos casos importa saber cuándo se ejecuta la consulta: muchas operaciones LINQ se evalúan al recorrer el resultado, no necesariamente al declarar la variable.

El código publicado no contiene `Where`, `Select`, `OrderBy` ni otras consultas LINQ. No atribuyas el ejemplo complementario a NEXUS: sirve para aprender una técnica que todavía no usa.

## 5. Análisis del funcionamiento

### Flujograma de exploración

El proceso escogido es moverse y explorar una celda. El movimiento fuera del mapa se rechaza antes de cobrar energía; un movimiento válido descuenta 2 de energía y examina la celda actual. Una anomalía reduce además 12 de estabilidad y 5 de energía.

![Flujo de movimiento y exploración](flujo-exploracion.svg)

El diagrama ampliado y su fuente Mermaid están en [`diagramas.md`](diagramas.md).

### Seguimiento del estado de un objeto

Supongamos un cadete creado con energía 60, estabilidad 70 y un inventario vacío. `Simulador.Iniciar` le agrega una célula de energía, por lo que la cantidad del inventario pasa a 1. Si luego un movimiento válido lo lleva por primera vez a una celda de anomalía:

| Momento | Energía | Estabilidad | Misión / inventario |
|---|---:|---:|---|
| Después de crear al cadete | 60 | 70 | `LlegoAnomalia = false`; 0 objetos. |
| Tras movimiento válido | 58 | 70 | Se descuenta el coste de movimiento de 2. |
| Tras explorar anomalía | 53 | 58 | Coste adicional: energía -5 y estabilidad -12; `LlegoAnomalia = true`. |
| Inicio del encuentro | 53 | 58 | Si aún no superó la entidad, se crea un enemigo con energía 50 y ataque 9. |

El resultado posterior del encuentro depende de decisiones y valores aleatorios. El seguimiento termina antes del combate para no inventar una tirada concreta. Cada llamada `CambiarEnergia` mantiene el valor entre cero y el máximo de 100.

### Predicción de código

**NEXUS:** un `Enemigo` recién creado con energía 40 recibe `RecibirDano(13)`. No sobrescribe ese método, así que usa `Personaje.RecibirDano`: se limita el daño a valores no negativos y se resta de la energía. La energía queda en 27 y el método devuelve 13.

**NEXUS:** un `Robot` con energía 40 recibe el mismo daño. `Robot.RecibirDano` hace división entera `13 / 2`, cuyo resultado es 6; llama al método base y queda con 34 de energía, devolviendo 6 como daño aplicado. Esta predicción no depende del generador aleatorio.

### ¿Qué ocurre si se modifica el código?

En `Robot.RecibirDano`, sustituir `dano / 2` por `(dano + 1) / 2` redondearía hacia arriba. Con daño 13, el robot recibiría 7 en vez de 6 y pasaría de 40 a 33 de energía, no a 34. El cambio altera el balance del combate y muestra por qué hay que considerar la división entera.

Otro caso: si `Inventario.Utilizar` quitara el objeto antes de llamar `Usar`, una célula de energía no utilizable por tener al cadete al máximo se perdería. Actualmente `Usar` devuelve `false` y `RemoveAt` no se ejecuta, así que el objeto se conserva. El orden de esas operaciones es parte de la regla del inventario.

## 6. Preparación para el examen H3

Intenta responder cada pregunta antes de leer el razonamiento. Las respuestas se apoyan en `Program.cs`; las que requieren una interfaz o LINQ usan explícitamente el ejemplo complementario.

### Pregunta 1 — Diseño orientado a objetos

¿Por qué `Encuentro.Resolver` recibe un `Personaje` y no parámetros separados para nombre, energía, ataque y tipo? ¿Qué ventaja concreta aparece al pasar un `Robot` o un `Enemigo`?

**Respuesta razonada:** `Personaje` ofrece un contrato común para energía, estado derrotado, daño, reacción y presentación. El método puede operar sobre la abstracción. La llamada virtual a `Reaccionar` ejecuta la implementación real de `Robot` o `Enemigo`, evitando que `Encuentro` duplique una cadena de condicionales por tipo. También facilita agregar otra clase derivada con las operaciones requeridas.

### Pregunta 2 — Clases, objetos y estado

Se crea `new Cadete("Luz", 20, 140, -4)`. ¿Qué energía y estabilidad quedan después de construirlo? Explica qué parte de la clase produce cada resultado.

**Respuesta razonada:** la energía queda limitada a 100 por el constructor de `Personaje`, que limita el valor inicial entre cero y `energiaMax`; la estabilidad queda en cero por el `Math.Max(0, Math.Min(MAX, estabilidad))` de `Cadete`. La validación acepta nombre no vacío y edad dentro de 0 a 120, así que la construcción no produce excepción. El constructor no aplica la admisión de la misión; esa regla está en `Simulador.Iniciar`. Al ejecutar el flujo interactivo, `LeerEntero` no aceptaría 140 como entrada porque el máximo permitido es 100.

### Pregunta 3 — Encapsulamiento y constructores

¿Por qué `Inventario._objetos` es privado y `readonly` si el inventario debe cambiar? ¿`ModuloAtaque(string nombre = ..., int bonus = 6)` demuestra sobrecarga?

**Respuesta razonada:** `private` obliga a usar las operaciones de `Inventario`, que aplican el límite de capacidad y validan índices. `readonly` impide reasignar el campo a otra lista, pero sí permite modificar la lista existente con `Add` y `RemoveAt`. El constructor de `ModuloAtaque` tiene una sola firma con argumentos opcionales; no es sobrecarga. Haría falta otra firma, por ejemplo `ModuloAtaque()` y `ModuloAtaque(int bonus)`, dentro de la misma clase.

### Pregunta 4 — Herencia y polimorfismo

Una variable declarada como `Personaje` referencia un `Robot` con energía 40. ¿Qué sucede al invocar `RecibirDano(13)` y por qué?

**Respuesta razonada:** se ejecuta `Robot.RecibirDano`, porque es una sobrescritura virtual y el tipo real es `Robot`. La división entera produce 6, por lo que el método base resta 6: energía final 34 y valor devuelto 6. Si el objeto fuera `Enemigo`, la implementación base restaría 13.

### Pregunta 5 — Interfaces y composición

¿Por qué es más preciso decir que un `Cadete` **tiene** un `Inventario` que decir que **es** un inventario? ¿Qué permitiría `IReparable` si existiera?

**Respuesta razonada:** el cadete compone un objeto `Inventario` en su constructor y lo expone como propiedad; no hereda sus operaciones o identidad. Es una relación de composición. Una interfaz como el ejemplo `IReparable` permitiría que clases distintas ofrecieran la misma operación `Reparar` sin exigir que compartan una clase base. La interfaz no está implementada en la versión actual.

### Pregunta 6 — Listas y LINQ

En el ejemplo complementario se crean una `CelulaEnergia`, un `Escudo` y un `Analizador`. ¿Qué nombres produce `Where(...).Select(...).OrderBy(...)` y qué hace cada operación?

**Respuesta razonada:** `Where` conserva la célula y el analizador; `Select` convierte cada objeto a su propiedad `Nombre`; `OrderBy` los ordena alfabéticamente. Se imprimen `Analizador` y `Celula de energia`. El inventario real usa `List<Objeto>` y bucles, no esa consulta LINQ.

### Pregunta 7 — Predicción y control del inventario

Un cadete con energía 100 intenta usar una `CelulaEnergia`. ¿Cuánta energía termina teniendo y qué pasa con el objeto?

**Respuesta razonada:** conserva 100. `CelulaEnergia.Usar` detecta que ya está al máximo, avisa y devuelve `false`. `Inventario.Utilizar` interpreta `false` como “no consumir” y no llama a `RemoveAt`; la célula queda en la misma posición del inventario.

### Pregunta 8 — Modificación de código

Si `Mapa.Mover` asignara `Fila` y `Columna` antes de comprobar los límites, ¿qué tipo de error podría aparecer? ¿Qué protege el orden actual?

**Respuesta razonada:** el movimiento podría dejar coordenadas fuera de las dimensiones. Las propiedades `Actual` y `Visitada` indexan matrices con esas coordenadas, por lo que una consulta posterior podría producir `IndexOutOfRangeException`. El orden actual calcula las nuevas coordenadas y comprueba los cuatro límites antes de asignarlas; ante un movimiento inválido devuelve `false` y conserva la posición.

