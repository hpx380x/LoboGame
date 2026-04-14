# ðŸ“œ HISTORIAL DE CAMBIOS (CHANGELOG)

Este documento registra todas las intervenciones tÃ©cnicas, parches y cÃ³digo generado o refactorizado por la IA. 
Su objetivo es garantizar que el usuario sepa **exactamente quÃ© archivos fueron alterados y por quÃ©**, evitando modificaciones "fantasma".

---
## [2026-04-12] - Inmersión en Lobby y Cámara Libre
- **LobbyHeadLook.cs**: 
    - Activación de Cámara Libre (Free Look) sin necesidad de clics.
    - Bloqueo automático del cursor al aparecer en el lobby.
- **LobbyRaycastInteraction.cs**: 
    - [MEJORA] Búsqueda robusta del HUD: ahora encuentra el `LobbyInteractionHUD` incluso si está desactivado en la jerarquía.
    - [MEJORA] Auto-configuración de Capas: Si no se configuran en el Inspector, el script usa automáticamente Default y Player (Capa 8).
    - [MEJORA] Detección de Hoguera: Mejorada la detección para captar clics en las rocas o hijos de la hoguera.
    - Sistema de interacción inmersivo basado en Raycast (Mirada). Detecta jugadores y hoguera para lanzar acciones.
- **LobbyInteractionUI.cs** (NUEVO): HUD contextual con barra circular de progreso (Radial Fill) para "Mantener X/E".
- **GameManager.cs**: Implementación de `KickPlayerServerRpc` para permitir al Host expulsar jugadores físicamente desde el lobby.

## [2026-04-12] - Sistema de Lobbies y Navegador de Servidores
- **LobbyUI.cs**: 
    - Integrado `Unity.Services.Lobbies`.
    - Implementada creación de partidas Públicas y Privadas (vía Toggle).
    - Añadido `JoinPanel` para buscar y listar servidores activos (`RefreshServerList`).
    - Añadido sistema de Heartbeat (`SendHeartbeatPingAsync`) en `Update` para mantener vivo el servidor en la nube.
- **PlayerLobbyPose.cs**: 
    - Corregido el problema de movimiento en el Lobby; ahora desactiva el `CharacterController` y `ThirdPersonController` de raíz.
    - Se asegura de forzar el parámetro `isSitting` en el Animator.
    - Añadido control de escena para evitar que el jugador se quede sentado en la partida real.
- **LobbyHeadLook.cs**: 
    - Implementada lógica de "Prioridad de Cámara": apaga la cámara principal de la escena al entrar al asiento para usar la vista FPS.
    - Limpieza automática: reactiva la cámara principal al salir o desconectarse.
- **NetworkPlayerSetup.cs**: Corregido aviso de Cinemachine inexistente al iniciar el personaje en la escena de Lobby.
    - Corregidos warnings de métodos obsoletos usando la nueva sintaxis `[Rpc(SendTo.Server, ...)]` de Unity 6.
    - Eliminada variable privada sin uso `_personajeLocalInstanciado`.


## [2026-04-11]
- **LobbyPlayerSpawner**: Convertido a NetworkBehaviour. Sincroniza asientos por ClientId (Host=0).
- **LobbyHeadLook.cs** (NUEVO): Sincronización de mirada en red (NetworkVariable) para lobby 1ª persona.
- **PlayerArmature**: Integrada cámara FP y script de mirada inteligente en el Prefab.
- **LobbyUI.cs**: Eliminadas llamadas locales redundantes de spawn para favorecer Netcode.



