# NEXUS 2.0 — Sistema del Monarca

NEXUS es un juego de exploración y combate para Windows, construido con C# y Windows Forms. Esta versión incluye una interfaz original inspirada en la estética de Solo Leveling, con portales, paneles del Sistema y energía violeta.

## Jugar después de clonar

Requisitos: Windows x64, Git, .NET 10 SDK e internet en el primer inicio para restaurar los paquetes NuGet.

```powershell
git clone https://github.com/estluismiguelyucrate-blip/NEXUS.git
cd NEXUS
.\Iniciar-NEXUS.bat
```

El script restaura las dependencias y abre el juego. También puedes abrir `NEXUS 2.0.slnx` en Visual Studio y presionar **F5**.

## Expediciones

- Elige entre **Ruinas del Umbral**, **Bosque Carmesí** y **Ciudad Sumergida**.
- Al crear un cazador, mira una cinemática introductoria con la historia del despertar y la aparición de los tres portales. Pulsa **Saltar** o Esc para continuar de inmediato.
- Recorre los calabozos con movimiento continuo en tiempo real, cámara que sigue al cazador, encuentros con enemigos y escenarios propios para cada mapa.
- Usa WASD o las flechas para moverte, Shift para impulsarte/esquivar, J o Espacio para atacar y Q para huir de un encuentro.
- Investiga anomalías, combate al Guardián, reúne objetos y recupera el núcleo.
- Administra energía, estabilidad, inventario y experiencia; luego vuelve a la extracción.
- Guarda o recupera partidas en SQL Server o LocalDB desde la configuración.

La conexión a SQL Server es opcional para iniciar y jugar. Se necesita para guardar o continuar partidas. La cadena predeterminada usa `Server=(localdb)\MSSQLLocalDB;Database=NEXUS;Trusted_Connection=True;TrustServerCertificate=True;`.

## Descargar el instalador

GitHub Actions genera un instalador de Windows en cada actualización de `main`. En el repositorio, abre **Actions**, elige la ejecución correcta de **NEXUS Windows Installer** y descarga el artefacto `NEXUS-Setup-v2.0.0`.

## Proyecto

- `MainForm.cs`: menú, selección de expedición y conexión SQL.
- `GameView.cs`: HUD adaptable al tamaño de ventana, mapa, inventario y pausa.
- `SystemTheme.cs`: estilo compartido de ventanas y avisos.
- `GameSession.cs` y `Program.cs`: estado de la partida y reglas del juego.
- `assets/`: logotipo e icono de NEXUS.

La solución principal es `NEXUS 2.0.slnx`. La carpeta `nexus visual` conserva un lanzador heredado y no forma parte de esa solución.

