## [2026-06-27] - Personalización de Nicknames y Sincronización en Votaciones
- **PlayerState.cs**: Añadida variable de red sincronizada `playerName` para almacenar el nombre del jugador.
- **GameManager.cs**: Añadido diccionario de nicknames de clientes en el servidor, formateo de la lista del lobby con nombres reales y asignación a `PlayerState.playerName` en el spawn de gameplay.
- **LobbyUI.cs**: Añadido el campo de entrada `nicknameInputField`, guardado local en `PlayerPrefs`, codificación del nickname en el payload de conexión (`ConnectionData`) y decodificación en `ApprovalCheck`.
- **VotingUI.cs**: Actualizados los botones de votación de asamblea para mostrar el nombre personalizado del jugador expulsable.
- **AutoGenerateQuestHUD.cs**: Resuelto warning de API obsoleta reemplazando `FindObjectsSortMode` por el overload `FindObjectsInactive.Include`.

## [2026-06-27] - Parches de Cinemachine v3 y Correcciones en Inicialización de Herramientas
- **NetworkPlayerSetup.cs**: Actualizado al namespace `Unity.Cinemachine` manteniendo la búsqueda de tipo `CinemachineVirtualCamera` para localizar correctamente la cámara existente en la escena de juego en Unity 6.
- **SpectatorController.cs**: Actualizado al namespace `Unity.Cinemachine` manteniendo el tipo `CinemachineVirtualCamera` para el control de la cámara de espectador.
- **PlayerToolVisuals.cs**: Añadida protección (`LimpiarReferenciaExterna`) en el `Awake` para descartar referencias externas o de assets del proyecto y forzar la búsqueda local. Evita excepciones de "Transform resides in a Prefab asset" y "Destroying assets is not permitted" al spawnear el jugador.
- **LobbyUI.cs**: Configurada la validación de conexión (`ConnectionApproval` con `CreatePlayerObject = false`) en el Lobby para desactivar la creación automática del cuerpo físico de juego redundante (`PlayerArmature`), resolviendo de raíz el error de duplicados (2 PlayerArmature) y la pérdida de cámara/controles al entrar a la partida.

## [2026-06-26] - Corrección de Transición de Animación a Gameplay
- **PlayerLobbyPose.cs**: Forzado el peso de la capa de sentado (Lobby Layer) a 0f en `Start()` si el personaje es instanciado en una escena distinta al lobby (como Scene_Gameplay), asegurando que el personaje se ponga de pie.
- **PlayerState.cs**: Añadida limpieza adicional en `OnNetworkSpawn()` para forzar el peso de la capa 'Lobby Layer' a 0f al entrar a la escena `Scene_Gameplay`, garantizando que el personaje ejecute las animaciones de andar/correr en la red.
- **PlayerTransformation.cs**: Añadido método de utilidad `DesactivarCapaSentadoSiEsGameplay()` e invocado en todos los modos de alternancia visual (Modo 0, Modo 1 y Modo 2) al cambiar de modelo entre lobo y aldeano, para asegurar que el lobo no herede el peso de la capa de sentado en gameplay.

## [2026-06-25] - Corrección de Postura, Cámara del Lobby y Animaciones del Lobo
- **LobbyHeadLook.cs**: Agregada la variable `usarPoseInicialComoBase` en el Inspector. Si está en true, calcula y bloquea la rotación del hueso de la cabeza a partir de la pose original capturada en `Start()`, evitando que la animación del clip de sentado (idle bobbing) pise e interfiera con el movimiento del ratón y el seguimiento de mirada. Sincronizado con su versión del escritorio.
- **RandomLobbySit.cs**: Solucionado bug donde el personaje no se sentaba (se quedaba tieso/parado) si la lista de clips aleatorios del Inspector estaba vacía. Ahora permite inicializar la pose de sentado base en cualquier circunstancia. Removida la auto-carga dinámica de `PlayerLobbyPose` para evitar que afecte a los aldeanos estáticos de prueba.
- **PlayerLobbyPose.cs**: Mover chequeo de escena de Awake a Start para evitar bug de freeze de movimiento durante la transición.
- **LobbyHeadLook.cs (Anterior)**: Retirar el check limitante de fpCamera en Update para permitir rotación de cabeza en tercera persona.
- **RandomLobbySit.cs (Anterior)**: Re-aplicar parámetros de sentado tras reasignar el runtimeAnimatorController para mantener la postura de sentado.
- **PlayerLobbyPose.cs (Anterior)**: Retraso en sentado, verificación dinámica de escena y removido desbloqueo de cursor (corrige bloqueo de rotación de cabeza/cámara).
- **PlayerAnimationSync.cs**: Sincronización de animaciones de red del lobo.

## [2026-06-24] - Parche de Optimización y Arquitectura Post-Auditoría
- **NetworkPlayerSetup.cs**: Eliminada reflexión cámara.
- **SpawnManager.cs**: Parametrizado centro obelisco.
- **SpectatorController.cs**: Caché de jugadores.
- **ScrollController.cs**: Limpieza de comentarios.
- **PlayerTransformation.cs**: GameManager.Instance implementado.

## [2026-06-24] - Optimización de Scripts del Lobby, Transformaciones y Animaciones
- **PlayerTransformation.cs**: Corregido bug donde el rol de hombre lobo no se sincronizaba con `PlayerState.isWolf` del servidor. Añadido fallback en la búsqueda del Animator al cambiar de visual en jerarquía para que si el hijo no tiene Animator, intente usar el de la raíz del jugador. Implementado resolvedor dinámico recursivo de hijos en ejecución (`FindChildRecursively`) para evitar que el script manipule los assets del Prefab en vez de los clones instanciados en la escena.
- **ThirdPersonController.cs**: Eliminado polling en `Update` del Animator. Suscrito al evento `OnAnimatorChanged` de `PlayerTransformation` para re-enlazar el Animator dinámicamente, con fallback inteligente al de la raíz si no hay un hijo activo con controlador asignado.
- **GameManager.cs**: Implementada la ejecución de `ResetearMisionDiaria` al amanecer para permitir repetir las misiones en días nuevos.
- **RandomLobbySit.cs**: Optimización de cola y RNG.
- **PlayerLobbyPose.cs**: Eliminación de Update polling.

## [2026-06-23] - Corrección en Herramienta de Fuentes
- **ApplyFontsToGame.cs**: Búsqueda dinámica de fuentes.

## [2026-06-23] - Rediseño del HUD de Gameplay
- **StylePlayerHUD.cs (Editor)**: Creado script utilitario de editor para estilizar de forma homogénea todos los elementos del HUD en `Scene_Gameplay.unity`. Modifica el panel de inventario y monedas con fondo oscuro y borde dorado, ajusta los márgenes y dimensiones de la lista de tareas (`taskPanel`), agrega fondos tipo etiqueta mística oscura para el indicador de fase (Día/Noche) y el prompt de interacción, y posiciona todo de manera limpia y balanceada.

## [2026-06-23] - Rediseño Estético del Panel de Votación (Asamblea)
- **StyleVotingPanel.cs (Editor)**: Creado script utilitario para estilizar automáticamente el panel de votación en `Scene_Gameplay.unity`. Se diseñó una ventana de asamblea elegante (580x520px) centrada en pantalla con fondo oscuro (`#08080B`) y borde en rojo-naranja fuego para generar tensión. Se implementó una cabecera de asamblea con un título grande ("ASAMBLEA DE DESTIERRO" en color rojo intenso, negrita y espaciado de letras ampliado) y un subtítulo explicativo. Además, se estilizó el molde del botón (`BotonMolde`) para tener un fondo oscuro premium, bordes con outlines finos y textos de votación legibles y centrados.
- **UniversalQuestInteractable.cs**: Vinculada la lógica del interactuable con el HUD de la pantalla principal (`GameplayUI.MostrarBarraProgreso` y `GameplayUI.ActualizarBarraProgreso`). Ahora, la barra de progreso general estilizada neón de la pantalla se activará y rellenará en tiempo real cuando el jugador local esté talando, regando, picando o interactuando con una misión (tanto en mecánicas de mantener pulsado como de aporrear botones).
- **StyleProgressBar.cs (Editor)**: Creado script utilitario para estilizar automáticamente la barra de progreso en `Scene_Gameplay.unity`. Se redimensionó la barra a un formato más elegante y moderno (380x22px), se oscureció el fondo (`#0F0F14` con opacidad del 85%), se agregó un borde dorado difuso con brillo sutil y se cambió el color de relleno (Fill) a un cian místico neón (`#00E5FF`) con un glow interno. También se añadió una etiqueta de texto TextMeshPro superior ("PROGRESO DE ACCIÓN") perfectamente centrada y estilizada.

## [2026-06-23] - Rediseño Premium de la Tienda de Objetos (ShopPanelController)
- **ShopPanelController.cs**: Completada la refactorización integral del controlador UI. Se agregaron pestañas interactivas para "Pergaminos" y "Consumibles", se implementó un HUD superior que muestra las monedas del jugador local en tiempo real reaccionando al evento `OnValueChanged` de `NetworkVariable` (eliminando el polling en `Update` y previniendo memory leaks), y se pulió la estética a un estilo premium modo oscuro con bordes y tipografías cuidadas.

## [2026-06-23] - Herramienta de Testing de Misiones (GameManager)
- **GameManager.cs**: Añadida sección de "DEBUG - Testing de Misiones" en el Inspector. Ahora el Host puede forzar la asignación de cualquier misión (`questID`) a un jugador específico (mediante su ClientID) o a todos los jugadores de la sala simultáneamente usando botones de Odin Inspector o el Context Menu.

## [2026-06-23] - Hotfix Visual en Misiones de Transporte (Llevar)
- **UniversalQuestInteractable.cs**: Corregido bug donde el script reactivaba accidentalmente los `MeshRenderer` de los triggers invisibles (el "cuadrado") al actualizar el estado de las misiones tipo `Transportar`. Ahora el script cachea en `Awake` qué renderers y colliders estaban realmente activos en el Inspector de Unity, y solo alterna esos objetos, respetando los cubos de trigger transparentes diseñados por el usuario.

## [2026-06-22] - Validación Estricta de Misiones (Anti-Cheat & Bug Fix)
- **UniversalQuestInteractable.cs**: Añadida validación local en `OnTriggerEnter` para que si el interactuable requiere una `QuestData` específica, el jugador no pueda interactuar ni ver el HUD si no la tiene activa.
- **PlayerInventory.cs**: Añadida validación de red en `ProcessUniversalInteractionServerRpc` para que el servidor rechace intentos de cobrar oro o marcar progreso en interactuables exclusivos de otras misiones, corrigiendo el bug de cobrar oro de misiones ajenas.