## [2026-04-05] - Misiones HÃ­bridas y Roles de Oficio
### âš™ï¸ Arquitectura y Gameplay
- **Enums/GameEnums.cs**: AÃ±adido `RolAldea` y tipos de misiones avanzadas (Wires, Candle Puzzle, Crafting).
- **PlayerState.cs**: Herencia de oficios (drop de items al morir) y sincronizaciÃ³n de red para roles.
- **GameManager.cs**: AsignaciÃ³n aleatoria de roles de aldea (Herrero) al inicio de la fase.
- **JobItem.cs** *(NUEVO)*: Sistema de recolecciÃ³n de herramientas caÃ­das para heredar oficios.
- **QuestInteractable.cs**: Refactoring para soporte completo de red y Despawn autoritativo.
- **SpawnAreaManager.cs** *(NUEVO)*: Generador de objetos por Ã¡rea para misiones de recolecciÃ³n.
- **PuzzleSequenceManager.cs** & **PuzzleCandle.cs** *(NUEVOS)*: Sistema Simon-Says para puzzles fÃ­sicos.
- **ForgeController.cs** & **CraftingStation.cs** *(NUEVOS)*: MecÃ¡nica social de la Forja (humo interactivo) y crafteo de la Daga Legendaria.
- **MinigameTrigger.cs** *(NUEVO)*: Disparador modular para abrir interfaces de minijuegos (preparado para integraciÃ³n externa).
- **Renombramiento**: Carpeta de tareas web renombrada a `TaskTapestryHtml`. Solo queda el minijuego de "Cuerdas" activo.
- **RemociÃ³n**: Eliminado prototipo de minijuego de cables anterior por solicitud del usuario para integraciÃ³n propia (Google Studio).

---
## [2026-04-11]
- **Asientos_Lobby**: Reorganización total de 10 asientos y 5 troncos (Orden 1-5 Horario).
- **LobbyPlayerSpawner**: Sincronizada la lista de spawn con el nuevo orden de asientos.
- **LobbyCameraManager.cs**: Refactorización completa (Cámara suave sin Cinemachine).
- **LobbyUI.cs**: Integración de personaje local en lobby dinámico.


## [2026-04-05] - Refactor: DestrucciÃ³n del God Object
### ðŸ› ï¸ RefactorizaciÃ³n Estructural
- **PlayerState.cs**: Troceado magistralmente de 1000 lÃ­neas a ~200 lÃ­neas. Reducido estrictamente a manejar identidad base (`isWolf`, `isDead`) y el ragdoll.
- **PlayerInventory.cs** *(NUEVO)*: Centraliza la lÃ³gica de economÃ­a (`monedas`) y el Ã­tem equipado en la mano, con sus mallas visuales.
- **PlayerStatusEffects.cs** *(NUEVO)*: Concentra los Buffs y Debuffs (Veneno, Silenciadores, Stun), descongestionando el sistema de vida.
- **PlayerItemController.cs** *(NUEVO)*: Abstrae la kilomÃ©trica lÃ³gica del "Switch" usado al pulsar [F]. Cada bomba apestosa y manzana recae en su propio cerebro.
- **GameEnums.cs** *(NUEVO)*: Movido globalmente `TipoObjeto` para evitar referencias circulares restrictivas en el proyecto.
- **FixPlayerInventoryPrefab.cs** *(NUEVO)*: Script de Editor (Tools) creado para automatizar la asignaciÃ³n exhaustiva de los objetos 3D (Daga, Antorcha, PociÃ³n) al componente PlayerInventory dentro de `PlayerArmature` sin intervenciÃ³n manual.

---
## [2026-04-11]
- **Asientos_Lobby**: Reorganización total de 10 asientos y 5 troncos (Orden 1-5 Horario).
- **LobbyPlayerSpawner**: Sincronizada la lista de spawn con el nuevo orden de asientos.
- **LobbyCameraManager.cs**: Refactorización completa (Cámara suave sin Cinemachine).
- **LobbyUI.cs**: Integración de personaje local en lobby dinámico.


## [2026-04-05] - EstabilizaciÃ³n de Controles e IngenierÃ­a de Red
### ðŸ› Bug Fixes (Correcciones)
- **Player Input**: Corregida la amnesia del `PlayerArmature`. Reasignado el archivo `StarterAssets.inputactions` al `PlayerInput` del Prefab para restaurar el movimiento (WASD).
- **Auto-Healing de CÃ¡mara**: Editado `NetworkPlayerSetup.cs`. Al cargar la escena de juego, si Unity 6 desvincula a Cinemachine, el script invoca e inyecta dinÃ¡micamente un `CinemachineBrain` a la MainCamera, vinculando al clon con la `Virtual Camera`.
- **Pergamino (Scroll)**: Editado `ScrollController.cs`. AÃ±adida compatibilidad con red local y recuperaciÃ³n de control UI para testear sin ser "DueÃ±o".

