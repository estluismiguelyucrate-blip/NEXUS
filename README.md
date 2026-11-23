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
- El juego abre en pantalla completa; pulsa **F11** o **Alt+Enter** para cambiar de modo.
- La banda sonora ambiental original de NEXUS se crea al iniciar y se guarda en `%LOCALAPPDATA%\NEXUS\audio`. Pulsa **M** o el control de música del menú/HUD para silenciarla.
- Haz clic en el mapa para caminar hasta ese punto.
- Investiga anomalías, combate al Guardián, reúne objetos y recupera el núcleo.
- Administra energía, estabilidad, inventario y experiencia; luego vuelve a la extracción.
- Guarda y continúa partidas localmente; SQL Server es opcional para copias sincronizadas.

Los guardados locales se almacenan en `%LOCALAPPDATA%\NEXUS\savegame.json`. SQL Server es opcional para copias sincronizadas; LocalDB no es requisito para jugar o guardar.

## Descargar el instalador

GitHub Actions genera un instalador y un paquete Windows x64 autocontenido en cada actualización de `main`. En **Actions**, descarga `NEXUS-Setup-v2.0.0` para instalar en Windows o `NEXUS-Steam-Windows-x64` para preparar la carga mediante SteamPipe. El paquete no reemplaza la configuración de App ID, depot, ficha, precio ni revisión en Steamworks.

## Proyecto

- `MainForm.cs`: menú, selección de expedición y conexión SQL.
- `GameView.cs`: HUD adaptable al tamaño de ventana, mapa, inventario y pausa.
- `SystemTheme.cs`: estilo compartido de ventanas y avisos.
- `GameSession.cs` y `Program.cs`: estado de la partida y reglas del juego.
- `assets/`: logotipo e icono de NEXUS.
- `docs/steam-release.txt`: instrucciones para cargar el paquete Windows en SteamPipe.

La solución principal es `NEXUS 2.0.slnx`. La carpeta `nexus visual` conserva un lanzador heredado y no forma parte de esa solución.