## [2026-06-22] - Corrección de Progreso en Misiones de Múltiples Pasos
- **PlayerQuestTracker.cs**: Modificado `ProcessStepServerRpc` para dar soporte a `MaterialType.Cualquiera` en la coincidencia flexible de pasos. Corregido error de compilación `CS0103` declarando la variable local `currentStep` faltante.
- **GameManager.cs**: Refactorización avanzada del gestor. Implementada caché de objetos lazy con borrado en cambios de escena. Evitado `NullReferenceException` al desconectarse clientes. Solucionado bug de físicas al cambiar `tpc.enabled = false` por `CanMove`. Asegurado `KickPlayerServerRpc`. Optimización O(N+M) en `EvaluarAmanecer` mediante diccionario. Optimización de consultas en `AsignarMisionesBasicasADiaNuevo`. Uso de `StringBuilder`. Corregida advertencia `CS0618` removiendo el uso del parámetro obsoleto `FindObjectsSortMode` en `FindObjectsByType`.
- **UniversalQuestInteractable.cs**: Añadido fallback dinámico de herramienta que consulta el tracker de misiones activas del jugador si el interactuable no tiene herramienta asignada localmente ni por questData. Se habilitó que las interacciones de tipo `Transportar` reproduzcan la animación asignada al iniciar. Implementada la caché de `PlayerToolVisuals` (`cachedToolVisuals`) en `OnTriggerEnter`/`OnTriggerExit`. Refactorizada la función `IniciarInteraccion` para no deshabilitar el ActionMap del jugador local (bloqueando únicamente el movimiento físico mediante `CanMove = false`), permitiendo así que el aporreo de botones (`AporrearBoton`) reciba de forma correcta las pulsaciones. Se actualizó `AbortarInteraccion` y `ResetVisualsLocales` consecuentemente. Se reemplazaron llamadas redundantes de `GetComponentInChildren<PlayerToolVisuals>` en `Update()` (mecánica de `PuntoEntrega`) y en `CompletarInteraccionLocal()` para utilizar la referencia cacheada `cachedToolVisuals`.
- **InteractivableMisionUniversal.cs**: Implementada caché de componentes del jugador local (`ThirdPersonController`, `PlayerInput`) al entrar/salir de la zona y agregada validación de estado del ActionMap antes de permitir el inicio de interacciones. Corregida advertencia `CS0618` removiendo el uso del parámetro obsoleto `FindObjectsSortMode` en `FindObjectsByType`.
- **PlayerToolVisuals.cs**: Modificada la función `SetTool` para que solo asigne el estado si realmente cambia, previniendo el reinicio de las partículas de las herramientas de forma duplicada si ya se encontraban activas.
- **PlayerInventory.cs**: Añadido reenvío automático a `PlayerQuestTracker.ProcessStepServerRpc` cuando el jugador completa la interacción con un interactuable universal. Esto hace que las misiones avancen correctamente de paso en paso en el servidor y actualicen el pergamino/HUD del cliente.
- **GameplayUI.cs**: Reemplazado el carácter unicode `☐` por los brackets estándar `[ ]` en la visualización de la lista de tareas para eliminar la advertencia de falta de carácter en el asset de fuente SDF de TextMeshPro (`gothic_ultra_tt SDF`).
- **traer.asset**: Reconfigurada la misión de "traer" agua a un formato secuencial de 2 pasos en el ScriptableObject: el Paso 1 consiste en recoger el agua en la Aldea (el pozo) y el Paso 2 en llevar el balde de agua al Gran Árbol.
- **llevar.asset**: Desactivada como misión básica (`esMisionBasica: 0`) para evitar asignaciones duplicadas e incorrectas de un solo paso.
- **Pocion.asset**: Reconfigurado el paso 2 de la misión de la poción. En lugar del paso muerto "Habla con un aldeano", ahora requiere recoger un `BaldeAgua` en la `Aldea` (en el pozo de agua), lo cual es 100% funcional.
- **Manzana.asset**: Corregido el texto placeholder de descripción de misión y reconfigurados los pasos asignándoles sus zonas correctas (`CasaBruja` para conseguir la manzana y `Altar` para bendecirla con Esencia Antigua) y descripciones detalladas en español.
- **Daga.asset**: Corregido el texto placeholder de la historia y añadidas descripciones en español a los 4 pasos secuenciales para evitar que aparezcan vacíos en la UI.
- **Scene_Gameplay.unity**: Actualizados 22 componentes `UniversalQuestInteractable` en la escena cambiando `materialAsignado` de `Ninguno` (0) a `Cualquiera` (18). Esto permite que los interactuables genéricos progresen tanto las misiones básicas como las legendarias de forma flexible según el paso activo.


## [2026-06-21] - Detección de Misiones por Área (Triggers de Cubo y Estructura Padre-Hijo)
- **UniversalQuestInteractable.cs**: Añadido fallback en `IniciarInteraccion` para leer el nombre de la herramienta desde `questData.toolVisualName` si el campo `nombreVisualManual` del Inspector está vacío, evitando que el parámetro `Tool` viaje vacío a la red y permitiendo iniciar las misiones de transportar/picar correctamente.
- **PlayerToolVisuals.cs**: Modificado `FindChildByName` para utilizar búsquedas parciales (`Contains`), solucionando el error por el cual no se encontraba el pico de minería debido a espacios finales en el nombre del objeto (`"Pickaxe "`) en la jerarquía del prefab.
- **QuestTriggerProxy.cs**: Nuevo script proxy para reenviar colisiones de triggers desde un objeto padre a los interactuables de misión en sus hijos.
- **QuestZonePoint.cs**: Se conserva la forma y escala de la caja (`BoxCollider`) original convirtiéndola en trigger sin destruir el colisionador.
- **UniversalQuestInteractable.cs** / **InteractivableMisionUniversal.cs**: Añadido soporte automático en `Awake` para detectar si el script está en un objeto hijo y el colisionador en el padre, en cuyo caso instala el proxy de forma transparente.

## [2026-06-21] - Corrección de Entrega en Misiones de Transportar
- **UniversalQuestInteractable.cs**: Corregido el trigger en OnTriggerEnter para permitir el acceso a los Puntos de Entrega (incluso si no tienen questData o materialAsignado). Ahora comprueba de manera dinámica la herramienta requerida configurada en el inspector en lugar de forzar 'caja'.
- **PlayerToolVisuals.cs**: Ampliado 'IsToolActive' para soportar todas las herramientas del juego (como 'regadera' / agua) de manera que se puedan entregar correctamente.

## [2026-06-21] - Cancelación de Misión por Límite de Zona
- **InteractivableMisionUniversal.cs**: Implementada cancelación de misión y descongelamiento del jugador en OnTriggerExit para evitar quedarse congelado al salir de la zona.

## [2026-06-21] - Añadida Zona Aldea
- **GameEnums.cs**: Añadido 'Aldea' al enum 'ZoneID' para dar soporte a misiones ubicadas en esta zona.

## [2026-06-21] - Sistema de Entrega de Materiales del Inventario
- **UniversalQuestInteractable.cs**: Añadida la opción para configurar entregas de materiales de la mochila sin necesidad de una caja física. El script valida que el jugador tenga el material y la cantidad requerida en su inventario antes de iniciar.
- **PlayerInventory.cs**: En el servidor, se deduce automáticamente la cantidad requerida del material del inventario del jugador tras una entrega exitosa.
- **UniversalQuestInteractableEditor.cs**: Expuestos los nuevos campos 'materialRequeridoParaEntrega' y 'cantidadRequeridaParaEntrega' en el Inspector bajo el tipo de mecánica 'PuntoEntrega'.

## [2026-06-19] - Auto-Limpieza de Misiones Completadas
- **PlayerInventory.cs**: Al completar la fase final de una misión mediante 'UniversalQuestInteractable', el servidor comprueba si el jugador tenía esta misión rastreada en su 'PlayerQuestTracker' y, de ser así, limpia su rastreador automáticamente (currentQuestID = "").

## [2026-06-19] - Misiones de Múltiples Etapas (Multi-Step Quests)
- **UniversalQuestInteractable.cs**: Reemplazado 'completadaHoy' por 'progresoActual'. Ahora el Huerto soporta la variable 'interaccionesRequeridas'. Si se pone en 3, la barra azul se llenará a tercios (1/3, 2/3, 3/3) con cada interacción.
- **PlayerInventory.cs**: Actualizada la lógica para dar el oro y los materiales única y exclusivamente cuando el 'progresoActual' del interactuable llega al máximo.
- **UniversalQuestInteractableEditor.cs**: Expuesto el campo 'interaccionesRequeridas' en el Inspector.

## [2026-06-19] - Misiones Diarias y Barra de Progreso
- **UniversalQuestInteractable.cs**: Refactorizado a NetworkBehaviour. Añadida UI de Barra de Progreso local. Lógica de cancelación por movimiento ('WASD' aborta la misión). Añadido estado 'completadaHoy' sincronizado en red para evitar farming infinito el mismo día.
- **PlayerInventory.cs**: Añadida lógica 'CancelUniversalInteractionServerRpc' y vinculación por NetworkObjectId para validar finalizaciones de misiones en el Servidor.
- **UniversalQuestInteractableEditor.cs**: Expuesto el campo 'barraProgresoUI' en el Inspector.

## [2026-06-19] - Escudo de Interacción Vertical (Anti-Gravedad)
- **UniversalQuestInteractable.cs**: Añadido un bloqueo de protección en el método \OnTriggerExit\. Si una fuerza física (como la zona de ingravidez) eleva al jugador y lo saca del volumen del Trigger durante los 3 segundos de animación, el HUD y el sistema de interacción no se destruyen prematuramente.

## [2026-06-19] - Desacoplamiento Visual Manual y Bloqueo de Red
- **UniversalQuestInteractable.cs**: Añadida la variable 
ombreVisualManual al Inspector para desacoplar el visual de la mano de los datos del inventario. El sistema ahora solicita directamente esta variable al iniciar la interacción.
- **PlayerToolVisuals.cs**: Blindado el apagado de la regadera. Si el sistema de inventario intenta enviar un apagado (ctive = false) debido a la obtención del 'BaldeAgua', este se ignora por completo para proteger la animación ininterrumpida de 3 segundos.

## [2026-06-19] - Fallback de Seguridad Final (Material Asignado)
- **UniversalQuestInteractable.cs**: Añadido un segundo nivel de fallback extremo para \herramientaAPedir\. Si el Asset está vacío y el string legacy está vacío, el script usa el Enum \materialAsignado.ToString()\ como último recurso para invocar la herramienta visual.
- **PlayerToolVisuals.cs**: Añadido el caso \aldeagua\ al switch para que reaccione correctamente al Enum del inventario y asigne la regadera.

## [2026-06-19] - Optimización de Rendimiento: Remoción de SendMessage
- **PlayerToolVisuals.cs**: Refactorización del método \SetToolByName\. Se eliminó el uso de \SendMessage\ basado en reflexión (altamente ineficiente y propenso a fallos silenciosos) por un bloque \switch\ robusto y directo. Esto elimina la recolección de basura (Garbage Collection) y añade advertencias explícitas en consola si el string solicitado no coincide con ninguna herramienta registrada.

## [2026-06-19] - Netcode Refactor (Event-Driven Architecture)
- **GameManager.cs**: Eliminado el método \Update()\ por completo. Refactorizada la máquina de estados y el reloj del servidor hacia una arquitectura basada en \Coroutine\ y la suscripción a \NetworkVariable.OnValueChanged\ dentro de \OnNetworkSpawn\ y \OnNetworkDespawn\. Esto evita sobrecarga de fotogramas, desvincular eventos al destruir la escena, y aplica estrictamente autoridad de servidor para misiones y temporizadores.

