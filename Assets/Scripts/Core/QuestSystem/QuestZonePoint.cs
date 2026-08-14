using UnityEngine;
using Core.Enums;
using Core.Environment;

namespace Core.QuestSystem
{
    /// <summary>
    /// Punto de interacción de misión básica colocado manualmente en la escena.
    /// El usuario añade este componente a un GameObject vacío, elige el tipo de misión
    /// en el Inspector y coloca el objeto donde quiera que el jugador interactúe.
    ///
    /// ─────────────────────────────────────────────────────────────────
    ///  CÓMO USARLO:
    ///  1. Crear un GameObject vacío en la escena (Ctrl+Shift+N).
    ///  2. Añadirle este componente (QuestZonePoint).
    ///  3. Seleccionar "Tipo Mision" en el Inspector.
    ///  4. Ajustar "Zona" si hace falta (viene preconfigurada por defecto).
    ///  5. Ajustar el radio del trigger si quieres mayor o menor área.
    ///  Las esferas de colores en el editor muestran el radio de detección.
    /// ─────────────────────────────────────────────────────────────────
    ///
    /// [CLIENT-SIDE] No es un NetworkBehaviour. Solo controla visual e interacción local.
    /// </summary>
    public class QuestZonePoint : MonoBehaviour
    {
        // ─────────────────────────── TIPOS DISPONIBLES ────────────────────────────
        public enum TipoMisionBasica
        {
            Limpiar,    // Escoba    — Barrer el Altar
            Regar,      // Regadera  — Regar el Huerto
            Talar,      // Hacha     — Cortar Leña
            Velas,      // Velas     — Encender Velas
            Apunalar,   // Daga      — Apuñalar
            Beber,      // Poción    — Beber Poción
            Afilar,     // Afilador  — Afilar la hoja
            Recoger,    // Recoger   — Recoger agua/material
            Entregar    // Entregar  — Entregar items/misión
        }

        // ─────────────────────────── INSPECTOR ────────────────────────────────────
        [Header("Configuración de Misión")]
        [Tooltip("Tipo de misión que se puede realizar en este punto")]
        public TipoMisionBasica tipoMision = TipoMisionBasica.Limpiar;

        [Tooltip("Zona del mapa donde está este punto (afecta a la validación de misión)")]
        public ZoneID zonaUbicacion = ZoneID.Altar;

        [Tooltip("Radio de detección en metros (el jugador debe entrar en este radio para ver el prompt)")]
        public float radioDeteccion = 2f;

        [Tooltip("Texto que aparece en el HUD cuando el jugador está cerca")]
        public string textoHUD = "";  // Si queda vacío, se usa el texto por defecto del tipo

        [Tooltip("Si es distinto de Ninguno, sobrescribe el material asignado a este punto (ej: EsenciaAntigua)")]
        public MaterialType materialSobreescribir = MaterialType.Ninguno;

        [Tooltip("Si es mayor que 0, sobrescribe la animación de interacción por defecto de este punto en el escenario (ej: 12 para entregar)")]
        public int animacionSobreescribir = 0;

        [Header("HUD Flotante 3D (Opcional)")]
        [Tooltip("Objeto 3D padre del texto flotante (ej: HUD_Texto / Canvas WorldSpace)")]
        public GameObject hudObject;
        [Tooltip("Componente TextMeshPro 3D con el texto flotante")]
        public TMPro.TextMeshPro hudText;

        [Header("Efectos Visuales")]
        [Tooltip("Partículas o luz de la estatua que se encenderán cuando el jugador entre en este punto/zona.")]
        public GameObject efectoEstatuaAlEstarEnZona;

        // ─────────────────────────── COLORES GIZMO ────────────────────────────────
        // Cada tipo tiene su propio color en el editor para identificarlos a simple vista
        private static readonly Color ColorLimpiar  = new Color(0.4f, 0.8f, 1.0f, 0.35f); // Azul claro
        private static readonly Color ColorRegar    = new Color(0.2f, 0.9f, 0.3f, 0.35f); // Verde
        private static readonly Color ColorTalar    = new Color(0.6f, 0.4f, 0.1f, 0.35f); // Marrón
        private static readonly Color ColorVelas    = new Color(1.0f, 0.8f, 0.1f, 0.35f); // Amarillo
        private static readonly Color ColorApunalar = new Color(0.9f, 0.2f, 0.2f, 0.35f); // Rojo
        private static readonly Color ColorBeber    = new Color(0.7f, 0.3f, 1.0f, 0.35f); // Morado
        private static readonly Color ColorAfilar   = new Color(0.1f, 0.8f, 0.8f, 0.35f); // Cian

