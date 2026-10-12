# Diagramas de NEXUS

Los diagramas describen la versión de `Program.cs` revisada el 12 de octubre de 2026. Las imágenes SVG y los bloques Mermaid permiten consultarlos y editarlos; también se conservan imágenes PNG para generar la edición PDF.

## Diagrama de clases

![Diagrama de clases de NEXUS](diagrama-clases.svg)

```mermaid
classDiagram
    class Personaje {
        <<abstract>>
        -int _energia
        +string Nombre
        +int EnergiaMax
        +int Energia
        +int Ataque
        +bool Derrotado
        +string Tipo
        +CambiarEnergia(int)
        +RecibirDano(int) int
        +Reaccionar(Cadete, bool)
        +Mostrar(bool)
    }
    class Cadete {
        -int _estabilidad
        -int _escudo
        -int _descansos
        +int XP
        +Inventario Inventario
        +Atacar(Personaje)
        +Descansar()
    }
    class Robot
    class Enemigo
    class Objeto {
        <<abstract>>
        +string Nombre
        +string Descripcion
        +Usar(Cadete, Personaje) bool
    }
    class CelulaEnergia
    class Escudo
    class ModuloAtaque
    class Analizador
    class Inventario {
        -List~Objeto~ _objetos
        +Agregar(Objeto) bool
        +Consultar()
        +Tiene~T~() bool
        +Utilizar(int, Cadete, Personaje) bool
        +Eliminar(int)
    }
    class Mapa
    class Mision
    class Simulador
    class Encuentro
    class Fabrica
    class Program

    Personaje <|-- Cadete
    Personaje <|-- Robot
    Personaje <|-- Enemigo
    Objeto <|-- CelulaEnergia
    Objeto <|-- Escudo
    Objeto <|-- ModuloAtaque
    Objeto <|-- Analizador
    Cadete *-- Inventario : crea y posee
    Inventario o-- "0..6" Objeto : almacena
    Simulador *-- Mapa
    Simulador *-- Mision
    Simulador o-- Cadete
    Encuentro ..> Personaje : usa
    Encuentro ..> Cadete : afecta
    Fabrica ..> Objeto : crea
    Fabrica ..> Personaje : crea
    Program ..> Simulador : inicia
```

La línea de triángulo hueco indica herencia. El rombo lleno marca la composición de `Cadete` con su inventario y del simulador con mapa/misión. La lista del inventario mantiene una agregación de hasta seis referencias de tipo `Objeto`. Las flechas punteadas muestran dependencias de uso, no herencia.

## Flujo de movimiento y exploración

![Flujo de movimiento y exploración](flujo-exploracion.svg)

```mermaid
flowchart TD
    A[Menú: elegir Moverse] --> B[Leer dirección W A S D]
    B --> C{¿Dirección válida?}
    C -- No --> X[Informar error; termina movimiento]
    C -- Sí --> D[Calcular nueva fila y columna]
    D --> E{¿Dentro del mapa?}
    E -- No --> Y[Informar límite; conservar posición y energía]
    E -- Sí --> F[Actualizar posición; energía -2]
    F --> G{¿Base?}
    G -- Sí --> H[Informar zona segura]
    G -- No --> I{¿Ya visitada?}
    I -- Sí --> J[Informar que no hay novedades]
    I -- No --> K[Marcar visitada]
    K --> L{Tipo de celda}
    L -- Recurso --> M{¿Coordenadas del núcleo?}
    M -- Sí --> N[Marcar núcleo encontrado]
    M -- No --> O[Generar objeto aleatorio]
    O --> P{¿Inventario tiene espacio?}
    P -- No --> Q[No agregar; permitir volver a intentar]
    P -- Sí --> R[Agregar objeto]
    L -- Anomalía --> S[Estabilidad -12; energía -5; marcar objetivo]
    S --> T{¿Entidad ya superada?}
    T -- No --> U[Resolver encuentro]
    T -- Sí --> V[Continuar partida]
    L -- Vacío --> W[Resolver evento aleatorio]
```

El flujo sigue `Simulador.Mover`, `Mapa.Mover` y `Simulador.Explorar`. Los eventos aleatorios y el combate pueden tener resultados diferentes entre ejecuciones.