## [2026-06-19] - Mission Debugger (Logs de Flujo y Trazabilidad)
- **UniversalQuestInteractable.cs**: Inyección de Logs Informativos para rastrear la entrada al trigger, el inicio de la interacción con datos (Anim, Tool, Duración) y validación estricta de la búsqueda del Animator.
- **PlayerToolVisuals.cs**: Inyección de Logs para rastrear qué herramienta específica se intenta activar y la validación de sus partículas hijas (Ej: PlayerWateringParticles) para evitar fallos silenciosos.

## [2026-06-19] - Reparación de Bug de Regresión (UniversalQuestInteractable)
- **UniversalQuestInteractable.cs**: Corregido bug crítico donde la animación de interacción y la regadera no se disparaban. El Animator se estaba buscando con GetComponent() en la raíz del jugador, devolviendo null porque vive en el hijo 'PlayerArmature'. Se cambió a GetComponentInChildren<Animator>() garantizando la ejecución del flujo visual.

## [2026-06-19] - HUD Flotante 3D Contextual (Billboard)
- **UniversalQuestInteractable.cs**: Añadida generación dinámica de HUD 3D (TextMeshPro) en Awake para mostrar textos in-world sobre los interactuables (cero dependencia de prefabs).
- **UX**: Añadido efecto Billboard local para que el texto mire a la cámara del jugador, y validación estricta de red para asegurar que solo el dueño (jugador local) enciende y ve este HUD.

## [2026-06-19] - Separación Lógica Acción vs Recolección
- **UniversalQuestInteractable.cs**: Añadido enum InteractionMode para prohibir que las misiones de Acción (Regar) entreguen objetos en el inventario. Implementada lógica estricta Server-Side.
- **UniversalQuestInteractableEditor.cs**: Añadido script de Editor personalizado para ocultar campos no relevantes según el modo de interacción seleccionado, previniendo errores de diseño de niveles.

## [2026-06-19] - Refactor Data-Driven Universal Quest System
- **QuestData.cs**: Añadida configuración modular (animationInteractionType, toolVisualName, interactionDuration).
- **UniversalQuestInteractable.cs**: Nuevo script genérico que lee de QuestData para ejecutar predicción de cliente dinámica.
- **PlayerInventory.cs**: Remplazado el sistema hardcodeado de riego por ProcessUniversalInteractionServerRpc.
- **PlayerToolVisuals.cs**: Implementado SetToolByName para activación dinámica.
- **Eliminados**: WitchGardenInteractable.cs y BasicQuestInteractable.cs.

## [2026-06-19] - Reubicación de Visuales de Herramientas (Anti-Congelamiento)
- **PlayerWateringParticles.cs**: Refactorizado para anclarse directamente en la regadera sin buscarla por la jerarquía. Instancia las partículas usando su propio transform.position.
- **PlayerInventory.cs & WitchGardenInteractable.cs**: Actualizadas las referencias a 'GetComponentInChildren(true)' para encontrar los scripts visuales anclados en los huesos de la mano, evitando tocar la raíz del jugador y protegiendo el Animator.

## [2026-06-19] - Sistema de Riego en Huerto de la Bruja
- **WitchGardenInteractable.cs**: Nuevo script de interacción para el huerto con congelación de movimiento instantánea (Client-Side Prediction) para evitar lag visual.
- **PlayerInventory.cs**: Implementados RPCs de validación de riego autoritativo, recompensa de oro asíncrona (15g), y sincronización cruzada de partículas hacia los demás clientes.
- **PlayerWateringParticles.cs**: Añadido método de apagado manual (StopWaterParticles).

## [2026-06-19] - Refactor Seguridad y Estabilidad Core
- **PlayerInventory.cs / BasicQuestInteractable.cs**: Añadida validación de distancia (Vector3.Distance) estricta en el ServerRpc de recompensa material para evitar exploits de farmeo a distancia.
- **PlayerQuestTracker.cs**: Ajustado rango de anti-cheat a 4.5 metros.
- **NetworkPlayerSetup.cs**: Implementado auto-cargador de Input Actions (Anti-Amnesia) mediante Resources.Load, previniendo el fallo silencioso del jugador congelado.

## [2026-06-19] - Fix Definitivo Input Pergamino Desacoplado
- **ScrollController.cs**: Eliminada la lectura invasiva de hardware (Keyboard.current.qKey) en favor del sistema de Eventos oficial de Unity (StarterAssetsInputs.scroll). Ahora el pergamino pausa el movimiento limpiamente mediante CanMove, cumpliendo la arquitectura estricta y evitando bugs de hardware.

# ÃÆÃÂ°Ãâ¦ÃÂ¸ÃÂ¢Ã¢âÂ¬ÃâÃâ¦Ã¢â¬Å HISTORIAL DE CAMBIOS (CHANGELOG)

Este documento registra todas las intervenciones tÃÆÃâÃâÃÂ©cnicas, parches y cÃÆÃâÃâÃÂ³digo generado o refactorizado por la IA. 
Su objetivo es garantizar que el usuario sepa **exactamente quÃÆÃâÃâÃÂ© archivos fueron alterados y por quÃÆÃâÃâÃÂ©**, evitando modificaciones "fantasma".

---
## [2026-06-19] - Fix Definitivo de Movimiento y UI de Pergamino
- **NetworkPlayerSetup.cs**:
    - Se eliminÃ³ la llamada a `playerInput.ActivateInput();` durante el spawn. Esto forzaba un reinicio del Action Map del New Input System que provocaba la pÃ©rdida total de controles (WASD/botones) al entrar a la partida.
- **ThirdPersonController.cs**:
    - Se eliminÃ³ el `return` prematuro en el Lock Guard para permitir que las funciones de gravedad (`JumpAndGravity`) y movimiento (`Move()`) sigan ejecutÃ¡ndose con inputs en cero. Esto evita que el personaje se quede levitando o bloqueado permanentemente tras leer.
    - AÃ±adidos logs de diagnÃ³stico avanzado para rastrear estados de bloqueo.
- **StarterAssetsInputs.cs**:
    - AÃ±adidos logs de depuraciÃ³n para rastrear la llegada fÃ­sica de los inputs WASD desde el hardware.
- **ScrollController.cs**:
    - Se modificÃ³ `ToggleReading` para que SIEMPRE permita cerrar el pergamino, incluso si el estado local o los controles fueron bloqueados externamente.
    - Se aÃ±adiÃ³ comprobaciÃ³n `Application.isFocused` para evitar que la pulsaciÃ³n de la tecla 'Q' se dispare cuando la ventana no estÃ¡ activa.

## [2026-06-19] - CorrecciÃ³n Visual de Regadera y PartÃ­culas de Agua
- **QuestZonePoint.cs**:
    - Corregido el mapeo de IDs de animaciÃ³n para Regar, Talar, Limpiar, Beber, ApuÃ±alar y Velas.
- **QuestInteractable.cs**:
    - AÃ±adida activaciÃ³n de herramientas visuales (regadera, escoba, pociÃ³n, daga) en red durante la interacciÃ³n.
- **PlayerWateringParticles.cs**:
    - Cambiada la bÃºsqueda de Shader a `Universal Render Pipeline/Particles/Unlit` para soporte completo de URP.
- **CheckQuestInteractables.cs**:
    - AÃ±adida importaciÃ³n de namespace que faltaba para solucionar error de compilaciÃ³n del Editor.

## [2026-04-12] - InmersiÃÆÃÂ³n en Lobby y CÃÆÃÂ¡mara Libre
- **LobbyHeadLook.cs**: 
    - ActivaciÃÆÃÂ³n de CÃÆÃÂ¡mara Libre (Free Look) sin necesidad de clics.
    - Bloqueo automÃÆÃÂ¡tico del cursor al aparecer en el lobby.
- **LobbyRaycastInteraction.cs**: 
    - [MEJORA] BÃÆÃÂºsqueda robusta del HUD: ahora encuentra el `LobbyInteractionHUD` incluso si estÃÆÃÂ¡ desactivado en la jerarquÃÆÃÂ­a.
    - [MEJORA] Auto-configuraciÃÆÃÂ³n de Capas: Si no se configuran en el Inspector, el script usa automÃÆÃÂ¡ticamente Default y Player (Capa 8).
    - [MEJORA] DetecciÃÆÃÂ³n de Hoguera: Mejorada la detecciÃÆÃÂ³n para captar clics en las rocas o hijos de la hoguera.
    - Sistema de interacciÃÆÃÂ³n inmersivo basado en Raycast (Mirada). Detecta jugadores y hoguera para lanzar acciones.
- **LobbyInteractionUI.cs** (NUEVO): HUD contextual con barra circular de progreso (Radial Fill) para "Mantener X/E".
- **GameManager.cs**: ImplementaciÃÆÃÂ³n de `KickPlayerServerRpc` para permitir al Host expulsar jugadores fÃÆÃÂ­sicamente desde el lobby.

## [2026-04-12] - Sistema de Lobbies y Navegador de Servidores
- **LobbyUI.cs**: 
    - Integrado `Unity.Services.Lobbies`.
    - Implementada creaciÃÆÃÂ³n de partidas PÃÆÃÂºblicas y Privadas (vÃÆÃÂ­a Toggle).
    - AÃÆÃÂ±adido `JoinPanel` para buscar y listar servidores activos (`RefreshServerList`).
    - AÃÆÃÂ±adido sistema de Heartbeat (`SendHeartbeatPingAsync`) en `Update` para mantener vivo el servidor en la nube.
- **PlayerLobbyPose.cs**: 
    - Corregido el problema de movimiento en el Lobby; ahora desactiva el `CharacterController` y `ThirdPersonController` de raÃÆÃÂ­z.
    - Se asegura de forzar el parÃÆÃÂ¡metro `isSitting` en el Animator.
    - AÃÆÃÂ±adido control de escena para evitar que el jugador se quede sentado en la partida real.
- **LobbyHeadLook.cs**: 
    - Implementada lÃÆÃÂ³gica de "Prioridad de CÃÆÃÂ¡mara": apaga la cÃÆÃÂ¡mara principal de la escena al entrar al asiento para usar la vista FPS.
    - Limpieza automÃÆÃÂ¡tica: reactiva la cÃÆÃÂ¡mara principal al salir o desconectarse.
- **NetworkPlayerSetup.cs**: Corregido aviso de Cinemachine inexistente al iniciar el personaje en la escena de Lobby.
    - Corregidos warnings de mÃÆÃÂ©todos obsoletos usando la nueva sintaxis `[Rpc(SendTo.Server, ...)]` de Unity 6.
    - Eliminada variable privada sin uso `_personajeLocalInstanciado`.


## [2026-04-11]
- **LobbyPlayerSpawner**: Convertido a NetworkBehaviour. Sincroniza asientos por ClientId (Host=0).
- **LobbyHeadLook.cs** (NUEVO): SincronizaciÃÆÃÂ³n de mirada en red (NetworkVariable) para lobby 1ÃâÃÂª persona.
- **PlayerArmature**: Integrada cÃÆÃÂ¡mara FP y script de mirada inteligente en el Prefab.
- **LobbyUI.cs**: Eliminadas llamadas locales redundantes de spawn para favorecer Netcode.



