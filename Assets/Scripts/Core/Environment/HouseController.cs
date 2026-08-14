using Unity.Netcode;
using UnityEngine;

namespace Core.Environment
{
    public class HouseController : NetworkBehaviour
    {
        [Header("Due\u00f1o")]
        [Tooltip("NetworkObjectId del jugador al que pertenece esta casa.")]
        public NetworkVariable<ulong> HouseOwnerClientId = new NetworkVariable<ulong>(ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        [Header("Seguridad Interna")]
        public NetworkVariable<bool> IsDoorLocked = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [Header("Referencias F\u00edsicas")]
        [Tooltip("Transform donde aparecer\u00e1 el jugador si amanece en la casa.")]
        public Transform bedSpawnPoint;
        [Tooltip("Collider o pared invisible que bloquea el paso cuando est\u00e1 cerrado.")]
        public Collider doorBlocker;

        [Header("Objetos Visuales de la Puerta (Opcionales)")]
        [Tooltip("El objeto de la puerta MODELADA CERRADA. Aparecer\u00e1 de noche.")]
        public GameObject puertaCerradaVisual;
        [Tooltip("El objeto de la puerta MODELADA ABIERTA. Aparecer\u00e1 de d\u00eda.")]
        public GameObject puertaAbiertaVisual;

        [Header("Indicador Visual")]
        [Tooltip("Objeto visual (ej: flecha flotante, luz, icono) que se encenderá solo para el dueño local de la casa.")]
        public GameObject indicadorMiCasa;

        [Header("HUD Flotante 3D")]
        [Tooltip("Texto flotante 3D (TextMeshPro) que aparecerá indicando que es tu casa.")]
        public TMPro.TextMeshPro hudTextoPropietario;
        [Tooltip("Desplazamiento/Posición del letrero 'Mi Casa' respecto a la casa o cama.")]
        public Vector3 offsetTextoHUD = new Vector3(0f, 3.8f, 0f);

        private void Awake()
        {
            cachedCamMain = Camera.main;
            EnsureHUDCreated();
        }

        // ── Caché de cámara para el efecto Billboard ──
        private Camera cachedCamMain;

        private void EnsureHUDCreated()
        {
            if (hudTextoPropietario == null)
            {
                Vector3 spawnPos = (bedSpawnPoint != null) ? bedSpawnPoint.position + offsetTextoHUD : transform.position + offsetTextoHUD;

                GameObject hudObj = new GameObject("HUD_MiCasa_Auto", typeof(TMPro.TextMeshPro));
                hudObj.transform.SetParent(this.transform);
                hudObj.transform.position = spawnPos;

                hudTextoPropietario = hudObj.GetComponent<TMPro.TextMeshPro>();
                hudTextoPropietario.text = "🏠 Mi Casa";
                hudTextoPropietario.fontSize = 5f;
                hudTextoPropietario.alignment = TMPro.TextAlignmentOptions.Center;
                hudTextoPropietario.color = new Color(0.2f, 0.9f, 1.0f); // Cyan brillante
                hudTextoPropietario.fontStyle = TMPro.FontStyles.Bold;
                
                hudObj.SetActive(false);
            }

            if (indicadorMiCasa == null && hudTextoPropietario != null)
            {
                indicadorMiCasa = hudTextoPropietario.gameObject;
            }
        }

        public override void OnNetworkSpawn()
        {
            EnsureHUDCreated();
            IsDoorLocked.OnValueChanged += OnDoorLockStateChanged;
            HouseOwnerClientId.OnValueChanged += OnOwnerChanged;

            UpdateDoorState(IsDoorLocked.Value);
            UpdateOwnerIndicator(HouseOwnerClientId.Value);
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.currentPhase.OnValueChanged += OnGamePhaseChanged;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.currentPhase.OnValueChanged -= OnGamePhaseChanged;
            }
        }

        private void OnGamePhaseChanged(GamePhase previous, GamePhase current)
        {
            UpdateOwnerIndicator(HouseOwnerClientId.Value);
        }

        public override void OnNetworkDespawn()
        {
            IsDoorLocked.OnValueChanged -= OnDoorLockStateChanged;
            HouseOwnerClientId.OnValueChanged -= OnOwnerChanged;
        }

        private void OnOwnerChanged(ulong previous, ulong current)
        {
            UpdateOwnerIndicator(current);
        }

        private void UpdateOwnerIndicator(ulong ownerId)
        {
            EnsureHUDCreated();
            bool esMiCasa = (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && ownerId == NetworkManager.Singleton.LocalClientId);

            if (indicadorMiCasa != null && (hudTextoPropietario == null || indicadorMiCasa != hudTextoPropietario.gameObject))
            {
                indicadorMiCasa.SetActive(esMiCasa);
            }

            if (hudTextoPropietario != null)
            {
                hudTextoPropietario.gameObject.SetActive(esMiCasa);
                if (esMiCasa)
                {
                    hudTextoPropietario.text = "🏠 Mi Casa";
                }
            }
        }

        private void Update()
        {
            // Billboard: el texto siempre mira a la cámara (sin FindWithTag cada frame)
            if (hudTextoPropietario != null && hudTextoPropietario.gameObject.activeSelf)
            {
                if (cachedCamMain == null) cachedCamMain = Camera.main;
                if (cachedCamMain != null) hudTextoPropietario.transform.rotation = cachedCamMain.transform.rotation;
            }
        }

        private void OnDoorLockStateChanged(bool previous, bool current)
        {
            UpdateDoorState(current);
        }

        private void UpdateDoorState(bool locked)
        {
            if (doorBlocker != null)
            {
                // Si la puerta est\u00e1 bloqueada, activamos el Collider para no dejar pasar a nadie.
                doorBlocker.gameObject.SetActive(locked);
            }

            // Gestionamos el cambio visual de los modelos
            if (puertaCerradaVisual != null) puertaCerradaVisual.SetActive(locked);
            if (puertaAbiertaVisual != null) puertaAbiertaVisual.SetActive(!locked);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Solo el servidor tiene la autoridad de cerrar puertas.
            if (!IsServer) return;

            GameManager gm = FindAnyObjectByType<GameManager>();
            if (gm == null || gm.currentPhase.Value != GamePhase.Noche) return;

            NetworkObject netObj = other.GetComponent<NetworkObject>();
            // Si el objeto que colisiona es un jugador y es el DUE\u00d1O de la casa
            if (netObj != null && netObj.OwnerClientId == HouseOwnerClientId.Value)
            {
                if (!IsDoorLocked.Value)
                {
                    Debug.Log($"[House {NetworkObjectId}] El due\u00f1o {HouseOwnerClientId.Value} ha entrado. Bloqueando la puerta de noche.");
                    IsDoorLocked.Value = true;
                }
            }
        }
    }
}