### ðŸ”Ž AuditorÃ­a
- Generado el reporte `audit_report_v0.1.md`.
- Analizado el rendimiento general: Sistema "Sano" (sin bÃºsquedas perjudiciales en los Updates de forma recurrente).

### ðŸš© Deuda TÃ©cnica Detectada
- `PlayerState.cs` necesita dividirse urgente (Modo God-Object detectado con mÃ¡s de 1000 lÃ­neas).
- `PlayerArmature.prefab` contiene 177 hijos; urge vaciarlo instanciando objetos en tiempo de ejecuciÃ³n.

---
## [2026-04-11]
- **Asientos_Lobby**: Reorganización total de 10 asientos y 5 troncos (Orden 1-5 Horario).
- **LobbyPlayerSpawner**: Sincronizada la lista de spawn con el nuevo orden de asientos.
- **LobbyCameraManager.cs**: Refactorización completa (Cámara suave sin Cinemachine).
- **LobbyUI.cs**: Integración de personaje local en lobby dinámico.


## [Versiones Previas] - FundaciÃ³n V0.1
*Historial condensado de creaciÃ³n:*
- Setup completo de `NetworkManager` con Relay y Lobby UI.
- SincronizaciÃ³n del Controlador de Tercera Persona y BlendTrees de animaciÃ³n (`PlayerAnimationSync`).
- Autoridad estricta de Netcode sobre `IsLobo` y variables vitales (`PlayerState`).
- MecÃ¡nica asimÃ©trica de Raycast para cazar (Muerte instantÃ¡nea + `IsDead`).
- Sistema Canvas de Asambleas dinÃ¡mico basado en Vivos vs Muertos.

---
## [2026-04-11]
- **Asientos_Lobby**: Reorganización total de 10 asientos y 5 troncos (Orden 1-5 Horario).
- **LobbyPlayerSpawner**: Sincronizada la lista de spawn con el nuevo orden de asientos.
- **LobbyCameraManager.cs**: Refactorización completa (Cámara suave sin Cinemachine).
- **LobbyUI.cs**: Integración de personaje local en lobby dinámico.


## [2026-04-05] - Integracion Tapiz Modular (Tapestry)
### ?? Minijuegos e Interfaz Nativa
- **TaskPoint.cs**: Añadida estructura TapestryConnectionData y lista de estado para persistencia de hilos entre sesiones.
- **TapestryMinigame.cs** *(NUEVO)*: Implementacion nativa en C# (UI Toolkit) que replica la logica de arrastre de hilos, validacion de colores y audio procedural (White Noise + Tones).
- **TapestryMinigame.uxml / .uss** *(NUEVOS)*: Diseño visual tipo pergamino medieval con columnas dinamicas.
- **TapestryTask_UI.prefab** *(NUEVO)*: Prefab de interfaz configurado con UIDocument y script de persistencia.
- **TapestryTask_Zone.prefab** *(NUEVO)*: Prefab de mundo modular con Trigger para activar la tarea de reparacion de tapiz.

- Assets/Models/SM_Prop_Tapestry_Broken.fbx: Creado modelo fracturado en Blender con puntas triangulares.
- Assets/Prefabs/Tasks/TapestryTask_Zone 1.prefab: Version final del disparador con el modelo 3D integrado.

## [2026-04-05] - UniÃ³n MÃ¡gica Tapestry
- Assets/Scripts/UI/Minigames/TapestryRestorer.cs: Nuevo script de restauraciÃ³n 3D.
- Assets/Models/SM_Prop_Tapestry_Magic.fbx: Modelo con Blend Shapes.
- Assets/Prefabs/Tasks/TapestryTask_Zone_Magic.prefab: Prefab final con soporte mÃ¡gico.
- Assets/Scripts/UI/Minigames/TapestryMinigame.cs: IntegraciÃ³n de eventos OnMinigameWon.