## [2026-04-05] - Misiones HÃÆÃâÃâÃÂ­bridas y Roles de Oficio
### ÃÆÃÂ¢Ãâ¦ÃÂ¡ÃÂ¢Ã¢â¬Å¾ÃÂ¢ÃÆÃÂ¯ÃâÃÂ¸ÃâÃ¯Â¿Â½ Arquitectura y Gameplay
- **Enums/GameEnums.cs**: AÃÆÃâÃâÃÂ±adido `RolAldea` y tipos de misiones avanzadas (Wires, Candle Puzzle, Crafting).
- **PlayerState.cs**: Herencia de oficios (drop de items al morir) y sincronizaciÃÆÃâÃâÃÂ³n de red para roles.
- **GameManager.cs**: AsignaciÃÆÃâÃâÃÂ³n aleatoria de roles de aldea (Herrero) al inicio de la fase.
- **JobItem.cs** *(NUEVO)*: Sistema de recolecciÃÆÃâÃâÃÂ³n de herramientas caÃÆÃâÃâÃÂ­das para heredar oficios.
- **QuestInteractable.cs**: Refactoring para soporte completo de red y Despawn autoritativo.
- **SpawnAreaManager.cs** *(NUEVO)*: Generador de objetos por ÃÆÃâÃâÃÂ¡rea para misiones de recolecciÃÆÃâÃâÃÂ³n.
- **PuzzleSequenceManager.cs** & **PuzzleCandle.cs** *(NUEVOS)*: Sistema Simon-Says para puzzles fÃÆÃâÃâÃÂ­sicos.
- **ForgeController.cs** & **CraftingStation.cs** *(NUEVOS)*: MecÃÆÃâÃâÃÂ¡nica social de la Forja (humo interactivo) y crafteo de la Daga Legendaria.
- **MinigameTrigger.cs** *(NUEVO)*: Disparador modular para abrir interfaces de minijuegos (preparado para integraciÃÆÃâÃâÃÂ³n externa).
- **Renombramiento**: Carpeta de tareas web renombrada a `TaskTapestryHtml`. Solo queda el minijuego de "Cuerdas" activo.
- **RemociÃÆÃâÃâÃÂ³n**: Eliminado prototipo de minijuego de cables anterior por solicitud del usuario para integraciÃÆÃâÃâÃÂ³n propia (Google Studio).

---
## [2026-04-11]
- **Asientos_Lobby**: ReorganizaciÃÆÃÂ³n total de 10 asientos y 5 troncos (Orden 1-5 Horario).
- **LobbyPlayerSpawner**: Sincronizada la lista de spawn con el nuevo orden de asientos.
- **LobbyCameraManager.cs**: RefactorizaciÃÆÃÂ³n completa (CÃÆÃÂ¡mara suave sin Cinemachine).
- **LobbyUI.cs**: IntegraciÃÆÃÂ³n de personaje local en lobby dinÃÆÃÂ¡mico.


## [2026-04-05] - Refactor: DestrucciÃÆÃâÃâÃÂ³n del God Object
### ÃÆÃÂ°Ãâ¦ÃÂ¸ÃÂ¢Ã¢âÂ¬ÃÂºÃâÃÂ ÃÆÃÂ¯ÃâÃÂ¸ÃâÃ¯Â¿Â½ RefactorizaciÃÆÃâÃâÃÂ³n Estructural
- **PlayerState.cs**: Troceado magistralmente de 1000 lÃÆÃâÃâÃÂ­neas a ~200 lÃÆÃâÃâÃÂ­neas. Reducido estrictamente a manejar identidad base (`isWolf`, `isDead`) y el ragdoll.
- **PlayerInventory.cs** *(NUEVO)*: Centraliza la lÃÆÃâÃâÃÂ³gica de economÃÆÃâÃâÃÂ­a (`monedas`) y el ÃÆÃâÃâÃÂ­tem equipado en la mano, con sus mallas visuales.
- **PlayerStatusEffects.cs** *(NUEVO)*: Concentra los Buffs y Debuffs (Veneno, Silenciadores, Stun), descongestionando el sistema de vida.
- **PlayerItemController.cs** *(NUEVO)*: Abstrae la kilomÃÆÃâÃâÃÂ©trica lÃÆÃâÃâÃÂ³gica del "Switch" usado al pulsar [F]. Cada bomba apestosa y manzana recae en su propio cerebro.
- **GameEnums.cs** *(NUEVO)*: Movido globalmente `TipoObjeto` para evitar referencias circulares restrictivas en el proyecto.
- **FixPlayerInventoryPrefab.cs** *(NUEVO)*: Script de Editor (Tools) creado para automatizar la asignaciÃÆÃâÃâÃÂ³n exhaustiva de los objetos 3D (Daga, Antorcha, PociÃÆÃâÃâÃÂ³n) al componente PlayerInventory dentro de `PlayerArmature` sin intervenciÃÆÃâÃâÃÂ³n manual.

---
## [2026-04-11]
- **Asientos_Lobby**: ReorganizaciÃÆÃÂ³n total de 10 asientos y 5 troncos (Orden 1-5 Horario).
- **LobbyPlayerSpawner**: Sincronizada la lista de spawn con el nuevo orden de asientos.
- **LobbyCameraManager.cs**: RefactorizaciÃÆÃÂ³n completa (CÃÆÃÂ¡mara suave sin Cinemachine).
- **LobbyUI.cs**: IntegraciÃÆÃÂ³n de personaje local en lobby dinÃÆÃÂ¡mico.


## [2026-04-05] - EstabilizaciÃÆÃâÃâÃÂ³n de Controles e IngenierÃÆÃâÃâÃÂ­a de Red
### ÃÆÃÂ°Ãâ¦ÃÂ¸ÃâÃ¯Â¿Â½ÃÂ¢Ã¢âÂ¬ÃÂº Bug Fixes (Correcciones)
- **Player Input**: Corregida la amnesia del `PlayerArmature`. Reasignado el archivo `StarterAssets.inputactions` al `PlayerInput` del Prefab para restaurar el movimiento (WASD).
- **Auto-Healing de CÃÆÃâÃâÃÂ¡mara**: Editado `NetworkPlayerSetup.cs`. Al cargar la escena de juego, si Unity 6 desvincula a Cinemachine, el script invoca e inyecta dinÃÆÃâÃâÃÂ¡micamente un `CinemachineBrain` a la MainCamera, vinculando al clon con la `Virtual Camera`.
- **Pergamino (Scroll)**: Editado `ScrollController.cs`. AÃÆÃâÃâÃÂ±adida compatibilidad con red local y recuperaciÃÆÃâÃâÃÂ³n de control UI para testear sin ser "DueÃÆÃâÃâÃÂ±o".

### ÃÆÃÂ°Ãâ¦ÃÂ¸ÃÂ¢Ã¢âÂ¬Ã¯Â¿Â½Ãâ¦ÃÂ½ AuditorÃÆÃâÃâÃÂ­a
- Generado el reporte `audit_report_v0.1.md`.
- Analizado el rendimiento general: Sistema "Sano" (sin bÃÆÃâÃâÃÂºsquedas perjudiciales en los Updates de forma recurrente).

### ÃÆÃÂ°Ãâ¦ÃÂ¸Ãâ¦ÃÂ¡ÃâÃÂ© Deuda TÃÆÃâÃâÃÂ©cnica Detectada
- `PlayerState.cs` necesita dividirse urgente (Modo God-Object detectado con mÃÆÃâÃâÃÂ¡s de 1000 lÃÆÃâÃâÃÂ­neas).
- `PlayerArmature.prefab` contiene 177 hijos; urge vaciarlo instanciando objetos en tiempo de ejecuciÃÆÃâÃâÃÂ³n.

---
## [2026-04-11]
- **Asientos_Lobby**: ReorganizaciÃÆÃÂ³n total de 10 asientos y 5 troncos (Orden 1-5 Horario).
- **LobbyPlayerSpawner**: Sincronizada la lista de spawn con el nuevo orden de asientos.
- **LobbyCameraManager.cs**: RefactorizaciÃÆÃÂ³n completa (CÃÆÃÂ¡mara suave sin Cinemachine).
- **LobbyUI.cs**: IntegraciÃÆÃÂ³n de personaje local en lobby dinÃÆÃÂ¡mico.


## [Versiones Previas] - FundaciÃÆÃâÃâÃÂ³n V0.1
*Historial condensado de creaciÃÆÃâÃâÃÂ³n:*
- Setup completo de `NetworkManager` con Relay y Lobby UI.
- SincronizaciÃÆÃâÃâÃÂ³n del Controlador de Tercera Persona y BlendTrees de animaciÃÆÃâÃâÃÂ³n (`PlayerAnimationSync`).
- Autoridad estricta de Netcode sobre `IsLobo` y variables vitales (`PlayerState`).
- MecÃÆÃâÃâÃÂ¡nica asimÃÆÃâÃâÃÂ©trica de Raycast para cazar (Muerte instantÃÆÃâÃâÃÂ¡nea + `IsDead`).
- Sistema Canvas de Asambleas dinÃÆÃâÃâÃÂ¡mico basado en Vivos vs Muertos.

---
## [2026-04-11]
- **Asientos_Lobby**: ReorganizaciÃÆÃÂ³n total de 10 asientos y 5 troncos (Orden 1-5 Horario).
- **LobbyPlayerSpawner**: Sincronizada la lista de spawn con el nuevo orden de asientos.
- **LobbyCameraManager.cs**: RefactorizaciÃÆÃÂ³n completa (CÃÆÃÂ¡mara suave sin Cinemachine).
- **LobbyUI.cs**: IntegraciÃÆÃÂ³n de personaje local en lobby dinÃÆÃÂ¡mico.


## [2026-04-05] - Integracion Tapiz Modular (Tapestry)
### ?? Minijuegos e Interfaz Nativa
- **TaskPoint.cs**: AÃÆÃÂ±adida estructura TapestryConnectionData y lista de estado para persistencia de hilos entre sesiones.
- **TapestryMinigame.cs** *(NUEVO)*: Implementacion nativa en C# (UI Toolkit) que replica la logica de arrastre de hilos, validacion de colores y audio procedural (White Noise + Tones).
- **TapestryMinigame.uxml / .uss** *(NUEVOS)*: DiseÃÆÃÂ±o visual tipo pergamino medieval con columnas dinamicas.
- **TapestryTask_UI.prefab** *(NUEVO)*: Prefab de interfaz configurado con UIDocument y script de persistencia.
- **TapestryTask_Zone.prefab** *(NUEVO)*: Prefab de mundo modular con Trigger para activar la tarea de reparacion de tapiz.

- Assets/Models/SM_Prop_Tapestry_Broken.fbx: Creado modelo fracturado en Blender con puntas triangulares.
- Assets/Prefabs/Tasks/TapestryTask_Zone 1.prefab: Version final del disparador con el modelo 3D integrado.

