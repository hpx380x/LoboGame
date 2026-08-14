using UnityEngine;
using StarterAssets;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace Core.Environment
{
    /// <summary>
    /// Componente que se coloca en el comerciante o puesto de mercado para abrir la tienda de pergaminos.
    /// </summary>
    public class MerchantInteract : MonoBehaviour
    {
        [Header("Configuración del Letrero Flotante")]
        [Tooltip("Texto a mostrar encima del comerciante (ej: TIENDA, MERCADER)")]
        public string textoTienda = "TIENDA";
        
        [Tooltip("Desplazamiento/Posición del letrero respecto al objeto (X, Y, Z)")]
        public Vector3 offsetHUD = new Vector3(0f, 2.5f, 0f);
        
        [Tooltip("Tamaño de la fuente del texto")]
        public float tamanoTexto = 50f;

        [Tooltip("Escala 3D del letrero flotante")]
        public Vector3 escalaHUD = new Vector3(0.015f, 0.015f, 0.015f);

        private bool jugadorCerca = false;
        private PlayerState psLocal;
        private Collider miCollider;
        private Transform hudTransform;

        // ── Caché: se resuelven una sola vez en Start ─────────────────
        private Camera mainCam;
        private GameplayUI cachedUI;
        private Core.UI.ShopPanelController cachedShopPanel;

        private void Awake()
        {
            miCollider = GetComponent<Collider>();
            if (miCollider == null)
            {
                SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = 3.0f;
                miCollider = sphere;
            }
            else
            {
                miCollider.isTrigger = true;
            }
        }

        private void Start()
        {
            // Cachear una sola vez al arrancar la escena
            mainCam         = Camera.main;
            cachedUI        = Object.FindAnyObjectByType<GameplayUI>();
            cachedShopPanel = Object.FindAnyObjectByType<Core.UI.ShopPanelController>(FindObjectsInactive.Include);

            // Crear Canvas WorldSpace flotante
            GameObject hudObj = new GameObject("HUD_Tienda", typeof(RectTransform), typeof(Canvas));
            hudObj.transform.SetParent(this.transform);
            hudObj.transform.localPosition = offsetHUD;
            
            Canvas canvas = hudObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            RectTransform rt = hudObj.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(250, 60);
            rt.localScale = escalaHUD;
            
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            textObj.transform.SetParent(hudObj.transform, false);
            
            TMPro.TextMeshProUGUI tmp = textObj.GetComponent<TMPro.TextMeshProUGUI>();
            tmp.text         = textoTienda;
            tmp.fontSize     = tamanoTexto;
            tmp.fontStyle    = TMPro.FontStyles.Bold;
            tmp.color        = new Color(1f, 0.85f, 0.2f, 1f);
            tmp.alignment    = TMPro.TextAlignmentOptions.Center;
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = Color.black;

            hudTransform = hudObj.transform;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.isTrigger) return;

            NetworkObject netObj = other.GetComponent<NetworkObject>() ?? other.GetComponentInParent<NetworkObject>();
            if (netObj == null || !netObj.IsOwner) return;

            psLocal = netObj.GetComponent<PlayerState>();
            if (psLocal == null || psLocal.isDead.Value) return;

            jugadorCerca = true;

            if (cachedUI != null)
                cachedUI.MostrarMensajeTarea("[E] Hablar con Comerciante", 0f);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.isTrigger) return;

            NetworkObject netObj = other.GetComponent<NetworkObject>() ?? other.GetComponentInParent<NetworkObject>();
            if (netObj == null || !netObj.IsOwner) return;

            jugadorCerca = false;
            psLocal      = null;

            if (cachedUI != null)
                cachedUI.MostrarMensajeTarea("", 0f);

            if (cachedShopPanel != null && cachedShopPanel.PanelActivo)
                cachedShopPanel.CerrarTienda();
        }

        private void Update()
        {
            // Billboard: el letrero siempre mira a la cámara (sin FindWithTag)
            if (hudTransform != null && mainCam != null)
                hudTransform.rotation = mainCam.transform.rotation;

            if (!jugadorCerca || psLocal == null) return;
            if (Keyboard.current == null) return;

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (cachedShopPanel != null)
                {
                    if (!cachedShopPanel.PanelActivo)
                    {
                        cachedShopPanel.AbrirTienda(psLocal.gameObject);
                        if (cachedUI != null) cachedUI.MostrarMensajeTarea("", 0f);
                    }
                    else
                    {
                        cachedShopPanel.CerrarTienda();
                    }
                }
                else
                {
                    Debug.LogWarning("[MerchantInteract] No se encontró el panel de la tienda (ShopPanelController).");
                }
            }
        }
    }
}
