# ROADMAP Y DOCUMENTACIÓN V0.1: FUNDAMENTOS MULTIJUGADOR

## 🏆 Resumen de lo alcanzado (Versión 0.1)

Hemos construido los cimientos más difíciles de cualquier juego multijugador asimétrico estilo "Hombre Lobo": **La Arquitectura Autoritaria de Red**.

### 1. Sistema de "Lobby a Partida" Seguro (Anti-Cheat)
*   **Aparición Retrasada:** Los jugadores no entran físicamente al mundo al conectarse. El servidor intercepta su conexión y los mantiene invisibles en un "Lobby de Espera".
*   **Autoridad del Host:** Diferenciación estricta entre opciones del Cliente y del Servidor. Solo el Servidor (el dueño de la casa) tiene permiso físico y de código para dictar el cambio de escena hacia la partida real.
*   **Transmisión Sincronizada de Escenas:** Configurado el `NetworkManager.SceneManager` para abducir de forma atestiguada tanto a Hosts como a Clientes pacíficos desde el `Scene_Menu` hacia `Scene_Gameplay`.

### 2. Gestión de Entidad y Cámara Locales (`NetworkPlayerSetup`)
*   **Posesión de Cuerpos:** Solucionado el problema fantasma en el que un jugador podía controlar a todos en pantalla. Ahora, el `ThirdPersonController` de Unity detecta si eres o no el "Dueño" de la sangre del modelo a través de `IsOwner`.
*   **Cámaras V2 Aisladas:** La cámara cinemática inyecta dinámicamente tu vista solo a *tu* esqueleto local, previniendo caos visual al unirse Clientes adicionales.

### 3. Sincronizador de Animaciones (`PlayerAnimationSync`)
*   Logramos sincronizar el delicado sistema de Animación del `StarterAssets`. 
*   Todas las variables complejas (BlendTrees de Velocidad, Estados del Salto y Caída Libre) viajan bajo las normas Autorizadas del Servidor (`ServerRpc` -> `NetworkVariable`).

### 4. Inteligencia Central (`GameManager`) y Fases del Juego
*   El `GameManager` evolucionó a una "Arca de Noé" (`DontDestroyOnLoad`). Él vigila cuándo las pantallas terminan de dibujarse para luego inyectar, de forma individual, sillas físicas a través del `SpawnManager`.
*   **Sistema Criptográfico (ClientRpcParams):** Primer despliegue del sistema de asignación de roles. El Servidor decide que 1 jugador es Lobo y tira una carta digital que, matemáticamente, solo la Consola de la víctima puede descifrar, negando Hacks a otros jugadores.

---

## 🗺️ HOJA DE RUTA AL ALFA (V1.0) - Próximos Pasos

Esta hoja detalla las siguientes versiones y mecánicas que hay que atacar para transformar este esqueleto técnico en un juego de engaño funcional.

### [V0.2] Fases Temporales y UI del Ciclo Principal
- [x] Mostrar Textos en Pantalla "¡Eres el LOBO!" o "Aldeano" al aparecer en partida.
- [x] Construir un Reloj Maestro (Server-Side).
- [x] Mecánica de "Noche Escurecida": Cambio de iluminación global sincronizada en base a `NetworkVariable<GamePhase>`.
- [x] Paralizar los movimientos (`CharacterController.enabled = false`) y forzar la reaparición del ratón de Windows cuando se inician asambleas de Día/Noche.

### [V0.3] Caza y Combate a Escondidas (Habilidades asimétricas)
- [ ] Modificador de Visión: Permitir al "Lobo" ver nombres brillantes en la oscuridad usando máscaras de cámara.
- [x] Botón de Interacción Central (`E` o clic): Para asesinar (Si eres lobo) teniendo en cuenta el Anti-Stacking de muertes falsas.
- [x] Sincronización de Estado `[NetworkVariable<bool> IsDead]`: Transformar cadáveres asombrosamente en la red con Ragdolls o animaciones y bloquearles el movimiento de por vida.

### [V0.4] Interfaz del Sistema Social y Votación
- [x] Menú de Asamblea: Interfaz de Canvas estilo "Meeting" visible para vivos durante el Día, construida dinámicamente según quién sobrevive aún.
- [x] Lógica de Tabulación Array: ServerRpc desde el cuerpo del jugador (Owner) donde se envía un ID a condenar y el Servidor verifica por Diccionarios si cruza la mayoría de votos.
- [x] Pantalla de Finalización ("Los Lobos ganan" / "La Aldea Gana") basada en contador constante de Muertos vs Vivos en el equipo contrario.
- [x] Sistema de reseteo (Corrutina simultánea) para arrastrar a los clientes de regreso a `Scene_Menu` e invocar el borrado y Shutdown seguro del NetworkManager de la partida anterior.

### [V0.5] Refinamiento de Mundo y Audio
- [ ] Adaptación de escenarios 3D (Zonas oscuras, colisiones funcionales).
- [ ] Audio 3D Sincronizado, fundamental para esconder pisadas del Lobo por la noche o escuchar chillidos a la distancia.
- [ ] Preparación final para Build Multi-Ordenador Local u Online (Mediante Unity Relay).