## [2026-04-05] - UniÃÆÃâÃâÃÂ³n MÃÆÃâÃâÃÂ¡gica Tapestry
- Assets/Scripts/UI/Minigames/TapestryRestorer.cs: Nuevo script de restauraciÃÆÃâÃâÃÂ³n 3D.
- Assets/Models/SM_Prop_Tapestry_Magic.fbx: Modelo con Blend Shapes.
- Assets/Prefabs/Tasks/TapestryTask_Zone_Magic.prefab: Prefab final con soporte mÃÆÃâÃâÃÂ¡gico.
- Assets/Scripts/UI/Minigames/TapestryMinigame.cs: IntegraciÃÆÃâÃâÃÂ³n de eventos OnMinigameWon.

- Assets/Scripts/Core/Network/HoldToStartAction.cs : Eliminado script de acciÃÆÃÂ³n de mantener tecla E para el lobby.
- Assets/Scripts/UI/Menu/PlayerLobbyPose.cs : Creado script para pose estÃÆÃÂ¡tica de aldeano en el lobby.

## [2026-04-11] - Lobby In-Game: Camera Manager
### Cambios
- **LobbyCameraManager.cs**: Reescrito sin Cinemachine. Usa dos Transform ancla (Ancla_CamaraMenu, Ancla_CamaraLobby) con Lerp suave (SmoothStep) entre posiciones de la Main Camera.
- **Scene_Menu.unity**: Creado GameObject 'LobbyCameraManager' con hijos 'Ancla_CamaraMenu' y 'Ancla_CamaraLobby'. Referencia cameraManager conectada en LobbyUI.

## [2026-04-11] - Lobby In-Game: Spawn de Personaje y CÃÆÃÂ¡mara DinÃÆÃÂ¡mica
### Cambios
- **LobbyCameraManager.cs**: Reescrito con modo SeguirPersonaje() ÃÂ¢Ã¢âÂ¬Ã¢â¬ï¿½ la cÃÆÃÂ¡mara sigue dinÃÆÃÂ¡micamente al Transform del jugador local via LateUpdate Lerp.
- **LobbyPlayerSpawner.cs** *(NUEVO)*: Spawner local (no red) del personaje del jugador en la escena de lobby. Instancia en asientos circulares alrededor de la hoguera.
- **LobbyUI.cs**: Integrado LobbyPlayerSpawner ÃÂ¢Ã¢âÂ¬Ã¢â¬ï¿½ llama SpawnPersonajeLocal() tras StartHost/StartClient y DestruirPersonajeLocal() al salir.
- **Scene_Menu.unity**: Creado Asientos_Lobby con 8 Transforms en cÃÆÃÂ­rculo (radio 2.5m) alrededor de la hoguera. LobbyPlayerSpawner configurado con prefab PlayerArmature y lista de asientos.

## [2026-04-11] - Lobby: Asientos sobre Troncos Reales
### Cambios
- **Scene_Menu.unity**: Eliminados 8 asientos ficticios. Creados 5 Transforms (Asiento_Tronco0..4) encima de los TRONCO reales de Hoguera Lobby Spawn, mirando hacia el centro de la hoguera. LobbyPlayerSpawner actualizado con los 5 IDs correctos.

## [2026-04-11] - Lobby: 2 Asientos por Tronco (10 total)
### Cambios
- **Scene_Menu.unity**: AÃÆÃÂ±adidos 5 asientos B (Asiento_TroncoXB), uno extra por cada tronco, desplazados a lo largo del eje del tronco. LobbyPlayerSpawner actualizado con los 10 asientos (A+B intercalados por tronco).

# #   [ 2 0 2 6 - 0 4 - 1 2 ] 
 -   M o d i f i c a d o   L o b b y C a m e r a M a n a g e r . c s :   A ÃÂ± a d i d a   a s i g n a c i ÃÂ³ n   m a n u a l   d e   c ÃÂ¡ m a r a   y   r e p o s i c i o n a m i e n t o   a u t o m ÃÂ¡ t i c o   d e l   H U D   3 D   ( P e r g a m i n o )   p a r a   c o r r e g i r   e r r o r e s   d e   d i s t a n c i a   y   v i s i b i l i d a d   a l   i n i c i a r . 
 
 -   C o r r e g i d o   e r r o r   d e   s i n t a x i s   ( l l a v e s   f a l t a n t e s )   e n   L o b b y C a m e r a M a n a g e r . c s   q u e   i m p e d ÃÂ­ a   l a   c o m p i l a c i ÃÂ³ n . 
 
 -   [ H O T F I X ]   R e e s c r i t u r a   t o t a l   d e   L o b b y C a m e r a M a n a g e r . c s   p a r a   e l i m i n a r   e l   c o n f l i c t o   d e   p r i o r i d a d e s   c o n   C a m e r a F P S L o b b y   y   a s e g u r a r   l a   v i s i b i l i d a d   d e l   H U D   e n   e l   m e n ÃÂº . 
 
 
## [2026-04-13] ÃÂ¢Ã¢âÂ¬Ã¢â¬ï¿½ SesiÃÆÃÂ³n: Fix HUD In-Game (CÃÆÃÂ¡mara Principal Nula)

### Causa RaÃÆÃÂ­z
Cambios anteriores (Gemini Flash) usaban cam.gameObject.SetActive(false) sobre la Main Camera al spawnear el jugador en el lobby. Esto hacÃÆÃÂ­a que Camera.main devolviera null en todos los demÃÆÃÂ¡s scripts (LobbyRaycastInteraction, etc.), rompiendo el raycast del HUD interactivo.

### Archivos Modificados
- LobbyHeadLook.cs ÃÂ¢Ã¢âÂ¬Ã¢â¬ï¿½ Cambio gameObject.SetActive(false) ÃÂ¢Ã¢â¬Â Ã¢â¬â¢ cam.enabled = false (solo componente, no GO) para no romper Camera.main.
- LobbyRaycastInteraction.cs ÃÂ¢Ã¢âÂ¬Ã¢â¬ï¿½ BÃÆÃÂºsqueda de cÃÆÃÂ¡mara en cascada: hijo ÃÂ¢Ã¢â¬Â Ã¢â¬â¢ Camera.main ÃÂ¢Ã¢â¬Â Ã¢â¬â¢ tag MainCamera. Re-bÃÆÃÂºsqueda dinÃÆÃÂ¡mica en Update si el jugador spawna despuÃÆÃÂ©s del Start.


## [2026-04-13] ÃÂ¢Ã¢âÂ¬Ã¢â¬ï¿½ Sesion: Eliminar movimiento de camara y pergamino

### Archivos Modificados
- `LobbyCameraManager.cs` ÃÂ¢Ã¢âÂ¬Ã¢â¬ï¿½ Eliminado metodo CorregirPosicionHUD() que movia el pergamino via transform. Eliminados campos inutilizados: pergaminoCanvas, distanciaAlHUD.


## [2026-04-13] ÃÂ¢Ã¢âÂ¬Ã¢â¬ï¿½ Session: Fix boton Host no pasaba a Lobby Interactions

### Problema
MostrarRoomPanel() solo activaba paneles UI pero no activaba la LobbyInteractionUI ni bloqueaba el cursor para el modo inmersivo de sala.

### Archivos Modificados
- `LobbyUI.cs` ÃÂ¢Ã¢âÂ¬Ã¢â¬ï¿½ MostrarRoomPanel() ahora activa LobbyInteractionUI, oculta joinPanel y bloquea el cursor (Locked) al entrar en sala.


### LobbyHeadLook
- Se aÃÆÃÂ±adiÃÆÃÂ³ explÃÆÃÂ­citamente fpCamera.gameObject.SetActive(IsOwner) para garantizar que, independientemente de cÃÆÃÂ³mo estÃÆÃÂ© guardado el prefab, la cÃÆÃÂ¡mara FPS se active al spawnear al jugador.


### Lobby UI y ParrelSync
- Se agregÃÆÃÂ³ soporte para **ParrelSync** en la inicializaciÃÆÃÂ³n de los servicios en la nube (UnityServices.InitializeAsync) dentro de \LobbyUI.cs\. Ahora cada clon (Player 2) usarÃÆÃÂ¡ un perfil ÃÆÃÂºnico separado. Esto soluciona los cuelgues (congelamientos) o errores silenciosos que impedÃÆÃÂ­an al segundo jugador entrar a los menÃÆÃÂºs de salas.


### Lobby UI y CÃÆÃÂ¡mara FPS
- Se agregÃÆÃÂ³ cÃÆÃÂ³digo en \LobbyHeadLook.cs\ para silenciar TODOS los AudioListener extra de la escena excepto el de la cÃÆÃÂ¡mara FPS actual. Esto detiene el spam masivo de consola ('There are 2 audio listeners in the scene') que ocultaba el cÃÆÃÂ³digo de sala y otros Debug Logs vitales.
- En \LobbyUI.cs\ se aÃÆÃÂ±adieron catch blocks genÃÆÃÂ©ricos para asegurar que si el servicio de Lobby de Unity lanza un error no controlado (ej. lÃÆÃÂ­mite de uso superado o mala conexiÃÆÃÂ³n), el botÃÆÃÂ³n Host no se quede permanentemente bloqueado.


### TransiciÃÆÃÂ³n a Gameplay
- Se actualizÃÆÃÂ³ \GameManager.cs\ para que al cargar la escena del juego (\Scene_Gameplay\), Netcode fuerce la destrucciÃÆÃÂ³n (Despawn) del \PlayerArmatureLobby\ antes de spawnear el personaje real. Esto evita errores debido a personajes duplicados o jugadores poseyendo errÃÆÃÂ³neamente un muÃÆÃÂ±eco del lobby.


### Personaje Gameplay
- Se aÃÆÃÂ±adiÃÆÃÂ³ una regla en \PlayerState.cs\ (OnNetworkSpawn) que fuerza al parÃÆÃÂ¡metro \isSitting\ del Animator a volverse falso al cargar la partida. Esto soluciona el problema de que el muÃÆÃÂ±eco apareciese deslumblando o atascado en la postura de la silla del lobby a la hora de jugar.


### TransiciÃÆÃÂ³n a Gameplay (CorrecciÃÆÃÂ³n)
- Se alterÃÆÃÂ³ \GameManager.cs\ para que el escaneo de limpieza busque especÃÆÃÂ­ficamente los componentes \PlayerLobbyPose\ de la escena y aplique el *Despawn* directamente a la red. El mÃÆÃÂ©todo anterior basado en la abstracciÃÆÃÂ³n \
etworkClient.PlayerObject\ fallaba porque el avatar de Lobby no utilizaba ese vÃÆÃÂ­nculo centralizado de Netcode.


### TransiciÃÆÃÂ³n a Gameplay (Mejora CrÃÆÃÂ­tica)
- Se reprogramÃÆÃÂ³ la eliminaciÃÆÃÂ³n de los muÃÆÃÂ±ecos \PlayerArmatureLobby(Clone)\ en \GameManager.cs\. Ahora la limpieza (\Despawn\) ocurre de inmediato al presionar el botÃÆÃÂ³n *Start Game* (antes del \LoadScene\), incluyendo aquellos objetos inactivos u ocultos en cachÃÆÃÂ©, en lugar de intentar borrarlos tras cargar la nueva escena, erradicando al 100% que puedan filtrarse a la partida.