        // ─────────────────────────── RUNTIME ──────────────────────────────────────
        private void Awake()
        {
            // Asegurar que el colisionador del objeto sea un trigger (conservando su forma de caja u otra)
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            // Obtener los datos según el tipo seleccionado
            (MaterialType matDefault, int animID, string textoDefault) = ObtenerDatosTipo(tipoMision);
            string textoFinal = string.IsNullOrEmpty(textoHUD) ? textoDefault : textoHUD;

            // Añadir UniversalQuestInteractable (o usar el que ya existe) y configurarlo
            var interactable = GetComponent<UniversalQuestInteractable>();
            if (interactable == null)
            {
                interactable = gameObject.AddComponent<UniversalQuestInteractable>();
            }

            // Determinar el material final: respetar el asignado previamente en el inspector o el de sobreescritura
            MaterialType matFinal = matDefault;
            if (materialSobreescribir != MaterialType.Ninguno)
            {
                matFinal = materialSobreescribir;
            }
            else if (interactable.materialAsignado != MaterialType.Ninguno)
            {
                matFinal = interactable.materialAsignado;
            }

            interactable.materialAsignado         = matFinal;
            interactable.zonaUbicacion            = zonaUbicacion;
            interactable.customUIMessage          = textoFinal;
            interactable.cantidadMaterialOtorgado = 1;
            interactable.tipoAnimacionRecogida    = animacionSobreescribir > 0 ? animacionSobreescribir : animID;

            // Configurar mecánica y número de pulsaciones automáticamente
            if (tipoMision == TipoMisionBasica.Talar)
            {
                interactable.mecanica = MecanicaInteraccion.AporrearBoton;
                interactable.pulsacionesRequeridas = 4;
                interactable.ocultarModeloAlCompletar = true; // El tronco desaparece de forma automática al completarse
            }
            else if (tipoMision == TipoMisionBasica.Afilar)
            {
                interactable.mecanica = MecanicaInteraccion.Timing;
                interactable.pulsacionesRequeridas = 2; // Requiere 2 aciertos de timing
            }
            else if (tipoMision == TipoMisionBasica.Recoger)
            {
                interactable.mecanica = MecanicaInteraccion.Transportar;
                interactable.pulsacionesRequeridas = 1;
                if (string.IsNullOrEmpty(interactable.toolVisualNameLegacy))
                {
                    interactable.toolVisualNameLegacy = "bucket"; // Prefab del balde de agua por defecto
                }
            }
            else
            {
                interactable.mecanica = MecanicaInteraccion.MantenerPulsado;
            }

            // Si el material recolectado es Hierba Sombria (plantas de la bruja), ocultar la planta al completarse
            if (matFinal == MaterialType.HierbaSombria)
            {
                interactable.ocultarModeloAlCompletar = true;
            }
            
            // Retrocompatibilidad con herramientas
            if (matFinal == MaterialType.Leña) interactable.toolVisualNameLegacy = "axe";

            interactable.nombreVisualManual = interactable.toolVisualNameLegacy;
            interactable.efectoEstatuaAlEstarEnZona = efectoEstatuaAlEstarEnZona;
            if (hudObject != null) interactable.hudObject = hudObject;
            if (hudText != null) interactable.hudText = hudText;

            interactable.EnsureHUDCreated();
        }

        private void Start()
        {
            // Configurar radio de detección a 10 metros por defecto
            SphereCollider sphere = GetComponent<SphereCollider>();
            if (sphere != null)
            {
                sphere.isTrigger = true;
                sphere.radius = Mathf.Max(radioDeteccion, 10f);
            }
            else
            {
                Collider col = GetComponent<Collider>();
                if (col != null)
                {
                    col.isTrigger = true;
                }
            }
        }

        // ─────────────────────────── GIZMOS EDITOR ────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Color c = ObtenerColorGizmo(tipoMision);
            Gizmos.color = c;
            Gizmos.DrawSphere(transform.position, radioDeteccion);

            // Borde sólido
            Color borde = c;
            borde.a = 1f;
            Gizmos.color = borde;
            Gizmos.DrawWireSphere(transform.position, radioDeteccion);
        }