- Assets/Scripts/Core/Network/HoldToStartAction.cs : Eliminado script de acción de mantener tecla E para el lobby.
- Assets/Scripts/UI/Menu/PlayerLobbyPose.cs : Creado script para pose estática de aldeano en el lobby.

## [2026-04-11] - Lobby In-Game: Camera Manager
### Cambios
- **LobbyCameraManager.cs**: Reescrito sin Cinemachine. Usa dos Transform ancla (Ancla_CamaraMenu, Ancla_CamaraLobby) con Lerp suave (SmoothStep) entre posiciones de la Main Camera.
- **Scene_Menu.unity**: Creado GameObject 'LobbyCameraManager' con hijos 'Ancla_CamaraMenu' y 'Ancla_CamaraLobby'. Referencia cameraManager conectada en LobbyUI.

## [2026-04-11] - Lobby In-Game: Spawn de Personaje y Cámara Dinámica
### Cambios
- **LobbyCameraManager.cs**: Reescrito con modo SeguirPersonaje() — la cámara sigue dinámicamente al Transform del jugador local via LateUpdate Lerp.
- **LobbyPlayerSpawner.cs** *(NUEVO)*: Spawner local (no red) del personaje del jugador en la escena de lobby. Instancia en asientos circulares alrededor de la hoguera.
- **LobbyUI.cs**: Integrado LobbyPlayerSpawner — llama SpawnPersonajeLocal() tras StartHost/StartClient y DestruirPersonajeLocal() al salir.
- **Scene_Menu.unity**: Creado Asientos_Lobby con 8 Transforms en círculo (radio 2.5m) alrededor de la hoguera. LobbyPlayerSpawner configurado con prefab PlayerArmature y lista de asientos.

## [2026-04-11] - Lobby: Asientos sobre Troncos Reales
### Cambios
- **Scene_Menu.unity**: Eliminados 8 asientos ficticios. Creados 5 Transforms (Asiento_Tronco0..4) encima de los TRONCO reales de Hoguera Lobby Spawn, mirando hacia el centro de la hoguera. LobbyPlayerSpawner actualizado con los 5 IDs correctos.

## [2026-04-11] - Lobby: 2 Asientos por Tronco (10 total)
### Cambios
- **Scene_Menu.unity**: Añadidos 5 asientos B (Asiento_TroncoXB), uno extra por cada tronco, desplazados a lo largo del eje del tronco. LobbyPlayerSpawner actualizado con los 10 asientos (A+B intercalados por tronco).

# #   [ 2 0 2 6 - 0 4 - 1 2 ] 
 -   M o d i f i c a d o   L o b b y C a m e r a M a n a g e r . c s :   A � a d i d a   a s i g n a c i � n   m a n u a l   d e   c � m a r a   y   r e p o s i c i o n a m i e n t o   a u t o m � t i c o   d e l   H U D   3 D   ( P e r g a m i n o )   p a r a   c o r r e g i r   e r r o r e s   d e   d i s t a n c i a   y   v i s i b i l i d a d   a l   i n i c i a r .  
 -   C o r r e g i d o   e r r o r   d e   s i n t a x i s   ( l l a v e s   f a l t a n t e s )   e n   L o b b y C a m e r a M a n a g e r . c s   q u e   i m p e d � a   l a   c o m p i l a c i � n .  
 -   [ H O T F I X ]   R e e s c r i t u r a   t o t a l   d e   L o b b y C a m e r a M a n a g e r . c s   p a r a   e l i m i n a r   e l   c o n f l i c t o   d e   p r i o r i d a d e s   c o n   C a m e r a F P S L o b b y   y   a s e g u r a r   l a   v i s i b i l i d a d   d e l   H U D   e n   e l   m e n � .  
 
## [2026-04-13] — Sesión: Fix HUD In-Game (Cámara Principal Nula)

### Causa Raíz
Cambios anteriores (Gemini Flash) usaban cam.gameObject.SetActive(false) sobre la Main Camera al spawnear el jugador en el lobby. Esto hacía que Camera.main devolviera null en todos los demás scripts (LobbyRaycastInteraction, etc.), rompiendo el raycast del HUD interactivo.

