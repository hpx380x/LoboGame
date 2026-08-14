using UnityEngine;

namespace Core.QuestSystem
{
    /// <summary>
    /// Gestiona la visibilidad de TODAS las herramientas del jugador que viven
    /// dentro de la jerarquía de huesos (mano/armadura).
    /// CLIENT-SIDE: Solo controla visualización local. Sin lógica de red.
    /// </summary>
    public class PlayerToolVisuals : MonoBehaviour
    {
        [Header("Herramientas (Asignación Manual o Automática)")]
        [Tooltip("Si dejas esto vacío, el script intentará buscar un hijo con el nombre exacto en el Awake.")]
        [SerializeField] private GameObject axeObject;            // "axe"
        [SerializeField] private GameObject escobaObject;         // "escoba"
        [SerializeField] private GameObject regaderaObject;       // "regadera"
        [SerializeField] private GameObject dagaObject;           // "Daga"
        [SerializeField] private GameObject pocionObject;         // "Pocion"
        [SerializeField] private GameObject pocionVelocidadObject;// "PocionVelocidad"
        [SerializeField] private GameObject antorchaObject;       // "Antorcha"
        [SerializeField] private GameObject manzanaOroObject;     // "ManzanaDeOro"
        [SerializeField] private GameObject bombaApestosaObject;  // "BombaApestosa"
        [SerializeField] private GameObject lupaObject;           // "Lupa"
        [SerializeField] private GameObject cajaObject;           // "Caja"
        [SerializeField] private GameObject pickaxeObject;        // "pickaxe"

        private void Awake()
        {
            // [Fix] Si la referencia asignada en el inspector no es un hijo directo de este clon en la escena
            // (por ejemplo, si es un prefab de proyecto o un objeto externo), la limpiamos para forzar la búsqueda local.
            LimpiarReferenciaExterna(ref axeObject, "axe");
            LimpiarReferenciaExterna(ref escobaObject, "escoba");
            LimpiarReferenciaExterna(ref regaderaObject, "regadera");
            LimpiarReferenciaExterna(ref dagaObject, "Daga");
            LimpiarReferenciaExterna(ref pocionObject, "Pocion");
            LimpiarReferenciaExterna(ref pocionVelocidadObject, "PocionVelocidad");
            LimpiarReferenciaExterna(ref antorchaObject, "Antorcha");
            LimpiarReferenciaExterna(ref manzanaOroObject, "ManzanaDeOro");
            LimpiarReferenciaExterna(ref bombaApestosaObject, "BombaApestosa");
            LimpiarReferenciaExterna(ref lupaObject, "Lupa");
            LimpiarReferenciaExterna(ref cajaObject, "Caja");
            LimpiarReferenciaExterna(ref pickaxeObject, "pickaxe");

            // Buscar cada herramienta por su nombre exacto en la jerarquía si no fue asignada manualmente
            if (axeObject == null) axeObject = FindChildByName(transform, "axe");
            if (escobaObject == null) escobaObject = FindChildByName(transform, "escoba");
            if (regaderaObject == null) regaderaObject = FindChildByName(transform, "regadera");
            if (dagaObject == null) dagaObject = FindChildByName(transform, "Daga");
            if (pocionObject == null) pocionObject = FindChildByName(transform, "Pocion");
            if (pocionVelocidadObject == null) pocionVelocidadObject = FindChildByName(transform, "PocionVelocidad");
            if (antorchaObject == null) antorchaObject = FindChildByName(transform, "Antorcha");
            if (manzanaOroObject == null) manzanaOroObject = FindChildByName(transform, "ManzanaDeOro");
            if (bombaApestosaObject == null) bombaApestosaObject = FindChildByName(transform, "BombaApestosa");
            if (lupaObject == null) lupaObject = FindChildByName(transform, "Lupa");
            if (cajaObject == null) cajaObject = FindChildByName(transform, "Caja");
            if (pickaxeObject == null) pickaxeObject = FindChildByName(transform, "pickaxe");

            // --- AUTO-ANCLAJE AL HUESO REAL ANIMADO ---
            Animator anim = GetComponentInChildren<Animator>();
            Transform manoDerecha = null;
            if (anim != null)
            {
                try { manoDerecha = anim.GetBoneTransform(HumanBodyBones.RightHand); }
                catch { /* Ignorar si el avatar no es humanoide */ }
            }
            
            // Fallback si no es humanoide
            if (manoDerecha == null) 
            {
                GameObject manoObj = FindChildByName(transform, "RightHand");
                manoDerecha = manoObj != null ? manoObj.transform : null;
            }
            
            if (manoDerecha != null)
            {
                Anclar(axeObject, manoDerecha);
                Anclar(escobaObject, manoDerecha);
                Anclar(regaderaObject, manoDerecha);
                Anclar(dagaObject, manoDerecha);
                Anclar(pocionObject, manoDerecha);
                Anclar(pocionVelocidadObject, manoDerecha);
                Anclar(antorchaObject, manoDerecha);
                Anclar(manzanaOroObject, manoDerecha);
                Anclar(bombaApestosaObject, manoDerecha);
                Anclar(lupaObject, manoDerecha);
                Anclar(cajaObject, manoDerecha);
                Anclar(pickaxeObject, manoDerecha);
            }

            // Desactivar colisionadores de todas las herramientas para no bugear la cámara de Cinemachine
            DesactivarColisionadores(axeObject);
            DesactivarColisionadores(escobaObject);
            DesactivarColisionadores(regaderaObject);
            DesactivarColisionadores(dagaObject);
            DesactivarColisionadores(pocionObject);
            DesactivarColisionadores(pocionVelocidadObject);
            DesactivarColisionadores(antorchaObject);
            DesactivarColisionadores(manzanaOroObject);
            DesactivarColisionadores(bombaApestosaObject);
            DesactivarColisionadores(lupaObject);
            DesactivarColisionadores(cajaObject);
            DesactivarColisionadores(pickaxeObject);

            // Ocultar TODAS al inicio — se activan solo durante la animación correcta
            HideAll();
        }

