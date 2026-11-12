# Lluvia Estelar

Juego 2D hecho en Unity 6 (6000.6.2f1, plantilla 2D URP). Los scripts se escribieron en Visual Studio Code con ayuda de IA.

Controlas una nave que debe esquivar meteoritos y recoger estrellas. La dificultad va subiendo con el tiempo.

## Cómo jugar

1. Abre el proyecto en Unity Hub.
2. Abre la escena `Assets/Scenes/SampleScene`.
3. Presiona **Play**.

| Acción | Teclas |
|---|---|
| Moverse | Flechas o WASD (o el stick izquierdo de un gamepad) |
| Empezar / reiniciar | Espacio, Enter o clic |
| Pausa | Esc o P |

- Sobrevivir: +10 puntos por segundo
- Estrella: +50 puntos
- Meteorito: −1 vida (tienes 3)
- El récord se guarda con `PlayerPrefs`

## Estructura

```
Assets/Scripts/
├── GameManager.cs       # Arma la escena, controla los estados, aparición de objetos e interfaz
├── PlayerController.cs  # Movimiento de la nave (Input System)
├── FallingObject.cs     # Meteoritos/estrellas que caen + partículas de explosión
└── SpriteFactory.cs     # Sprites generados por código (sin imágenes externas)
```

El juego se monta solo al darle Play, con `[RuntimeInitializeOnLoadMethod]`, así que no hay que configurar nada en el editor.