### Archivos Modificados
- LobbyHeadLook.cs — Cambio gameObject.SetActive(false) → cam.enabled = false (solo componente, no GO) para no romper Camera.main.
- LobbyRaycastInteraction.cs — Búsqueda de cámara en cascada: hijo → Camera.main → tag MainCamera. Re-búsqueda dinámica en Update si el jugador spawna después del Start.


## [2026-04-13] — Sesion: Eliminar movimiento de camara y pergamino

### Archivos Modificados
- `LobbyCameraManager.cs` — Eliminado metodo CorregirPosicionHUD() que movia el pergamino via transform. Eliminados campos inutilizados: pergaminoCanvas, distanciaAlHUD.


## [2026-04-13] — Session: Fix boton Host no pasaba a Lobby Interactions

### Problema
MostrarRoomPanel() solo activaba paneles UI pero no activaba la LobbyInteractionUI ni bloqueaba el cursor para el modo inmersivo de sala.

### Archivos Modificados
- `LobbyUI.cs` — MostrarRoomPanel() ahora activa LobbyInteractionUI, oculta joinPanel y bloquea el cursor (Locked) al entrar en sala.


### LobbyHeadLook
- Se añadió explícitamente fpCamera.gameObject.SetActive(IsOwner) para garantizar que, independientemente de cómo esté guardado el prefab, la cámara FPS se active al spawnear al jugador.


### Lobby UI y ParrelSync
- Se agregó soporte para **ParrelSync** en la inicialización de los servicios en la nube (UnityServices.InitializeAsync) dentro de \LobbyUI.cs\. Ahora cada clon (Player 2) usará un perfil único separado. Esto soluciona los cuelgues (congelamientos) o errores silenciosos que impedían al segundo jugador entrar a los menús de salas.


### Lobby UI y Cámara FPS
- Se agregó código en \LobbyHeadLook.cs\ para silenciar TODOS los AudioListener extra de la escena excepto el de la cámara FPS actual. Esto detiene el spam masivo de consola ('There are 2 audio listeners in the scene') que ocultaba el código de sala y otros Debug Logs vitales.
- En \LobbyUI.cs\ se añadieron catch blocks genéricos para asegurar que si el servicio de Lobby de Unity lanza un error no controlado (ej. límite de uso superado o mala conexión), el botón Host no se quede permanentemente bloqueado.


### Transición a Gameplay
- Se actualizó \GameManager.cs\ para que al cargar la escena del juego (\Scene_Gameplay\), Netcode fuerce la destrucción (Despawn) del \PlayerArmatureLobby\ antes de spawnear el personaje real. Esto evita errores debido a personajes duplicados o jugadores poseyendo erróneamente un muñeco del lobby.


### Personaje Gameplay
- Se añadió una regla en \PlayerState.cs\ (OnNetworkSpawn) que fuerza al parámetro \isSitting\ del Animator a volverse falso al cargar la partida. Esto soluciona el problema de que el muñeco apareciese deslumblando o atascado en la postura de la silla del lobby a la hora de jugar.


### Transición a Gameplay (Corrección)
- Se alteró \GameManager.cs\ para que el escaneo de limpieza busque específicamente los componentes \PlayerLobbyPose\ de la escena y aplique el *Despawn* directamente a la red. El método anterior basado en la abstracción \
etworkClient.PlayerObject\ fallaba porque el avatar de Lobby no utilizaba ese vínculo centralizado de Netcode.


### Transición a Gameplay (Mejora Crítica)
- Se reprogramó la eliminación de los muñecos \PlayerArmatureLobby(Clone)\ en \GameManager.cs\. Ahora la limpieza (\Despawn\) ocurre de inmediato al presionar el botón *Start Game* (antes del \LoadScene\), incluyendo aquellos objetos inactivos u ocultos en caché, en lugar de intentar borrarlos tras cargar la nueva escena, erradicando al 100% que puedan filtrarse a la partida.


### Transición a Gameplay (Fallo de Identificación)
- Se descubrió que el prefab \PlayerArmatureLobby\ en realidad NO contenía el script \PlayerLobbyPose\ que \LobbyPlayerSpawner.cs\ marcaba en su tooltip. Por eso la limpieza fallaba. Se actualizó \GameManager.cs\ para cazar estos clones por su nombre bruto (FindObjectsByType<NetworkObject> + '.name.Contains'), asegurando su exterminación ignorando qué scripts lleven o no.


