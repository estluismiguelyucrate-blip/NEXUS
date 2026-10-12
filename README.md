# NEXUS 2.0

NEXUS es una aventura de exploración por consola escrita en C#. El jugador crea un cadete, recorre un mapa de celdas, administra energía y estabilidad, recoge objetos, se enfrenta a entidades y completa una misión de extracción.

## Requisitos

- Visual Studio con desarrollo de escritorio .NET, o el SDK de .NET 10.
- Windows, macOS o Linux con una terminal interactiva.

## Ejecutar

Desde la carpeta raíz del repositorio:

```sh
dotnet run --project "NEXUS 2.0.csproj"
```

En Visual Studio, abre `NEXUS 2.0.slnx`, establece el proyecto como inicio y ejecuta con **Iniciar**. El programa espera entrada por consola y al finalizar pide una tecla para cerrar.

## Estructura

- `Program.cs`: aplicación completa; contiene el modelo del juego, reglas, menús y punto de entrada.
- `NEXUS 2.0.csproj`: aplicación de consola para `net10.0`; no declara paquetes NuGet externos.
- `NEXUS 2.0.slnx`: solución de Visual Studio.
- [`docs/NEXUS-manual-tecnico.md`](docs/NEXUS-manual-tecnico.md): arquitectura, explicación de POO, análisis del flujo y guía de estudio H3.
- [`docs/diagramas.md`](docs/diagramas.md): diagramas de clases y del proceso de exploración.

## GitHub Desktop

El repositorio ya está publicado como público. Para trabajar desde GitHub Desktop, clónalo desde GitHub o agrega la carpeta local existente y luego revisa los cambios antes de confirmar y subirlos. Consulta la [guía oficial de GitHub Desktop para agregar un proyecto](https://docs.github.com/es/desktop/adding-and-cloning-repositories/adding-an-existing-project-to-github-using-github-desktop) y la [guía para confirmar y revisar cambios](https://docs.github.com/es/desktop/making-changes-in-a-branch/committing-and-reviewing-changes-to-your-project).

## Alcance del código

El proyecto usa clases abstractas, herencia, sobrescritura de métodos, composición y `List<Objeto>`. En la versión documentada no hay interfaces implementadas ni consultas LINQ; el manual incluye ejemplos complementarios para estudiarlas y los señala como tales.

## Licencia

No se ha especificado una licencia. Consulta al autor antes de reutilizar o redistribuir el código.

