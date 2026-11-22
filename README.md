# NEXUS 2.0

NEXUS es una aventura de exploración de ciencia ficción en C# con interfaz de escritorio Windows Forms y guardado de partidas en SQL Server.

## Funciones disponibles

- Menú principal con inicio y continuación de partida.
- Pantalla de exploración con mapa interactivo 4 × 5, sectores descubiertos, posición y tipos de zona.
- Reglas de combate con opciones de atacar, analizar, usar un objeto o escapar.
- Inventario interactivo: añadir, usar y descartar células de energía, escudos, analizadores y módulos.
- Sucesos de exploración, daños, experiencia, descanso limitado, objetivos de misión y extracción.
- Guardado y recuperación del estado completo de la partida como JSON en SQL Server.
- Ficha de cadete para iniciar una expedición y controles WASD o flechas.
- Configuración y prueba de conexión a SQL Server.

La interfaz de escritorio conserva el mapa H3 y conecta el ciclo de exploración, combate, inventario y extracción a las clases originales del juego. El mapa mostrado tiene 4 × 5 zonas; el PDF de la misión no exige expresamente tres mapas separados. Si hay un documento adicional con mapas distintos, se pueden integrar al recibirlo.

## Requisitos y ejecución

- Windows y .NET 10 SDK con el componente Windows Forms.
- SQL Server o SQL Server Express LocalDB para probar el guardado.
- El paquete `Microsoft.Data.SqlClient` se restaura desde NuGet.

Desde la carpeta raíz:

```powershell
dotnet run --project "NEXUS 2.0.csproj"
```

La conexión predeterminada usa `Server=(localdb)\MSSQLLocalDB;Database=NEXUS;Trusted_Connection=True;TrustServerCertificate=True;`. Si tu instancia es distinta, abre **Conexión SQL Server** y cambia la cadena. La aplicación crea la base indicada y la tabla de partidas si no existen y si la cuenta SQL tiene permisos.

## Documentación

La carpeta `docs/` conserva el diseño y análisis de la versión de consola H3 y sus propuestas de evolución. Los bocetos son referencias visuales; la nueva pantalla ejecutable está en `MainForm.cs`.

