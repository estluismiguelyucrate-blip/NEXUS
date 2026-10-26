# NEXUS 2.0

NEXUS es una aventura de exploración por consola hecha en C#. El jugador crea un Cadete, recorre un mapa, administra energía y estabilidad, recoge objetos, enfrenta entidades y trata de completar una misión de extracción.

## Estado actual

La versión H3 implementa una misión y un mapa fijo de 4 × 5 en consola. Tiene combate por turnos, anomalías, objetos, inventario limitado, objetivos y extracción. El programa todavía no tiene ventanas gráficas ni guarda partidas en una base de datos.

## Tecnologías

- C# y .NET 10, con destino net10.0.
- Aplicación de consola; el proyecto actual no declara paquetes NuGet externos.
- La siguiente etapa propuesta es Windows Forms con SQL Server y Entity Framework Core. Esa etapa aún no está implementada.

## Cómo ejecutar la versión actual

Se necesita el SDK de .NET 10. Desde la carpeta raíz:

    dotnet run --project "NEXUS 2.0.csproj"

También se puede abrir NEXUS 2.0.slnx en Visual Studio con las herramientas de .NET instaladas. La aplicación espera que se ingresen las opciones en la consola.

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
- NEXUS 2.0.csproj y NEXUS 2.0.slnx: proyecto y solución actuales.
- README.md: presentación y ejecución.
- docs/: manual, propuesta y evidencias.

La propuesta parte de las clases que ya están en Program.cs. Los formularios, las capas y las tablas SQL son ideas para implementar después, no funciones que ya se puedan ejecutar.

## GitHub Desktop

Para trabajar con el repositorio, se puede clonarlo desde GitHub o agregar una carpeta local con GitHub Desktop. Consulta la [guía oficial para agregar un proyecto existente](https://docs.github.com/es/desktop/adding-and-cloning-repositories/adding-an-existing-project-to-github-using-github-desktop) y la [guía para revisar y confirmar cambios](https://docs.github.com/es/desktop/making-changes-in-a-branch/committing-and-reviewing-changes-to-your-project).