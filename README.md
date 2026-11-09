# NEXUS 2.0

NEXUS es una aventura de exploración por consola hecha en C#. El jugador crea un Cadete, recorre un mapa, administra energía y estabilidad, recoge objetos, enfrenta entidades y trata de completar una misión de extracción.

## Estado actual

La versión H3 implementa una misión y un mapa fijo de 4 × 5 en consola. Tiene combate por turnos, anomalías, objetos, inventario limitado, objetivos y extracción. La solución ahora incluye un primer menú principal en Windows Forms; la opción «Iniciar juego» ejecuta la versión de consola existente.

## Tecnologías

- C# y .NET 10, con destino net10.0.
- Aplicación de consola y aplicación Windows Forms en la misma solución; no se declaran paquetes NuGet externos.
- La persistencia con SQL Server y Entity Framework Core sigue siendo una etapa futura.

## Cómo ejecutar la versión actual

Se necesita Windows y el SDK de .NET 10. Abre `NEXUS 2.0.slnx` en Visual Studio y selecciona `nexus visual` como proyecto de inicio para ejecutar el menú gráfico. Para iniciar la versión de consola directamente, desde la carpeta raíz ejecuta:

    dotnet run --project "NEXUS 2.0.csproj"

La aplicación de consola espera que se ingresen las opciones en la terminal.

## Documentación

La carpeta docs separa el análisis del programa que existe y la propuesta para una etapa futura:

- [Diseño del juego](docs/diseno-del-juego.md): visión, reglas, ciclo, navegación y cuatro bocetos de pantallas.
- [Arquitectura actual](docs/arquitectura-actual.md): clases y límites reales del código H3.
- [Arquitectura propuesta](docs/arquitectura-propuesta.md): separación entre Windows Forms, lógica del juego y acceso a SQL Server.
- [Modelo de datos propuesto](docs/modelo-de-datos-propuesto.md): entidades, relaciones y estrategia de guardado y recuperación.
- [Análisis técnico](docs/analisis-tecnico.md): alternativas, decisiones y fuentes técnicas verificadas.
- [Manual técnico H3](docs/NEXUS-manual-tecnico.md): explicación del código de consola y preparación para el examen.
- [Diagramas existentes](docs/diagramas.md): clases y flujo de exploración de H3.
- [Evidencias visuales](docs/evidencias/): bocetos y diagramas de la propuesta.

## Estructura principal

- Program.cs: código actual del juego.
- NEXUS 2.0.csproj: aplicación de consola original.
- nexus visual/: primer menú gráfico de Windows Forms, integrado en la solución y con referencia al proyecto NEXUS original.
- NEXUS 2.0.slnx: solución que contiene ambos proyectos.
- README.md: presentación y ejecución.
- docs/: manual, propuesta y evidencias.

La versión gráfica reutiliza la clase `Cadete` como vínculo con el ensamblado del juego y permite iniciar el simulador original. Las funciones de persistencia y las tablas SQL siguen siendo propuestas para una etapa futura.

## GitHub Desktop

Para trabajar con el repositorio, se puede clonarlo desde GitHub o agregar una carpeta local con GitHub Desktop. Consulta la [guía oficial para agregar un proyecto existente](https://docs.github.com/es/desktop/adding-and-cloning-repositories/adding-an-existing-project-to-github-using-github-desktop) y la [guía para revisar y confirmar cambios](https://docs.github.com/es/desktop/making-changes-in-a-branch/committing-and-reviewing-changes-to-your-project).
