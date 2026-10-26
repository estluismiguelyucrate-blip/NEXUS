# Arquitectura actual de NEXUS H3

## Qué existe

El proyecto actual es una aplicación de consola C# en .NET 10. El archivo NEXUS 2.0.csproj apunta a net10.0, activa ImplicitUsings y Nullable, y no declara paquetes NuGet. La solución NEXUS 2.0.slnx contiene ese proyecto. Casi toda la lógica vive en Program.cs.

Al iniciar, Program.Main crea un Simulador. Este pide los datos del Cadete y aplica condiciones de acceso: edad de 18 a 60 años, energía mínima 25 y estabilidad mínima 30. Después entrega una célula de energía y abre el menú para jugar.

## Clases y responsabilidades reales

| Clase o tipo en Program.cs | Lo que hace ahora |
|---|---|
| TipoCelda | Enumera vacío, recurso, base y anomalía. |
| Consola | Lee texto y números, valida rangos y escribe mensajes con color. También mantiene el Random compartido. |
| Personaje | Clase abstracta que guarda nombre, energía máxima, energía y ataque; aplica daño y define operaciones abstractas o virtuales. |
| Cadete | Hereda de Personaje; guarda edad, estabilidad, XP, escudo, descansos y un Inventario. Controla ataque, descanso, daño y estabilidad. |
| Robot y Enemigo | Tipos derivados con respuestas distintas; el Robot reduce a la mitad el daño recibido. |
| Objeto | Clase abstracta con nombre, descripción y método Usar. |
| CelulaEnergia, Escudo, ModuloAtaque, Analizador | Recuperan energía, protegen, mejoran ataque o revelan una entidad y duplican el siguiente golpe. |
| Inventario | Mantiene una List<Objeto> privada, limita la capacidad a 6 y agrega, consulta, utiliza o elimina objetos. |
| Fabrica | Crea objetos y entidades de manera aleatoria. |
| Encuentro | Ejecuta los turnos de combate y determina victoria, escape o derrota. |
| Mapa | Guarda celdas y visitadas en arreglos bidimensionales, valida límites y dibuja en consola. |
| Mision | Guarda tres objetivos booleanos: anomalía, núcleo y entidad superada. La extracción se comprueba en Simulador. |
| Simulador | Crea el mapa de 4 × 5, controla menú, movimientos, exploración, eventos, inventario, descanso y extracción. |
| Program | Contiene el punto de entrada Main. |

## Funcionamiento de una partida

El mapa se define como una matriz fija dentro de Simulador. Mapa recibe los datos, guarda la posición de la base y marca la primera casilla como visitada. Cada movimiento válido consume 2 de energía. Al visitar una anomalía, el Cadete pierde 12 de estabilidad y 5 de energía; si aún no ha superado la entidad, empieza el encuentro con el Guardián, que tiene 50 de energía y ataque 9. El núcleo está en (2,4) usando índices desde cero.

En celdas vacías, EventoAleatorio decide entre no encontrar nada (25%), encontrar objeto (20%), tener encuentro (20%), ganar información y 5 XP (15%), perder de 5 a 12 de energía (10%) o recibir daño del traje (10%). El inventario empieza con una célula de energía y admite seis objetos. Si está lleno, la casilla de objeto vuelve a quedar disponible.

Para terminar, el Cadete debe marcar los tres objetivos y estar en la base. La extracción primero intenta agregar un módulo; si el inventario está lleno no se completa. Al completarse, da 100 XP. Si energía o estabilidad llegan a cero, la misión falla. En la base hay dos descansos que recuperan energía y estabilidad.

## Límites para la siguiente etapa

- Consola, Mostrar, Consultar, Dibujar y varias reglas imprimen directamente en Console. No hay una interfaz gráfica separada.
- Simulador concentra el ciclo, los menús, el mapa y los eventos. Copiar esa lógica en formularios la haría difícil de mantener.
- El mapa, la ubicación del núcleo y la única misión están escritos en código.
- La partida vive en memoria. Cerrar la aplicación pierde posición, energía, objetos, objetivos, celdas visitadas y XP.
- No hay SQL Server, ORM, repositorios, DTO de interfaz ni modelo de guardado.
- Se crea un Cadete por sesión; no hay grupo seleccionable.
- Fabrica y Encuentro son estáticos y el azar compartido dificulta repetir exactamente una partida.
- WinForms requiere un proyecto de escritorio Windows; el proyecto actual está configurado como aplicación de consola.

## Archivos de referencia

- [Program.cs](../Program.cs): clases y reglas existentes.
- [NEXUS 2.0.csproj](../NEXUS%202.0.csproj): configuración actual.
- [NEXUS 2.0.slnx](../NEXUS%202.0.slnx): solución actual.
- [Diagrama de clases H3](diagrama-clases.svg) y [flujo de exploración H3](flujo-exploracion.svg).

La versión gráfica todavía no está implementada. En los demás documentos se mantiene separada la descripción actual de la propuesta futura.