### Lobby Animaciones
- Se agregó RandomLobbySit.cs para randomizar la animación sitting

### Lobby Animaciones (Fix Editor) 
- Se arregló un bug visual del editor en \RandomLobbySit.cs\ retrasando la inyección 1 frame y usando asignación de clips directos para prevenir crasheos del Grafo en el Animator.

### Lobby Animaciones (Silenciador de Consola)
- Se añadió script para limpiar el lag visual del Editor de Unity.

### Lobby Animaciones (Fix Congelamiento)
- Se reprogramó \RandomLobbySit.cs\ para forzar el rebobinado del tiempo del Animator interno (\_animator.Play\) al cambiar la animación dinámicamente, solucionando el problema de personajes que quedaban atascados en la pose final de clips cortos.

### Lobby Animaciones (Suavizado de Transicion)
- Se habilitó CrossFadeInFixedTime en RandomLobbySit.cs para fundir animaciones aleatorias sin cortes.

### Lobby Animaciones (Modo Espejo)
- Se añadió lógica a \RandomLobbySit.cs\ para activar aleatoriamente un parámetro Bool de 'Mirror' en el Animator, multiplicando visualmente la variedad de animaciones de sentado.

### Lobby Animaciones (Desincronización Realista)
- Se reestructuró la rutina en \RandomLobbySit.cs\ integrando tiempos de espera variables (\minIdleTime\, \maxIdleTime\) para que el personaje repose en la pose base entre animaciones, y se introdujo un \maxStartupDelay\ para desfasar el reloj interno entre los jugadores.

### Lobby Animaciones (Estilo Película / Stop-Motion)
- Se integró un limitador de fotogramas opcional (\useCinematicFramerate\) en \RandomLobbySit.cs\ para simular un estilo de interpolación a saltitos o película animada (ej. 12-24 fps) manipulando el tiempo natural del Animator.

### Lobby Animaciones (Fix Desplazamiento)
- Se desactivó forzosamente el \pplyRootMotion\ en \RandomLobbySit.cs\ para evitar que las animaciones con datos de movimiento muevan al personaje de su asiento.

### Lobby Animaciones (Mezcla de Cine) 
- Reestructuración total de \RandomLobbySit.cs\ para usar un sistema de **Blend Tree**. Ahora las animaciones no se interrumpen, sino que se funden suavemente mediante un parámetro de mezcla gestionado por código, eliminando el 100% de los saltos visuales.

### Lobby Animaciones (FIX CRITICO - Sin Override Controller)
- Reestructura total de \RandomLobbySit.cs\. Se eliminó el uso de \AnimatorOverrideController\ (causa del crash del Editor). Ahora el sistema solo usa parámetros nativos del Animator (\SitActionBlend\, \SitActionIndex\) controlados por código, compatible 100% con el Animator abierto.

### Editor - Herramienta de Reparacion
- Se creó \FixBrokenAnimatorTransitions.cs\ en \Assets/Editor/\ para detectar y eliminar transiciones rotas en el Animator Controller que causaban el error masivo \GenerateConnectionKey NullReferenceException\.

### Editor - Fix Animator Crash (v3)
- Se actualizó \FixBrokenAnimatorTransitions.cs\ para eliminar automáticamente estados con \motion = null\ (causa raíz del error \GenerateConnectionKey\). Afectados: \AC_ScrollUI\, \StarterAssetsThirdPerson\, \TestController\.

## 2026-04-14
### RandomLobbySit - Cola global de clips
- Añadida clase estática \LobbyClipQueue\ para evitar que dos personajes reproduzcan el mismo clip simultáneamente.
- Añadida variable \_lastClip\ por instancia para evitar repetición consecutiva del mismo clip.
### Editor - FixAnimationRootMotion
- Creado script para fijar Root Transform Y en FBXs de animaciones sentado (evita que personajes se levanten).
### AutoCloseAnimatorFix
- Desactivado: ya no cierra el Animator al darle Play.