        private void LimpiarReferenciaExterna(ref GameObject obj, string nombreHerramienta)
        {
            if (obj == null) return;
            if (!obj.transform.IsChildOf(transform) || !obj.scene.IsValid())
            {
                Debug.Log($"[PlayerToolVisuals] La referencia '{obj.name}' para '{nombreHerramienta}' no pertenece a este clon del jugador (es un prefab del proyecto o externo). Limpiando para buscarlo localmente en los hijos.");
                obj = null;
            }
        }

        private void DesactivarColisionadores(GameObject obj)
        {
            if (obj == null) return;
            Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in colliders)
            {
                Destroy(col);
            }
            CambiarCapaRecursiva(obj, 2); // Capa 2 es Ignore Raycast
        }

        private void CambiarCapaRecursiva(GameObject obj, int capa)
        {
            if (obj == null) return;
            obj.layer = capa;
            foreach (Transform child in obj.transform)
            {
                CambiarCapaRecursiva(child.gameObject, capa);
            }
        }

        private void Anclar(GameObject obj, Transform parent)
        {
            if (obj != null && parent != null && obj.transform.parent != parent)
            {
                obj.transform.SetParent(parent, false);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
            }
        }

        // ─────────────────────── API PÚBLICA ────────────────────────────────────

        /// <summary>ID 2 — Cortar madera con el hacha.</summary>
        public void SetAxeActive(bool active)      => SetTool(axeObject,            "axe",             active);

        /// <summary>ID 4 — Barrer el altar con la escoba.</summary>
        public void SetEscobaActive(bool active)   => SetTool(escobaObject,         "escoba",          active);