### TransiciÃÆÃÂ³n a Gameplay (Fallo de IdentificaciÃÆÃÂ³n)
- Se descubriÃÆÃÂ³ que el prefab \PlayerArmatureLobby\ en realidad NO contenÃÆÃÂ­a el script \PlayerLobbyPose\ que \LobbyPlayerSpawner.cs\ marcaba en su tooltip. Por eso la limpieza fallaba. Se actualizÃÆÃÂ³ \GameManager.cs\ para cazar estos clones por su nombre bruto (FindObjectsByType<NetworkObject> + '.name.Contains'), asegurando su exterminaciÃÆÃÂ³n ignorando quÃÆÃÂ© scripts lleven o no.


### Lobby Animaciones
- Se agregÃÆÃÂ³ RandomLobbySit.cs para randomizar la animaciÃÆÃÂ³n sitting

### Lobby Animaciones (Fix Editor) 
- Se arreglÃÆÃÂ³ un bug visual del editor en \RandomLobbySit.cs\ retrasando la inyecciÃÆÃÂ³n 1 frame y usando asignaciÃÆÃÂ³n de clips directos para prevenir crasheos del Grafo en el Animator.

### Lobby Animaciones (Silenciador de Consola)
- Se aÃÆÃÂ±adiÃÆÃÂ³ script para limpiar el lag visual del Editor de Unity.

### Lobby Animaciones (Fix Congelamiento)
- Se reprogramÃÆÃÂ³ \RandomLobbySit.cs\ para forzar el rebobinado del tiempo del Animator interno (\_animator.Play\) al cambiar la animaciÃÆÃÂ³n dinÃÆÃÂ¡micamente, solucionando el problema de personajes que quedaban atascados en la pose final de clips cortos.

### Lobby Animaciones (Suavizado de Transicion)
- Se habilitÃÆÃÂ³ CrossFadeInFixedTime en RandomLobbySit.cs para fundir animaciones aleatorias sin cortes.

### Lobby Animaciones (Modo Espejo)
- Se aÃÆÃÂ±adiÃÆÃÂ³ lÃÆÃÂ³gica a \RandomLobbySit.cs\ para activar aleatoriamente un parÃÆÃÂ¡metro Bool de 'Mirror' en el Animator, multiplicando visualmente la variedad de animaciones de sentado.

### Lobby Animaciones (DesincronizaciÃÆÃÂ³n Realista)
- Se reestructurÃÆÃÂ³ la rutina en \RandomLobbySit.cs\ integrando tiempos de espera variables (\minIdleTime\, \maxIdleTime\) para que el personaje repose en la pose base entre animaciones, y se introdujo un \maxStartupDelay\ para desfasar el reloj interno entre los jugadores.

### Lobby Animaciones (Estilo PelÃÆÃÂ­cula / Stop-Motion)
- Se integrÃÆÃÂ³ un limitador de fotogramas opcional (\useCinematicFramerate\) en \RandomLobbySit.cs\ para simular un estilo de interpolaciÃÆÃÂ³n a saltitos o pelÃÆÃÂ­cula animada (ej. 12-24 fps) manipulando el tiempo natural del Animator.

### Lobby Animaciones (Fix Desplazamiento)
- Se desactivÃÆÃÂ³ forzosamente el \pplyRootMotion\ en \RandomLobbySit.cs\ para evitar que las animaciones con datos de movimiento muevan al personaje de su asiento.

### Lobby Animaciones (Mezcla de Cine) 
- ReestructuraciÃÆÃÂ³n total de \RandomLobbySit.cs\ para usar un sistema de **Blend Tree**. Ahora las animaciones no se interrumpen, sino que se funden suavemente mediante un parÃÆÃÂ¡metro de mezcla gestionado por cÃÆÃÂ³digo, eliminando el 100% de los saltos visuales.

### Lobby Animaciones (FIX CRITICO - Sin Override Controller)
- Reestructura total de \RandomLobbySit.cs\. Se eliminÃÆÃÂ³ el uso de \AnimatorOverrideController\ (causa del crash del Editor). Ahora el sistema solo usa parÃÆÃÂ¡metros nativos del Animator (\SitActionBlend\, \SitActionIndex\) controlados por cÃÆÃÂ³digo, compatible 100% con el Animator abierto.

### Editor - Herramienta de Reparacion
- Se creÃÆÃÂ³ \FixBrokenAnimatorTransitions.cs\ en \Assets/Editor/\ para detectar y eliminar transiciones rotas en el Animator Controller que causaban el error masivo \GenerateConnectionKey NullReferenceException\.

### Editor - Fix Animator Crash (v3)
- Se actualizÃÆÃÂ³ \FixBrokenAnimatorTransitions.cs\ para eliminar automÃÆÃÂ¡ticamente estados con \motion = null\ (causa raÃÆÃÂ­z del error \GenerateConnectionKey\). Afectados: \AC_ScrollUI\, \StarterAssetsThirdPerson\, \TestController\.

## 2026-04-14
### RandomLobbySit - Cola global de clips
- AÃÆÃÂ±adida clase estÃÆÃÂ¡tica \LobbyClipQueue\ para evitar que dos personajes reproduzcan el mismo clip simultÃÆÃÂ¡neamente.
- AÃÆÃÂ±adida variable \_lastClip\ por instancia para evitar repeticiÃÆÃÂ³n consecutiva del mismo clip.
### Editor - FixAnimationRootMotion
- Creado script para fijar Root Transform Y en FBXs de animaciones sentado (evita que personajes se levanten).
### AutoCloseAnimatorFix
- Desactivado: ya no cierra el Animator al darle Play.

### Reemplazo de Refugios
- Creado script `ReplaceShelterTool.cs` (borrado despuÃÂ©s temporalmente o mantenido en Editor/) para instanciar en lote el modelo actualizado `casa aldeano` y eliminar los `SM_Bld_Preset_Shelter_01_Optimized` de la escena principal para ahorrar memoria y cumplir requisitos narrativos.
- 
 
 1 5 / 0 4 / 2 0 2 6 : 
 
 G a m e M a n a g e r . c s 
 
 P l a y e r S t a t e . c s 
 
 H o u s e C o n t r o l l e r . c s 
 
 - 
 
 S i s t e m a 
 
 d e 
 
 C a s a s 
 
 y 
 
 s p a w n s 
 
 d i u r n o s 
 
 i m p l e m e n t a d o . 
 
 
## [2026-04-17] - Animaciones de Lobby
- **RandomLobbySit.cs**: Eliminada la restricciÃ³n de exclusiÃ³n por nombre para permitir animaciones aleatorias en todos los personajes.

## [2026-04-17] - Correcciones de Lobby (Sync & Desync)
- **LobbyPlayerSpawner.cs**: Movida la lÃ³gica de ocultaciÃ³n de aldeanos de prueba a OnNetworkSpawn local para asegurar que los jugadores tardÃ­os vean el lobby limpio.
- **RandomLobbySit.cs**: AÃ±adida inicializaciÃ³n de semilla por instancia y desfase temporal del Animator para evitar el efecto espejo entre personajes.
- **RandomLobbySit.cs**: Eliminada lÃ³gica de 'Play' inicial que causaba que los personajes aparecieran de pie por error de capa/estado.
- **LobbyHeadLook.cs**: Habilitada la rotaciÃ³n fÃ­sica del hueso de la cabeza para el dueÃ±o, permitiendo ver el movimiento desde cÃ¡maras externas o espejos.
- **LobbyHeadLook.cs**: AÃ±adida auto-detecciÃ³n del hueso 'Head'. Ahora el script encuentra la cabeza del personaje automÃ¡ticamente si no se asigna en el Inspector.
- **LobbyHeadLook.cs**: Refactorizada la rotaciÃ³n del hueso de la cabeza para sumar el movimiento del ratÃ³n sobre la animaciÃ³n base, evitando que la cabeza pierda los movimientos naturales del 'idle'. AÃ±adidas opciones en el inspector para invertir ejes de modelos como los de Synty.
- **LobbyHeadLook.cs**: Corregido problema de cuellos rotos, se ha expuesto el mapeo de ejes en el inspector para configurarlos fÃ¡cilmente dependiendo del rig del personaje (X, Y o Z).

## [2026-04-17] - Refactor Modular del Sistema de Misiones (Data-Oriented Design)
- **GameEnums.cs**: AÃ±adidos enums MaterialType y ZoneID para usar en en lugar de strings.
- **ItemData.cs**: Nueva clase ScriptableObject para crear objetos modulares sin tocar cÃ³digo.
- **QuestData.cs**: Refactorizado para usar una estructura estricta de 3 pasos basada en MaterialType y ZoneID.
- **QuestManager.cs**: Nuevo Singleton para registrar todas las misiones y sincronizarlas de forma Server-Authoritative.
- **PlayerQuestTracker.cs**: Refactorizado para soportar un Ãºnico slot de misiÃ³n y comprobaciÃ³n de distancia mÃ¡xima (4m) desde el servidor.
- **QuestInteractable.cs**: Actualizado para emitir eventos de recolecciÃ³n basados en MaterialType en lugar de usar strings genÃ©ricos.
- **Fixes (CompilaciÃ³n)**: Solucionado warning de CS0108 en HouseController.cs, actualizados atributos obsoletos [ServerRpc] en PlayerQuestTracker.cs por los nuevos [Rpc] de Netcode, y reparado CraftingStation.cs para enlazarse con el nuevo PlayerQuestTracker modular.
- **Fixes (CompilaciÃ³n Editor)**: Actualizado QuestDataEditor.cs para incluir el namespace de Core.QuestSystem y mostrar en el Inspector los nuevos parÃ¡metros modulares (questID y objetoRecompensa) en lugar de los antiguos (objetoRecompensaFinal).
- **Fixes (Warnings API)**: Actualizados todos los scripts ({30} archivos encontrados) para usar FindAnyObjectByType en lugar de FindFirstObjectByType, y usar FindObjectsInactive en vez de FindObjectsSortMode para callar los deprecation warnings de Unity 6.
- **Modular Quest System**: Finalizada la actualizaciÃ³n de Enums (MaterialType y ZoneID) y corregidos todos los errores de compilaciÃ³n en SpawnAreaManager, PuzzleSequenceManager, QuestData y QuestInteractable.
- **Modular Inventory**: Refactorizado PlayerInventory para usar un array dinÃ¡mico de materiales basado en el enum MaterialType.
- **Modular Crafting**: Actualizado CraftingStation para soportar recetas configurables (listas de requisitos) desde el Inspector.
- **Quest Rewards**: Los objetos QuestInteractable ahora pueden otorgar materiales al inventario del jugador tras la interacciÃ³n.
- **Misiones BÃ¡sicas**: Implementado sistema de recompensas de oro y recompensas fÃ­sicas para las misiones.
- **Enums**: AÃ±adidos nuevos tipos de interacciÃ³n (LeÃ±a, Afilado, Armadura, etc.) para misiones diarias.
- **CorrecciÃ³n**: Corregido error de compilaciÃ³n CS0246 (List<>) en CraftingStation.cs.

