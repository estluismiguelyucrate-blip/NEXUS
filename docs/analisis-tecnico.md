# Análisis técnico y decisiones

## Lo que investigué

El proyecto actual está configurado como aplicación de consola para net10.0. Windows Forms no se activa solo agregando un formulario. Para la aplicación gráfica se requiere un destino Windows, por ejemplo net10.0-windows, y habilitar UseWindowsForms. Así lo describe [la referencia de propiedades del SDK de escritorio](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props-desktop). .NET 10 es LTS y tiene soporte hasta noviembre de 2028, según [versiones y soporte de .NET](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support).

El proveedor de EF Core para SQL Server se llama Microsoft.EntityFrameworkCore.SqlServer. La guía muestra cómo configurarlo con UseSqlServer y aclara que usa Microsoft.Data.SqlClient por debajo: [proveedor de SQL Server](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/). La tabla oficial de versiones indica que EF Core 10 corresponde a .NET 10, así que recomendaría usar una versión 10.x compatible y revisar notas antes de instalar: [versiones de EF Core](https://learn.microsoft.com/en-us/ef/core/what-is-new/).

## Alternativas consideradas

| Decisión | Alternativas | Elección | Motivo |
|---|---|---|---|
| Interfaz | Consola, Windows Forms, WPF, Unity | Windows Forms | Es la tecnología indicada en la práctica y permite hacer ventanas con controles. El mapa H3 se puede mostrar con 20 botones en una cuadrícula. |
| Mostrar mapa | Dibujar cada píxel, usar imágenes, botones en TableLayoutPanel | Botones al principio | Es más fácil cambiar el estilo, marcar la posición y seleccionar una casilla. Se puede hacer dibujo personalizado cuando las reglas estén firmes. |
| Acceso a SQL | ADO.NET, EF Core, archivos JSON | EF Core con SQL Server | El modelo tiene partidas, objetivos e inventario relacionados. JSON podría servir para un prototipo sin servidor, pero no cumple el requisito de SQL Server. |
| Organización del código | Toda la lógica en los formularios, separar responsabilidades | WinForms + Core + Data | Cambiar una ventana no debería obligar a volver a escribir las reglas. La guía de .NET explica la separación entre interfaz, lógica y acceso a datos: [arquitectura en capas](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures). |
| Base para aprender | Servidor remoto, SQL Server Express LocalDB | LocalDB al desarrollar | Es una instancia ligera para desarrollo local. Una base compartida requeriría un servidor y configuración aparte: [LocalDB](https://learn.microsoft.com/es-es/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver17). |

Windows Forms es una tecnología de interfaz de escritorio para Windows con formularios, controles y eventos. Para este mapa por casillas me parece adecuada. No es un motor de juego 3D; si en el futuro se quisiera combate en tiempo real, animaciones complejas o varias plataformas, habría que comparar de nuevo. WPF también es para escritorio pero tiene otra forma de diseñar ventanas. Unity es más apropiado si el objetivo cambiara a un videojuego con un motor y un flujo de trabajo distinto, aunque ya no cumpliría la tecnología pedida.

## Decisiones recomendadas

1. Mantener primero las reglas H3: mapa 4 × 5, energía, estabilidad, inventario, anomalías y extracción.
2. Separar Console de las clases de juego. Las reglas deben devolver resultados y la ventana mostrar el mensaje.
3. Crear Nexus.Core, Nexus.Data y Nexus.WinForms; evitar consultas SQL desde los botones.
4. Empezar con una misión, un Cadete por partida y LocalDB. No añadir cuentas en línea, equipos o sincronización todavía.
5. Guardar con un repositorio y EF Core. Llamar SaveChangesAsync al guardar o al llegar a un punto de control y mostrar un mensaje si SQL Server no responde.
6. Guardar cadena de conexión y credenciales fuera del repositorio. El .gitignore ya excluye archivos appsettings.*.Local.json y .env; revisar los archivos antes de publicar.
7. No rehacer todo al mismo tiempo: primero separar reglas, luego mostrar el mapa y al final guardar/recuperar.

## Comprobación de información sugerida por IA

Tomé la IA como apoyo para ordenar ideas, no como fuente final. Revisé estas recomendaciones con Microsoft Learn:

| Idea inicial | Qué comprobé | Decisión |
|---|---|---|
| “Podemos agregar Windows Forms y mantener la aplicación gráfica para cualquier sistema operativo.” | El destino del SDK de escritorio es Windows específico. | La futura versión gráfica será para Windows; el proyecto de consola queda como base hasta migrarlo. |
| “Entity Framework funciona igual con cualquier versión de .NET.” | EF Core publica compatibilidad por versión; EF Core 10 corresponde a .NET 10 y hay un proveedor de SQL Server. | Se propone EF Core 10.x junto con .NET 10 y se comprobarán actualizaciones. |
| “Al guardar quizá queda el inventario sí, pero la misión no.” | EF Core aplica SaveChanges como transacción para proveedores compatibles. | Guardar el estado de una partida en la misma operación; si se divide en varias, revisar transacciones. |
| “LocalDB permite compartir una partida por Internet.” | La documentación la describe como instancia local ligera de SQL Server Express. | Usarla para aprender en una computadora; el juego compartido necesitaría una base accesible por red. |
| “La IA puede decir qué clases existen en el proyecto.” | Revisé Program.cs y encontré las clases, la matriz y las reglas reales. | Los datos actuales vienen del código; formularios, capas y tablas son propuesta futura. |

La verificación no significa que ya se haya probado una interfaz o una base de datos. Esas partes todavía no están en el repositorio.

## Fuentes técnicas consultadas

Revisión: 26 de octubre de 2026. Son fuentes primarias de Microsoft:

- [Descripción general de Windows Forms](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/overview/)
- [Tutorial para crear una aplicación Windows Forms](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/get-started/create-app-visual-studio?view=netdesktop-9.0)
- [Propiedades del SDK de aplicaciones de escritorio](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props-desktop)
- [Versiones y soporte de .NET](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support)
- [Versiones y ciclos de EF Core](https://learn.microsoft.com/en-us/ef/core/what-is-new/)
- [Proveedor EF Core para SQL Server](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/)
- [Guardar datos en EF Core](https://learn.microsoft.com/en-us/ef/core/saving/basic)
- [Claves primarias y foráneas en SQL Server](https://learn.microsoft.com/en-us/sql/relational-databases/tables/primary-and-foreign-key-constraints?view=sql-server-ver17)
- [SQL Server Express LocalDB](https://learn.microsoft.com/es-es/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver17)
- [Arquitecturas comunes en capas para .NET](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)