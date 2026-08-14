using UnityEngine;
using Unity.Netcode;
using System;

namespace Quests
{
    /// <summary>
    /// Componente que gestiona la transformación visual del jugador entre Aldeano y Lobo.
    /// Sincronizado por red de forma autoritativa utilizando la fase del GameManager.
    /// </summary>
    public class PlayerTransformation : NetworkBehaviour
    {
        [Header("Prefabs Visuales (Instanciación)")]
        [SerializeField] private GameObject villagerVisualPrefab;
        [SerializeField] private GameObject wolfVisualPrefab;
        [SerializeField] private Transform visualContainer;

        [Header("Objetos en Jerarquía (Alternativa sin instanciar)")]
        [Tooltip("Si tus modelos ya están dentro del personaje, arrástralos aquí para que solo se enciendan/apaguen.")]
        [SerializeField] private GameObject villagerVisualObject;
        [SerializeField] private GameObject wolfVisualObject;

        [Header("Renderers de Malla (Alternativa para el mismo esqueleto)")]
        [Tooltip("Si el lobo comparte los mismos huesos que el aldeano, arrastra sus mallas aquí.")]
        [SerializeField] private SkinnedMeshRenderer villagerMeshRenderer;
        [SerializeField] private SkinnedMeshRenderer wolfMeshRenderer;

        [Header("Configuración del Rol")]
        [Tooltip("Si es true, este jugador se transformará en lobo durante la noche.")]
        [SerializeField] private bool isWerewolfRole = true;

        // Variable de red que determina si el visual de lobo debe estar activo
        public readonly NetworkVariable<bool> IsWolfVisualActive = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // Evento que notifica a otros componentes (ej: Animador, Físicas) que el Animator ha cambiado
        public event Action<Animator> OnAnimatorChanged;

        private GameObject currentVisualInstance;
        private GameManager gameManager;
        private PlayerState playerState;
        private bool isSubscribedToGameManager = false;
        private bool isSubscribedToPlayerState = false;

        /// <summary>
        /// Asigna o remueve el rol de hombre lobo a este jugador (Solo Servidor).
        /// </summary>
        public void SetWerewolfRole(bool isWerewolf)
        {
            if (!IsServer) return;
            isWerewolfRole = isWerewolf;
            if (gameManager != null)
            {
                EvaluateTransformationServer(gameManager.currentPhase.Value);
            }
            else
            {
                EvaluateTransformationServer(GamePhase.Dia);
            }
        }

        private void Awake()
        {
            playerState = GetComponent<PlayerState>();
        }

        public override void OnNetworkSpawn()
        {
            // Suscribirse a cambios visuales (Regla 28)
            IsWolfVisualActive.OnValueChanged += OnVisualStateChanged;

            // Intentar buscar GameManager y suscribirse
            gameManager = GameManager.Instance;
            TrySubscribeToGameManager();
            TrySubscribeToPlayerState();

            // Si somos el servidor, sincronizar el rol inicial de PlayerState
            if (IsServer && playerState != null)
            {
                isWerewolfRole = playerState.isWolf.Value;
                EvaluateTransformationServer(gameManager != null ? gameManager.currentPhase.Value : GamePhase.Dia);
            }

            // Inicializar con el estado actual
            UpdateVisualModel(IsWolfVisualActive.Value);
        }

        private void Start()
        {
            // Fallback por si no se encontró en OnNetworkSpawn debido al orden de inicialización
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }
            TrySubscribeToGameManager();
            TrySubscribeToPlayerState();
        }

        private void TrySubscribeToGameManager()
        {
            if (IsServer && gameManager != null && !isSubscribedToGameManager)
            {
                // El servidor se suscribe al cambio de fase
                gameManager.currentPhase.OnValueChanged += OnGamePhaseChangedServer;
                isSubscribedToGameManager = true;
                
                // Aplicar estado inicial del servidor
                EvaluateTransformationServer(gameManager.currentPhase.Value);
            }
        }

        private void TrySubscribeToPlayerState()
        {
            if (IsServer && playerState != null && !isSubscribedToPlayerState)
            {
                playerState.isWolf.OnValueChanged += OnWolfRoleChangedServer;
                isSubscribedToPlayerState = true;
            }
        }