        /// <summary>ID 6 — Regar el huerto con la regadera.</summary>
        public void SetRegaderaActive(bool active)
        {
            if (regaderaObject == null)
            {
                Debug.LogError("[ToolVisuals ERROR] El GameObject para la herramienta 'regadera' NO está asignado en el Inspector de PlayerToolVisuals!");
                return;
            }

            SetTool(regaderaObject, "regadera", active);
            
            // Buscar el script de partículas en el interior de la regadera y sincronizarlo
            if (regaderaObject != null)
            {
                Debug.Log($"[ToolVisuals] Buscando PlayerWateringParticles dentro de {regaderaObject.name}...");
                var wp = regaderaObject.GetComponent<PlayerWateringParticles>();
                if (wp != null)
                {
                    Debug.Log("[ToolVisuals] ¡Componente de partículas encontrado con éxito! Ejecutando Play().");
                    if (active) wp.PlayWaterParticles();
                    else wp.StopWaterParticles();
                }
                else if (active)
                {
                    Debug.LogWarning("[ToolVisuals ALERT] La regadera se encendió, pero NO se encontró el script PlayerWateringParticles en sus hijos.");
                }
            }
        }

        /// <summary>ID 7 — Apuñalar con la daga.</summary>
        public void SetDagaActive(bool active)     => SetTool(dagaObject,           "Daga",            active);

        /// <summary>ID 6 — Beber la poción.</summary>
        public void SetPocionActive(bool active)   => SetTool(pocionObject,         "Pocion",          active);

        // --- Herramientas extra (para uso desde otros sistemas, ej. PlayerInventory) ---
        public void SetPocionVelocidadActive(bool active) => SetTool(pocionVelocidadObject, "PocionVelocidad", active);
        public void SetAntorchaActive(bool active)        => SetTool(antorchaObject,        "Antorcha",        active);
        public void SetManzanaOroActive(bool active)      => SetTool(manzanaOroObject,      "ManzanaDeOro",    active);
        public void SetBombaApestosaActive(bool active)   => SetTool(bombaApestosaObject,   "BombaApestosa",   active);
        public void SetLupaActive(bool active)            => SetTool(lupaObject,            "Lupa",            active);
        public void SetCajaActive(bool active)            => SetTool(cajaObject,            "Caja",            active);
        public void SetPickaxeActive(bool active)         => SetTool(pickaxeObject,         "pickaxe",         active);

        /// <summary>Llamada dinámica para el sistema Universal Quest mediante strings</summary>
        public void SetToolByName(string toolName, bool active)
        {
            if (string.IsNullOrEmpty(toolName))
            {
                Debug.LogWarning($"[PlayerToolVisuals] Se solicitó una herramienta con un string vacío o nulo.");
                return;
            }

            // Normalizamos el string a minúsculas para evitar problemas de "Regadera" vs "regadera"
            string nombreNormalizado = toolName.Trim().ToLower();
            Debug.Log($"[PlayerToolVisuals] Procesando solicitud directa para: '{nombreNormalizado}' -> Estado: {active}");

            switch (nombreNormalizado)
            {
                case "axe":
                case "hacha":
                    SetAxeActive(active);
                    break;

                case "pickaxe":
                case "pico":
                    SetPickaxeActive(active);
                    break;

                case "escoba":
                    SetEscobaActive(active);
                    break;

                case "regadera":
                case "wateringcan":
                case "baldeagua":
                    SetRegaderaActive(active);
                    break;

                case "daga":
                    SetDagaActive(active);
                    break;

                case "pocion":
                    SetPocionActive(active);
                    break;

                case "pocionvelocidad":
                    SetPocionVelocidadActive(active);
                    break;

                case "antorcha":
                    SetAntorchaActive(active);
                    break;

                case "manzanadeoro":
                    SetManzanaOroActive(active);
                    break;

                case "bombaapestosa":
                    SetBombaApestosaActive(active);
                    break;

                case "lupa":
                    SetLupaActive(active);
                    break;
                    
                case "caja":
                case "box":
                    SetCajaActive(active);
                    break;

                default:
                    Debug.LogError($"[PlayerToolVisuals ERROR] El string '{toolName}' no coincide con ninguna herramienta registrada en el switch.");
                    break;
            }
        }