## [2026-06-16] - Misiones Normales, Tienda de Pergaminos y Mejoras de HUD
- **BasicQuestInteractable.cs** (NUEVO): Creado trigger local de interacciÃÂ³n para misiones bÃÂ¡sicas de recolecciÃÂ³n y limpieza.
- **BasicQuestSetup.cs** (NUEVO): Creado script de inicializaciÃÂ³n dinÃÂ¡mica de escena que configura los interactuables de misiones bÃÂ¡sicas, el comerciante y el panel de tienda al cargar Scene_Gameplay.
- **GameplayUI.cs**: AÃÂ±adida vinculaciÃÂ³n de eventos locales del jugador y modificado RedibujarListaTareas para estructurar en HUD las Tareas del DÃÂ­a, MisiÃÂ³n Activa y Materiales Recogidos.
- **PlayerInventory.cs**: Corregido el tamaÃÂ±o del array de materiales a 30 para prevenir desbordamientos de ÃÂ­ndice y aÃÂ±adida vinculaciÃÂ³n a HUD local en spawn.
- **PlayerQuestTracker.cs**: AÃÂ±adida vinculaciÃÂ³n a HUD local en spawn.
- **GameManager.cs**: SincronizaciÃÂ³n corregida para misiones bÃÂ¡sicas en transiciones de dÃÂ­a y primer dÃÂ­a.
- **ShopPanelController.cs**: Corregida propiedad de alineaciÃÂ³n en VerticalLayoutGroup.
- **Quest Data Assets**: Creados assets para Limpieza, Huerto, Velas y modificado Madera.
- **PlayerToolVisuals.cs** (NUEVO): Creado script para activar y desactivar de forma recursiva objetos ocultos de herramientas (hacha) en la mano del jugador.
- **NetworkPlayerSetup.cs**: Acoplamiento dinÃÂ¡mico de PlayerToolVisuals al spawnear cualquier jugador en red.
- **QuestInteractable.cs**: Integrado el hacha para misiones normales de talado de madera.
- **BasicQuestInteractable.cs**: Corregida visibilidad del hacha al talar leÃÂ±a en misiones bÃÂ¡sicas.
- **PlayerWateringParticles.cs** (NUEVO): Creado script para instanciar, configurar y controlar un sistema de partÃÂ­culas de agua (azul translÃÂºcido, colisiones fÃÂ­sicas y desvanecimiento) en la punta del balde/regadera del jugador.
- **NetworkPlayerSetup.cs**: Acoplamiento dinÃÂ¡mico de PlayerWateringParticles al spawnear cualquier jugador.
- **QuestInteractable.cs**: Sincronizada la activaciÃÂ³n de partÃÂ­culas de agua al regar en misiones normales.
- **BasicQuestInteractable.cs**: Sincronizada la activaciÃÂ³n de partÃÂ­culas de agua al regar en misiones bÃÂ¡sicas.
- **QuestInteractable.cs**: AÃÂ±adida validaciÃÂ³n de paso de misiÃÂ³n en OnTriggerEnter y Update para evitar interacciones no asignadas, y soporte de encendido permanente para velas.
- **BasicQuestInteractable.cs**: AÃÂ±adida validaciÃÂ³n de paso de misiÃÂ³n en OnTriggerEnter y Update para evitar interacciones no asignadas, y soporte de encendido permanente para velas.

## [2026-06-16] - Animaciones: Beber, Regar y ApuÃÂ±alar
- **PlayerToolVisuals.cs**: AÃÂ±adido soporte para daga y pociÃÂ³n (SetDagaActive, SetPocionActive).
- **BasicQuestInteractable.cs**: Rutina PlayQuickAnimationRoutine ampliada para beber (Frasco, ID 6), apuÃÂ±alar (Afilado, ID 7) con visibilidad de herramientas.
- **BasicQuestSetup.cs**: Riego actualizado a ID 6; aÃÂ±adidos bloques de detecciÃÂ³n para pociones (Frasco, ID 6) y daga/diana (Afilado, ID 7).


## [2026-06-16] - Herramientas de mano con nombres exactos
- **PlayerToolVisuals.cs**: Reescrito con los 10 objetos reales del proyecto (axe, escoba, regadera, Daga, Pocion, PocionVelocidad, Antorcha, ManzanaDeOro, BombaApestosa, Lupa). HideAll() oculta todo al inicio.
- **BasicQuestInteractable.cs**: PlayQuickAnimationRoutine activa escoba (ID 4) y regadera (ID 6) ademÃÂ¡s de hacha/pociÃÂ³n/daga.


## [2026-06-16] - Zonas por suelo_*
- **BasicQuestSetup.cs**: Reemplazado escaneo de props por detecciÃÂ³n de objetos suelo_* (altar, aserradero, cementerio, arbol, cueva, torre). Cada suelo genera un hijo ZonaTrigger invisible con SphereCollider sin tocar el MeshCollider del suelo.

## [2026-06-16] - Sistema VelaController
- **VelaController.cs**: Nuevo script por vela. Apaga fuego en Awake, enciende permanente con Encender() (loop forzado).
- **BasicQuestSetup.cs**: Escanea objetos 'vela' y acopla VelaController automaticamente al inicio.
- **BasicQuestInteractable.cs**: Logica de velas delegada a FindObjectsByType<VelaController> y Encender(). Eliminado fireObject obsoleto.

## [2026-06-16] - Reset de velas por jugador
- **VelaController.cs**: AÃÂ±adido Apagar() publico que resetea EstaEncendida y detiene particulas.
- **BasicQuestInteractable.cs**: Al entrar al trigger de velas con mision activa, se llama Apagar() en todas las VelaController. Cada jugador encuentra velas apagadas independientemente.

## [2026-06-16] - Troncos madera como interactuables
- **BasicQuestSetup.cs**: Nuevo escaneo de objetos 'madera*' (madera, madera1..4). Cada tronco recibe ZonaTrigger hijo radio=2m con LeÃÂ±a/Aserradero/ID2. MeshCollider original intacto.

## [2026-06-16] - QuestZonePoint manual
- **QuestZonePoint.cs**: Nuevo componente para colocar puntos de mision manualmente en escena. Selector de tipo (Limpiar/Regar/Talar/Velas/Apunalar/Beber), zona configurable y radio. Gizmos de colores en el editor (azul=limpiar, verde=regar, marron=talar, amarillo=velas, rojo=apunalar, morado=beber).

## [2026-06-16] - Lobos reciben misiones basicas
- **GameManager.cs**: Eliminada la exclusion isWolf.Value del reparto de misiones basicas. Ahora todos los jugadores vivos (aldeanos y lobos) reciben misiones basicas al inicio/nuevo dia.

## [2026-06-16] - UI Misiones Lobo
- **GameplayUI.cs**: Eliminada la restriccion !esLobo de la seccion 'MISION ACTIVA'. Ahora el lobo puede ver los pasos y ubicaciones de su mision basica bajo el titulo 'SIMULAR MISION'.

## [2026-06-16] - Limpieza de UI de Tareas
- **GameplayUI.cs**: Se ocultan de la interfaz las tareas del sistema antiguo que no tenian nombre ('Tarea sin nombre'). Ahora solo se dibuja la seccion de 'SIMULAR TAREAS' si hay misiones reales configuradas. Asi la UI queda limpia y el lobo solo ve su nueva mision basica.

## [2026-06-16] - Restablecer Tareas Antiguas
- **GameplayUI.cs**: Revertido el filtro de 'Tarea sin nombre'. Ahora, si una tarea antigua no tiene nombre configurado en el Inspector, se mostrara como 'Tarea en el mapa' en lugar de desaparecer, para que los jugadores puedan seguir completandolas.

## [2026-06-16] - Nombres automaticos para TaskPoints
- **TaskPoint.cs**: Si una tarea antigua no tiene nombre configurado en el Inspector ('Tarea sin nombre'), el script ahora tomara automaticamente el nombre del GameObject (ej. 'suelo_aserradero' -> 'Suelo aserradero') para mostrarlo en la UI.

## [2026-06-16] - Cierre forzado de Tienda
- **ShopPanelController.cs**: Se ha forzado la llamada a CerrarTienda() fuera del condicional de validacion de monedas en ComprarObjeto para asegurar que la UI de la tienda siempre se cierre tras presionar el boton de compra.

## [2026-06-17] - Correccion Asignacion Misiones Basicas
- **QuestManager.cs**: GetAllAvailableQuests ahora incluye todas las misiones registradas dinamicamente en vez de solo las de Inspector.
- **PlayerQuestTracker.cs**: AÃ±adido ServerAssignQuest para evitar problemas de permisos RPC (Owner) cuando el GameManager asigna misiones al inicio del dia.

## [2026-06-17] - Remocion TaskPoint Comerciante
- **BasicQuestSetup.cs**: Se ha programado para destruir automaticamente el componente TaskPoint del Puesto del Comerciante (Market_Stall_07) para evitar que sea considerado una tarea aleatoria.

## [2026-06-17] - Remocion Definitiva Comerciante de Tareas
- **GameManager.cs**: Filtrado explicito de Market_Stall_07 al coleccionar puntos de tarea al inicio del juego, ya que la destruccion del BasicQuestSetup llegaba un frame tarde por orden de ejecucion.

## [2026-06-17] - Correccion Rol UI sin tareas
- **GameplayUI.cs**: Actualizada la asignacion de la variable esLobo en MostrarRol() para garantizar que el texto rojo de Objetivo Real se imprima en el pergamino aun cuando la lista antigua de tareas aleatorias este vacia.

## [2026-06-17] - Correccion Asignacion Mision Basica Nula
- **GameManager.cs**: La asignacion de misiones basicas ahora itera sobre los PlayerState fisicos en la escena en vez del diccionario ConnectedClients, previniendo que se saltara la asignacion si el PlayerObject no estaba aun referenciado internamente por Netcode durante el frame de carga.

## [2026-06-17] - Correccion Actualizacion UI Local Host
- **PlayerQuestTracker.cs**: Se ha forzado una llamada local a LoadLocalQuest() cuando el Host asume el rol de Servidor al asignar una misiÃ³n, garantizando que su propia interfaz grÃ¡fica se actualice instantÃ¡neamente sin depender del evento asÃ­ncrono de Netcode.

## [2026-06-17] - Correccion Race Condition UI vs Asignacion de Mision
- **PlayerQuestTracker.cs**: AÃ±adido mÃ©todo ForzarActualizacionUI() para re-emitir el estado de misiÃ³n a la UI cuando esta se suscribe tarde.
- **GameplayUI.cs**: VincularJugadorLocal ahora detecta si ya habia una misiÃ³n asignada antes de suscribirse y la carga explicitamente.