        private void OnWolfRoleChangedServer(bool previousValue, bool newValue)
        {
            if (IsServer)
            {
                isWerewolfRole = newValue;
                if (gameManager != null)
                {
                    EvaluateTransformationServer(gameManager.currentPhase.Value);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            IsWolfVisualActive.OnValueChanged -= OnVisualStateChanged;

            if (IsServer && gameManager != null && isSubscribedToGameManager)
            {
                gameManager.currentPhase.OnValueChanged -= OnGamePhaseChangedServer;
                isSubscribedToGameManager = false;
            }

            if (IsServer && playerState != null && isSubscribedToPlayerState)
            {
                playerState.isWolf.OnValueChanged -= OnWolfRoleChangedServer;
                isSubscribedToPlayerState = false;
            }
        }

        /// <summary>
        /// Servidor: Evalúa e impone el estado de transformación cuando cambia la fase en el GameManager.
        /// </summary>
        private void OnGamePhaseChangedServer(GamePhase previousValue, GamePhase newValue)
        {
            if (!IsServer) return;
            EvaluateTransformationServer(newValue);
        }

        private void EvaluateTransformationServer(GamePhase phase)
        {
            // Si la fase actual es Noche y el jugador tiene el rol de Hombre Lobo, se transforma en lobo
            bool isNightActive = (phase == GamePhase.Noche);
            bool isActuallyWolf = playerState != null ? playerState.isWolf.Value : isWerewolfRole;
            isWerewolfRole = isActuallyWolf; // Mantener local sincronizado
            IsWolfVisualActive.Value = isNightActive && isActuallyWolf;
            Debug.Log($"[Server Transformation] Evaluando jugador {gameObject.name}. Fase: {phase}. ¿Lobo activo?: {IsWolfVisualActive.Value}");
        }

        private void OnVisualStateChanged(bool previousValue, bool newValue)
        {
            UpdateVisualModel(newValue);
        }

        private Transform FindChildRecursively(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name || child.name.StartsWith(name)) return child;
                Transform result = FindChildRecursively(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private Transform FindChildByKeywordRecursively(Transform parent, string keyword)
        {
            foreach (Transform child in parent)
            {
                if (child.name.ToLower().Contains(keyword.ToLower())) return child;
                Transform result = FindChildByKeywordRecursively(child, keyword);
                if (result != null) return result;
            }
            return null;
        }

        /// <summary>
        /// Destruye el modelo anterior e instancia el nuevo de forma local en cada cliente.
        /// </summary>
        private void UpdateVisualModel(bool isWolf)
        {
            // MODO 0: Si tenemos Renderers de malla asignados directamente (mismo esqueleto)
            if (villagerMeshRenderer != null && wolfMeshRenderer != null)
            {
                villagerMeshRenderer.enabled = !isWolf;
                wolfMeshRenderer.enabled = isWolf;
                
                // Como comparten el mismo esqueleto, el Animator raíz sigue siendo el mismo.
                Animator rootAnimator = GetComponent<Animator>();
                if (rootAnimator == null) rootAnimator = GetComponentInChildren<Animator>();
                if (rootAnimator != null)
                {
                    DesactivarCapaSentadoSiEsGameplay(rootAnimator);
                    OnAnimatorChanged?.Invoke(rootAnimator);
                    Debug.Log($"[Client Transformation] Malla alternada en el mismo esqueleto a {(isWolf ? "Lobo" : "Aldeano")} en {gameObject.name}.");
                }
                return;
            }

            // --- RESOLUCIÓN DINÁMICA DE HIJOS (Anti-Prefab Asset Bug) ---
            // Si las referencias están vacías o apuntan al asset del Prefab en vez del clon de la escena, las re-localizamos en caliente.
            if (villagerVisualObject == null || !villagerVisualObject.transform.IsChildOf(transform))
            {
                string originalName = villagerVisualObject != null ? villagerVisualObject.name : "SM_Chr_Peasant_Male_01";
                Transform found = FindChildRecursively(transform, originalName);
                if (found == null) found = FindChildByKeywordRecursively(transform, "Peasant");
                if (found == null) found = FindChildByKeywordRecursively(transform, "Chr_");
                
                if (found != null)
                {
                    villagerVisualObject = found.gameObject;
                    Debug.Log($"[PlayerTransformation] Autodetectado Aldeano local: {villagerVisualObject.name}");
                }
            }

            if (wolfVisualObject == null || !wolfVisualObject.transform.IsChildOf(transform))
            {
                string originalName = wolfVisualObject != null ? wolfVisualObject.name : "Loboplayer";
                Transform found = FindChildRecursively(transform, originalName);
                if (found == null) found = FindChildByKeywordRecursively(transform, "lobo");
                if (found == null) found = FindChildByKeywordRecursively(transform, "wolf");
                
                if (found != null)
                {
                    wolfVisualObject = found.gameObject;
                    Debug.Log($"[PlayerTransformation] Autodetectado Lobo local: {wolfVisualObject.name}");
                }
            }

            // MODO 1: Si existen los objetos en la jerarquía, alternamos su visibilidad individualmente
            if (villagerVisualObject != null || wolfVisualObject != null)
            {
                if (villagerVisualObject != null)
                {
                    villagerVisualObject.SetActive(!isWolf);
                    Debug.Log($"[PlayerTransformation] Aldeano local ({villagerVisualObject.name}) SetActive={!isWolf}");
                }
                if (wolfVisualObject != null)
                {
                    wolfVisualObject.SetActive(isWolf);
                    Debug.Log($"[PlayerTransformation] Lobo local ({wolfVisualObject.name}) SetActive={isWolf}");
                }

                GameObject activeObj = isWolf ? wolfVisualObject : villagerVisualObject;
                if (activeObj != null)
                {
                    // Forzar posición y rotación local a cero para evitar que queden flotando o desplazados
                    activeObj.transform.localPosition = Vector3.zero;
                    activeObj.transform.localRotation = Quaternion.identity;

                    Animator activeAnimator = activeObj.GetComponentInChildren<Animator>();
                    if (activeAnimator == null)
                    {
                        // Fallback: si no está en el objeto de visual (ej. aldeano usa el del padre), usar el de la raíz
                        activeAnimator = GetComponent<Animator>();
                    }

                    if (activeAnimator != null)
                    {
                        DesactivarCapaSentadoSiEsGameplay(activeAnimator);
                        OnAnimatorChanged?.Invoke(activeAnimator);
                        Debug.Log($"[Client Transformation] Visual cambiado en jerarquía a {(isWolf ? "Lobo" : "Aldeano")} en {gameObject.name}. Animator re-enlazado: {activeAnimator.gameObject.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[PlayerTransformation] No se encontró Animator en el objeto activo ni en la raíz para {gameObject.name}.");
                    }
                }
                return;
            }

            // MODO 2: Si no están en la jerarquía, instanciamos los prefabs
            // 1. Limpieza segura del modelo anterior (Regla 24: Garbage Collection)
            if (currentVisualInstance != null)
            {
                Destroy(currentVisualInstance);
            }

            // Determinar prefab a instanciar
            GameObject prefabToSpawn = isWolf ? wolfVisualPrefab : villagerVisualPrefab;
            Transform parent = visualContainer != null ? visualContainer : transform;

            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"[PlayerTransformation] El prefab para {(isWolf ? "Lobo" : "Aldeano")} no está asignado en {gameObject.name}.");
                return;
            }

            // 2. Instanciación del nuevo modelo
            currentVisualInstance = Instantiate(prefabToSpawn, parent);
            currentVisualInstance.transform.localPosition = Vector3.zero;
            currentVisualInstance.transform.localRotation = Quaternion.identity;

            // 3. Obtener el nuevo Animator y notificar a los componentes interesados
            Animator newAnimator = currentVisualInstance.GetComponentInChildren<Animator>();
            if (newAnimator != null)
            {
                DesactivarCapaSentadoSiEsGameplay(newAnimator);
                OnAnimatorChanged?.Invoke(newAnimator);
                Debug.Log($"[Client Transformation] Visual de {(isWolf ? "Lobo" : "Aldeano")} instanciado en {gameObject.name}. Animator re-enlazado.");
            }
            else
            {
                Debug.LogWarning($"[PlayerTransformation] No se encontró Animator en el modelo instanciado para {gameObject.name}.");
            }
        }
 
        /// <summary>
        /// Si estamos en gameplay, fuerza a 0 el peso de la capa de sentado para que el lobo o aldeano
        /// transformado pueda andar/correr correctamente.
        /// </summary>
        private void DesactivarCapaSentadoSiEsGameplay(Animator anim)
        {
            if (anim == null) return;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Scene_Menu")
            {
                int lobbyLayerIdx = anim.GetLayerIndex("Lobby Layer");
                if (lobbyLayerIdx != -1)
                {
                    anim.SetLayerWeight(lobbyLayerIdx, 0f);
                    Debug.Log($"[PlayerTransformation] {name}: Peso de la capa 'Lobby Layer' en Animator '{anim.gameObject.name}' forzado a 0f fuera de la escena del Lobby.");
                }
            }
        }
    }
}