        public bool IsToolActive(string toolName)
        {
            if (string.IsNullOrEmpty(toolName)) return false;
            string nombreNormalizado = toolName.Trim().ToLower();
            
            switch (nombreNormalizado)
            {
                case "axe":
                case "hacha":
                    return axeObject != null && axeObject.activeSelf;
                case "pickaxe":
                case "pico":
                    return pickaxeObject != null && pickaxeObject.activeSelf;
                case "escoba":
                    return escobaObject != null && escobaObject.activeSelf;
                case "regadera":
                case "wateringcan":
                case "baldeagua":
                    return regaderaObject != null && regaderaObject.activeSelf;
                case "daga":
                    return dagaObject != null && dagaObject.activeSelf;
                case "pocion":
                    return pocionObject != null && pocionObject.activeSelf;
                case "pocionvelocidad":
                    return pocionVelocidadObject != null && pocionVelocidadObject.activeSelf;
                case "antorcha":
                    return antorchaObject != null && antorchaObject.activeSelf;
                case "manzanadeoro":
                    return manzanaOroObject != null && manzanaOroObject.activeSelf;
                case "bombaapestosa":
                    return bombaApestosaObject != null && bombaApestosaObject.activeSelf;
                case "lupa":
                    return lupaObject != null && lupaObject.activeSelf;
                case "caja":
                case "box":
                    return cajaObject != null && cajaObject.activeSelf;
            }
            return false;
        }

        /// <summary>Oculta absolutamente todas las herramientas de una vez.</summary>
        public void HideAll()
        {
            SetToolSilent(axeObject);
            SetToolSilent(escobaObject);
            SetToolSilent(regaderaObject);
            SetToolSilent(dagaObject);
            SetToolSilent(pocionObject);
            SetToolSilent(pocionVelocidadObject);
            SetToolSilent(antorchaObject);
            SetToolSilent(manzanaOroObject);
            SetToolSilent(bombaApestosaObject);
            SetToolSilent(lupaObject);
            SetToolSilent(cajaObject);
            SetToolSilent(pickaxeObject);
        }

        // ─────────────────────── PRIVADOS ────────────────────────────────────────

        private void SetTool(GameObject tool, string nombre, bool active)
        {
            if (tool == null)
            {
                if (active) Debug.LogWarning($"[PlayerToolVisuals] No se encontró '{nombre}' en {gameObject.name}");
                return;
            }

            // ✅ FIX: Solo cambiar estado si realmente cambia, y solo reiniciar partículas
            // si pasamos de inactivo a activo (no si ya estaba activo)
            bool yaEstabaActivo = tool.activeSelf;
            tool.SetActive(active);

            if (active && !yaEstabaActivo)
            {
                ParticleSystem[] pss = tool.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in pss)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Play(true);
                }
            }

            Debug.Log($"[PlayerToolVisuals] {nombre} → {(active ? "VISIBLE" : "OCULTO")}");
        }

        // Sin log — usado solo para ocultar en masa al inicio
        private static void SetToolSilent(GameObject tool)
        {
            if (tool != null) tool.SetActive(false);
        }

        /// <summary>Búsqueda recursiva exacta por nombre (case-insensitive).</summary>
        private GameObject FindChildByName(Transform parent, string targetName)
        {
            foreach (Transform child in parent)
            {
                string childNameNorm = child.name.Trim().ToLower();
                string targetNameNorm = targetName.Trim().ToLower();

                if (childNameNorm.Contains(targetNameNorm))
                    return child.gameObject;

                GameObject result = FindChildByName(child, targetName);
                if (result != null) return result;
            }
            return null;
        }
    }
}