        private void OnDrawGizmosSelected()
        {
            // Al seleccionar: mostrar también el texto del tipo centrado
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * (radioDeteccion + 0.3f),
                $"[{tipoMision}] — {zonaUbicacion}\nr={radioDeteccion}m"
            );
        }

        [ContextMenu("Generar HUD 3D en la Escena")]
        public void GenerarHUD3DEnEditor()
        {
            // Buscar si ya tiene un hijo llamado HUD_Texto
            Transform existingChild = transform.Find("HUD_Texto");
            GameObject hudObj;
            if (existingChild != null)
            {
                hudObj = existingChild.gameObject;
            }
            else
            {
                hudObj = new GameObject("HUD_Texto", typeof(TMPro.TextMeshPro));
                hudObj.transform.SetParent(this.transform, false);
                hudObj.transform.localPosition = new Vector3(0f, 2.0f, 0f);
                UnityEditor.Undo.RegisterCreatedObjectUndo(hudObj, "Generar HUD 3D");
            }

            hudText = hudObj.GetComponent<TMPro.TextMeshPro>();
            hudText.rectTransform.sizeDelta = new Vector2(8f, 2.5f);
            hudText.fontSize = 4f;
            hudText.alignment = TMPro.TextAlignmentOptions.Center;
            hudText.textWrappingMode = TMPro.TextWrappingModes.Normal;
            hudText.color = new Color(1f, 0.9f, 0.2f, 1f); // Dorado/Amarillo

            (MaterialType matDefault, int animID, string textoDefault) = ObtenerDatosTipo(tipoMision);
            string textoFinal = !string.IsNullOrEmpty(textoHUD) ? textoHUD : textoDefault;
            hudText.text = textoFinal;

            hudObject = hudObj;

            // También actualizar en UniversalQuestInteractable si existe
            var interactable = GetComponent<UniversalQuestInteractable>();
            if (interactable != null)
            {
                interactable.hudObject = hudObject;
                interactable.hudText = hudText;
                interactable.customUIMessage = textoFinal;
                UnityEditor.EditorUtility.SetDirty(interactable);
            }

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[QuestZonePoint] ¡HUD 3D generado y asignado con éxito en '{gameObject.name}'!");
        }
#endif

        // ─────────────────────────── HELPERS PRIVADOS ─────────────────────────────
        private static (MaterialType mat, int animID, string texto) ObtenerDatosTipo(TipoMisionBasica tipo)
        {
            return tipo switch
            {
                TipoMisionBasica.Limpiar   => (MaterialType.Escoba,    2, "[E] Barrer"),
                TipoMisionBasica.Regar     => (MaterialType.BaldeAgua,  8, "[E] Regar"),
                TipoMisionBasica.Talar     => (MaterialType.Leña,       9, "[E] Cortar Leña"),
                TipoMisionBasica.Velas     => (MaterialType.Vela,       1, "[E] Encender Velas"), // 1 es crouch pick (recoger_aga)
                TipoMisionBasica.Apunalar  => (MaterialType.Ninguno,    6, "[E] Apuñalar"),
                TipoMisionBasica.Beber     => (MaterialType.Frasco,     7, "[E] Beber Poción"),
                TipoMisionBasica.Afilar    => (MaterialType.Afilado,    3, "[E] Afilar Hoja"),
                TipoMisionBasica.Recoger   => (MaterialType.BaldeAgua,  1, "[E] Recoger Agua"),
                TipoMisionBasica.Entregar  => (MaterialType.Ninguno,    1, "[E] Entregar"),
                _                          => (MaterialType.Ninguno,    3, "[E] Interactuar"),
            };
        }

        private static Color ObtenerColorGizmo(TipoMisionBasica tipo)
        {
            return tipo switch
            {
                TipoMisionBasica.Limpiar   => ColorLimpiar,
                TipoMisionBasica.Regar     => ColorRegar,
                TipoMisionBasica.Talar     => ColorTalar,
                TipoMisionBasica.Velas     => ColorVelas,
                TipoMisionBasica.Apunalar  => ColorApunalar,
                TipoMisionBasica.Beber     => ColorBeber,
                TipoMisionBasica.Afilar    => ColorAfilar,
                TipoMisionBasica.Recoger   => Color.blue,
                _                          => Color.white,
            };
        }
    }
}