## [2026-06-17] - Eliminacion Sistema Viejo de TaskPoints
- **TaskPoint.cs**: ELIMINADO completamente.
- **TaskInfo.cs**: ELIMINADO completamente.
- **TaskPointEditor.cs**: ELIMINADO completamente.
- **BasicQuestSetup.cs**: ELIMINADO completamente.
- **MinigameBase.cs, MinigameDescarga.cs, MinigameRunas.cs, MinigameSlidingPuzzle.cs, MinigameTeclado.cs**: ELIMINADOS (dependian de TaskPoint).
- **MinigameTrigger.cs, TapestryMinigame.cs, TapestryRestorer.cs**: ELIMINADOS (dependian de TaskPoint).
- **GameManager.cs**: Eliminadas AsignarTareasATodosLosJugadores y RecibirAsignacionTareasClientRpc.
- **GameplayUI.cs**: Eliminados tareasActuales, ActualizarListaTareas, SetTareaActiva, CompletarTareaActiva, FallarTareaActiva y AvanzarSiguienteTareaDisponible.

## [2026-06-17] - Fix Race Condition Misiones Basicas Lobo
- **GameManager.cs**: Bug critico donde las misiones basicas no se mostraban al jugar como lobo. SpawnAsPlayerObject de NGO necesita un frame para propagar los PlayerObjects. Se anadio corrutina AsignarMisionesConRetardo con WaitForEndOfFrame. Se reemplaza FindObjectsByType por ConnectedClients como fuente de verdad del servidor.

## [2026-06-17] - Fix Espera Activa PlayerObjects NGO
- **GameManager.cs**: WaitForEndOfFrame insuficiente para NGO. La corrutina ahora espera activamente (hasta 5s) a que todos los PlayerObjects esten listos en ConnectedClients antes de asignar misiones. Anadidos logs de diagnostico detallados.

## [2026-06-17] - Filtro Misiones Sin Objetos En Escena
- **GameManager.cs**: Anadido metodo MisionTieneInteractuablesEnEscena(). Antes de incluir una mision en el sorteo diario, verifica que cada paso tenga al menos un BasicQuestInteractable con el MaterialType y ZoneID correctos en la escena. Misiones sin objetos colocados quedan descartadas automaticamente.

## [2026-06-17] - Fix UI Mision Lobo No Visible
- **GameplayUI.cs**: MostrarRol() disparaba antes de VincularJugadorLocal(), por lo que el primer dibujo ocurria con localQuestTracker==null y la mision no aparecia. Ahora VincularJugadorLocal() lee isWolf directamente desde PlayerState y fuerza RedibujarListaTareas() tras vincular el tracker.

## [2026-06-17] - Fix MaterialType Incorrecto en Assets de Misiones Basicas
- **Huerto.asset**: materialRequerido corregido de 0 (Ninguno) a 17 (BaldeAgua).
- **Limpieza.asset**: materialRequerido corregido de 0 (Ninguno) a 15 (Escoba).
- **Madera.asset**: materialRequerido corregido de 0 (Ninguno) a 8 (Lena).

## [2026-06-17] - Fix Animacion Regar
- **QuestZonePoint.cs**: Cambiado animID para TipoMisionBasica.Regar de 6 a 9. El AnimatorController (StarterAssetsThirdPerson) tiene mapeada la animacion 'regar' al InteractionType 9. El 6 era compartido por error con 'Beber pocion' (tableaction).

## [2026-06-17] - Fix Bug Mision Sin Nombre en UI
- **PlayerQuestTracker.cs & GameplayUI.cs**: FixedString32Bytes en Unity Netcode anadia bytes nulos (\0) al final del string, lo que provocaba que QuestManager.GetQuestByID fallara al buscar la clave exacta en el diccionario. Se ha anadido .TrimEnd('\0') a todas las llamadas de ToString() de currentQuestID para que UI y Tracker puedan cargar el ScriptableObject correctamente.

## [2026-06-17] - Fix Duplicado BasicQuestInteractable
- **QuestZonePoint.cs**: Si el usuario habia anadido manualmente BasicQuestInteractable, QuestZonePoint anadia uno nuevo en Awake() provocando duplicados. Ahora usa GetComponent() primero.

## [2026-06-17] - Fix Robusto de Nombres de Misiones
- **QuestManager.cs**: Modificado GetQuestByID para ignorar espacios, saltos de linea y mayusculas/minusculas. Asi garantizamos que el sistema encuentre el pergamino sin importar basura de red oculta.

## [2026-06-18] - ROLLBACK: Revertidos cambios que rompÃ­an el movimiento
- **NetworkPlayerSetup.cs**: Eliminados los `AddComponent<PlayerToolVisuals>()` y `AddComponent<PlayerWateringParticles>()` dinÃ¡micos que se aÃ±adieron durante los parches de anclaje. Esos AddComponent ejecutaban Awake() de forma prematura sobre el Animator del jugador, corrompiendo su estado y dejando al personaje congelado. Revertido a la versiÃ³n del Ãºltimo commit funcional.
- **PlayerInventory.cs**: Eliminado el bloque de auto-anclaje a la mano (`GetBoneTransform + SetParent`) que se aÃ±adiÃ³ en `Awake()`. Ese cÃ³digo interferÃ­a con la inicializaciÃ³n del Animator. Revertido al Awake original: solo `GetComponent<PlayerState>()`.

## [2026-06-18] - Fix DEFINITIVO de CÃ¡mara/Movimiento (LobbyHeadLook)
- **LobbyHeadLook.cs**: **CAUSA RAÃZ DEL CONGELAMIENTO ENCONTRADA.** El script `LobbyHeadLook` estaba en el Prefab de gameplay (`PlayerArmature`). Al arrancar en `Scene_Gameplay`, su `OnNetworkSpawn()` detectaba que no era el Lobby y ejecutaba `fpCamera.enabled = false`, apagando la Main Camera del juego. Sin cÃ¡mara activa, el juego parecÃ­a completamente congelado aunque el sistema de movimiento funcionara correctamente. SoluciÃ³n: eliminada la lÃ­nea `fpCamera.enabled = false` del bloque de guardia de gameplay â el script ahora solo se desactiva a sÃ­ mismo (`enabled = false`) sin tocar la cÃ¡mara.

## [2026-06-18] - Fix Definitivo de Movimiento en Gameplay
- **NetworkPlayerSetup.cs**: El `ThirdPersonController` y `CharacterController` ahora se habilitan EXPLÃCITAMENTE en `OnNetworkSpawn()` para el jugador local (IsOwner). Antes, el sistema dependÃ­a de que `AlCambiarDeFase` lo habilitara, lo que creaba una condiciÃ³n de carrera donde el cambio de fase podÃ­a dispararse antes de que el jugador estuviera completamente listo. Ahora el propio script de setup es el Ãºnico responsable de activar el movimiento.

## [2026-06-18] - Fix Congelamiento de Movimiento (Avatar no Humanoide)
- **PlayerInventory.cs** y **PlayerToolVisuals.cs**: Envuelto `Animator.GetBoneTransform()` en un bloque `try-catch`. Si el modelo 3D del jugador usa un Avatar de tipo "GenÃ©rico" en lugar de "Humanoide", esta funciÃ³n de Unity provocaba una excepciÃ³n (`InvalidOperationException`) que interrumpÃ­a en seco la carga del resto de scripts del jugador, dejando el controlador de movimiento inhabilitado. Ahora se ignora el error y usa el sistema de respaldo correctamente.

## [2026-06-18] - Fix Crash de PartÃ­culas de Agua
- **PlayerWateringParticles.cs**: Corregido error `Setting the duration while system is still playing is not supported`. Al aÃ±adir un `ParticleSystem` por cÃ³digo en Unity, este arranca en modo `Play` automÃ¡ticamente por el `playOnAwake` predeterminado. AÃ±adido un `Stop(StopEmittingAndClear)` forzado justo despuÃ©s de su creaciÃ³n para permitir que el script asigne sus parÃ¡metros (duration, color, etc) de forma segura antes de usarlo.

## [2026-06-17] - Fix Anclaje de Objetos en la Mano
- **PlayerInventory.cs** y **PlayerToolVisuals.cs**: AÃ±adido uso de `Animator.GetBoneTransform(HumanBodyBones.RightHand)` como mÃ©todo principal de bÃºsqueda. Esto soluciona un problema crÃ­tico donde el script podÃ­a encontrar un `GameObject` estÃ¡tico llamado "RightHand" en lugar del hueso animado real del esqueleto, lo que causaba que los objetos no siguieran la animaciÃ³n `Idle`. El script ahora extrae el hueso directamente desde el motor de animaciÃ³n de Unity.

## [2026-06-18] - Fix de LocomociÃ³n en Gameplay
- **StarterAssetsThirdPerson.controller**: Cambiado el peso por defecto de la capa InteractingDown a 0 para que no congele la locomociÃ³n de las piernas.
- **StarterAssetsThirdPerson.controller**: Corregido y guardado el peso predeterminado de la capa a 0 ya que seguÃ­a persistiendo en 1.
- **CheckPlayerInput.cs** (NUEVO): Herramienta de diagnÃ³stico para inspeccionar los componentes y estado de input del jugador local en tiempo de ejecuciÃ³n.
- **FixPlayerInputPrefab.cs** (NUEVO): Script para reasignar automÃ¡ticamente el asset `StarterAssets.inputactions` al componente `PlayerInput` del Prefab del jugador.
- **PlayerArmature.prefab**: Reasignado el asset de Input Actions que se habÃ­a perdido (quedando en NULL), restaurando el control WASD y de cÃ¡mara.
- **PlayerArmatureLobby.prefab**: Reasignado el asset de Input Actions al prefab del lobby.
- **PlayerToolVisuals.cs**: Destruidos por cÃ³digo los colisionadores de todas las herramientas visuales en Awake y cambiada su capa recursivamente a Ignore Raycast (capa 2) para evitar cualquier interferencia o choque con Cinemachine.

## [2026-06-18] - CorrecciÃ³n de ExportaciÃ³n de Animaciones a FBX para Blender
- **ExportAnimationsToFbx.cs**: Configurada la exportaciÃ³n de animaciones a formato binario (Binary FBX) e incorporado el parÃ¡metro de inclusiÃ³n de modelo + animaciÃ³n. Adicionalmente, se programÃ³ la eliminaciÃ³n automÃ¡tica de todos los componentes de tipo Light y Camera del prefab temporal (destruyendo dependencias como UniversalAdditionalLightData) antes de exportar, para evitar el error de importaciÃ³n en Blender 5.0 (CyclesLightSettings cast_shadow AttributeError).
- 2026-06-21 - UniversalQuestInteractable.cs: Efectos visuales al completar

## [2026-06-21]
- **QuestInteractable.cs**: Sincronizacion Animacion-Accion

- **UniversalQuestInteractable.cs y PlayerInventory.cs**: Bloqueo estricto del mapa de acciones de PlayerInput para evitar cancelaciones de interacción accidentales por micro-movimientos.

- **QuestDataEditor.cs**: Actualizado el Editor Personalizado para mostrar las variables ocultas de interacción (Duración, Animación, etc) que estaban escondidas por el bypass de Odin Inspector.

- **PlayerToolVisuals.cs**: Añadido soporte para la herramienta 'pickaxe' (Pico) en el sistema visual.

- **InteractivableMisionUniversal.cs**: Creado nuevo script definitivo para misiones interactivas con sincronización en cadena, validación estricta de distancia en Servidor, y escudo Anti-Freeze en el Cliente.
