using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

// ─── Cola global compartida entre TODOS los personajes del Lobby ───────────────
// Evita que dos personajes reproduzcan el mismo clip a la vez.
// [FIX #2] Añadido Reset() para limpiar el estado entre sesiones/escenas.
static class LobbyClipQueue
{
    public static readonly HashSet<AnimationClip> EnUso = new HashSet<AnimationClip>();
    public static bool Reservar(AnimationClip clip) => clip != null && EnUso.Add(clip);
    public static void Liberar(AnimationClip clip)  { if (clip != null) EnUso.Remove(clip); }

    // [FIX #2] Llamar al volver al Lobby para evitar clips "fantasma" de sesiones anteriores.
    public static void Reset() => EnUso.Clear();
}

/// <summary>
/// Animaciones aleatorias de sentado para el Lobby.
/// Usa un Blend Tree 1D (SitActionBlend 0=Idle, 1=Accion) + AnimatorOverrideController.
/// El intercambio de clip ocurre cuando blend=0 (invisible), evitando saltos visuales.
/// </summary>
[RequireComponent(typeof(Animator))]
public class RandomLobbySit : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Animaciones")]
    [Tooltip("El clip BASE de sentado (Ranura 0 del Blend Tree, threshold 0).")]
    [SerializeField] private AnimationClip baseSittingIdle;

    [Tooltip("El clip PLACEHOLDER de la Ranura 1 del Blend Tree (threshold 1). Será reemplazado en runtime.")]
    [SerializeField] private AnimationClip actionSlotPlaceholder;

    [Tooltip("Todos los clips de acción aleatorios. No dejar slots vacíos.")]
    [SerializeField] private List<AnimationClip> randomSittingClips = new List<AnimationClip>();

    [Header("Transición")]
    [Tooltip("Duración del fundido entre Idle y Acción (segundos).")]
    [SerializeField] [Range(0.1f, 2f)] private float smoothTransitionTime = 0.5f;

    [Tooltip("Tiempo mínimo de reposo en Idle antes de reproducir una acción.")]
    [SerializeField] private float minIdleTime = 3f;

    [Tooltip("Tiempo máximo de reposo en Idle antes de reproducir una acción.")]
    [SerializeField] private float maxIdleTime = 8f;

    [Tooltip("Desincronización inicial máxima entre personajes para que no actúen al unísono.")]
    [SerializeField] private float maxStartupDelay = 4f;

    // ─── Privadas ─────────────────────────────────────────────────────────────

    private Animator _animator;
    private AnimatorOverrideController _overrideController;
    private int _blendHash;

    // [FIX #7] Cacheamos el resultado de HasParameter para no hacer foreach en cada frame de lerp.
    private bool _hasBlendParam = false;

    private bool _initialized = false;
    private bool _ready = false;
    private AnimationClip _lastClip   = null;
    private AnimationClip _clipActivo = null;

    // [FIX #3] System.Random por instancia: no interfiere con el Random global de Unity.
    // Cada personaje tiene su propia semilla independiente y simultánea.
    private System.Random _rng;

    // ─── Lista de clips válidos (sin nulos) ───────────────────────────────────

    // [FIX #5] Se filtra una vez en Start para eliminar entradas nulas del Inspector.
    private List<AnimationClip> _clipsValidos = new List<AnimationClip>();

    // ─── Ciclo de vida ────────────────────────────────────────────────────────

    private void OnEnable()
    {
        // Al volver de SetActive(false), relanzamos la corrutina desde el principio.
        if (_initialized)
        {
            StopAllCoroutines();
            if (_animator != null) _animator.SetFloat(_blendHash, 0f);
            _ready = true;
            StartCoroutine(RandomSitRoutine());
        }
    }

    private void OnDisable()
    {
        // Liberamos el clip activo para que otros personajes puedan usarlo.
        LobbyClipQueue.Liberar(_clipActivo);
        _clipActivo = null;
        _ready = false;
    }

    private IEnumerator Start()
    {
        _animator = GetComponent<Animator>();

        // Solo activo en el Lobby. En Scene_Gameplay y otras escenas no hace nada.
        if (SceneManager.GetActiveScene().name != "Scene_Menu")
            yield break;

        // [FIX #2] Limpiamos la cola global al iniciar la primera instancia en el Lobby.
        // Esto garantiza que no queden clips "fantasma" de partidas anteriores.
        LobbyClipQueue.Reset();

        yield return null; // Frame de gracia para que el Animator arranque.

        // [FIX #5] Filtrar clips nulos del Inspector antes de usarlos.
        _clipsValidos.Clear();
        if (randomSittingClips != null)
        {
            foreach (var clip in randomSittingClips)
                if (clip != null) _clipsValidos.Add(clip);
        }

        if (_animator == null || _animator.runtimeAnimatorController == null
            || baseSittingIdle == null || actionSlotPlaceholder == null)
        {
            Debug.LogWarning($"[RandomSit] {gameObject.name}: Faltan referencias básicas en el Inspector " +
                             $"(Animator, baseSittingIdle o actionSlotPlaceholder).");
            yield break;
        }

        // Override Controller: permite cambiar clips en runtime sin tocar el AnimatorController.
        _overrideController = new AnimatorOverrideController(_animator.runtimeAnimatorController);
        _animator.runtimeAnimatorController = _overrideController;
        _animator.applyRootMotion = false;

        // Esperamos un frame para que el Animator procese el cambio de controlador
        yield return null;

        // [CORRECCIÓN CRÍTICA] Al asignar runtimeAnimatorController se resetean todos los parámetros y pesos del Animator.
        // Re-establecemos la pose de sentado y los pesos de capa inmediatamente para que el personaje se quede sentado.
        _animator.SetBool("isSitting", true);
        _animator.SetBool("Grounded", true);
        _animator.SetBool("FreeFall", false);
        _animator.SetFloat("Speed", 0f);

        int sitLayerIndex = _animator.GetLayerIndex("Lobby Layer");
        if (sitLayerIndex != -1)
        {
            _animator.SetLayerWeight(sitLayerIndex, 1.0f);
        }

        // Si tenemos clips aleatorios, activamos la rutina de transiciones aleatorias
        if (_clipsValidos.Count > 0)
        {
            _blendHash = Animator.StringToHash("SitActionBlend");

            // [FIX #7] Cacheamos la comprobación del parámetro una sola vez.
            _hasBlendParam = HasParameterInternal(_blendHash);
            if (!_hasBlendParam)
            {
                Debug.LogError($"[RandomSit] {gameObject.name}: Parámetro 'SitActionBlend' no existe en el Animator. " +
                               $"Parámetros actuales: {GetAllParameterNames()}");
                yield break;
            }

            // [FIX #3] Semilla única por instancia usando System.Random.
            // No toca Random global de Unity, por lo que múltiples personajes pueden
            // inicializar en el mismo frame sin sobreescribirse la semilla entre sí.
            _rng = new System.Random(System.Guid.NewGuid().GetHashCode() ^ gameObject.GetHashCode());

            _initialized = true;
            _ready = true;

            StartCoroutine(RandomSitRoutine());
        }
        else
        {
            Debug.Log($"[RandomSit] {gameObject.name}: No se especificaron clips aleatorios. El personaje se mantendrá en su pose de sentado base.");
        }
    }

    // ─── Rutina principal ─────────────────────────────────────────────────────

    private IEnumerator RandomSitRoutine()
    {
        // Desincronización inicial: cada personaje espera un tiempo distinto.
        yield return new WaitForSeconds((float)(_rng.NextDouble() * maxStartupDelay));

        while (_ready)
        {
            // 1. REPOSO: Fundimos al Idle (blend → 0).
            yield return StartCoroutine(FadeBlend(0f));

            float tiempoEspera = minIdleTime + (float)(_rng.NextDouble() * (maxIdleTime - minIdleTime));
            yield return new WaitForSeconds(tiempoEspera);

            if (!_ready) yield break;

            // 2. PREPARACIÓN: Con blend=0 (Idle visible), intercambiamos el clip sin salto visual.
            AnimationClip clipElegido = ElegirClipUnico();
            if (clipElegido == null) continue; // Seguridad extra: no debería ocurrir

            _clipActivo = clipElegido;
            LobbyClipQueue.Reservar(_clipActivo);
            _overrideController[actionSlotPlaceholder] = clipElegido;

            // 3. ACCIÓN: Fundimos hacia el clip de acción (blend → 1).
            yield return StartCoroutine(FadeBlend(1f));

            // Esperamos a que termine la animación antes de volver al Idle.
            float espera = Mathf.Max(0.1f, clipElegido.length - smoothTransitionTime);
            yield return new WaitForSeconds(espera);

            // 4. Liberamos el clip para que otros personajes puedan usarlo.
            LobbyClipQueue.Liberar(_clipActivo);
            _lastClip  = _clipActivo;
            _clipActivo = null;
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private IEnumerator FadeBlend(float objetivo)
    {
        // [FIX #7] Usamos la variable cacheada en lugar de llamar HasParameter cada frame.
        if (!_hasBlendParam) yield break;

        float inicio  = _animator.GetFloat(_blendHash);
        float elapsed = 0f;

        while (elapsed < smoothTransitionTime)
        {
            elapsed += Time.deltaTime;
            _animator.SetFloat(_blendHash, Mathf.Lerp(inicio, objetivo, elapsed / smoothTransitionTime));
            yield return null;
        }

        _animator.SetFloat(_blendHash, objetivo);
    }

    /// <summary>
    /// Elige un clip que no esté en uso por otro personaje y que no sea el último reproducido.
    /// Prioridad: (1) libre y no repetido → (2) libre → (3) cualquiera sin repetir → (4) cualquiera.
    /// </summary>
    private AnimationClip ElegirClipUnico()
    {
        // Prioridad 1: libre Y distinto al último
        var candidatos = new List<AnimationClip>();
        foreach (var clip in _clipsValidos)
            if (!LobbyClipQueue.EnUso.Contains(clip) && clip != _lastClip)
                candidatos.Add(clip);

        if (candidatos.Count > 0)
            return candidatos[_rng.Next(0, candidatos.Count)];

        // Prioridad 2: libre (aunque repita el último si no hay otra opción)
        var libres = new List<AnimationClip>();
        foreach (var clip in _clipsValidos)
            if (!LobbyClipQueue.EnUso.Contains(clip))
                libres.Add(clip);

        if (libres.Count > 0)
            return libres[_rng.Next(0, libres.Count)];

        // Prioridad 3: cualquiera que no repita el último
        var sinRepetir = new List<AnimationClip>();
        foreach (var clip in _clipsValidos)
            if (clip != _lastClip) sinRepetir.Add(clip);

        if (sinRepetir.Count > 0)
            return sinRepetir[_rng.Next(0, sinRepetir.Count)];

        // Prioridad 4: cualquiera (todos en uso, todos iguales al último — caso extremo)
        return _clipsValidos[_rng.Next(0, _clipsValidos.Count)];
    }

    /// <summary>
    /// Comprobación interna usada UNA SOLA VEZ en Start para cachear _hasBlendParam.
    /// No llamar desde corrutinas ni Update.
    /// </summary>
    private bool HasParameterInternal(int hash)
    {
        if (_animator == null) return false;
        foreach (AnimatorControllerParameter p in _animator.parameters)
            if (p.nameHash == hash) return true;
        return false;
    }

    private string GetAllParameterNames()
    {
        if (_animator == null) return "sin animator";
        var sb = new System.Text.StringBuilder();
        foreach (AnimatorControllerParameter p in _animator.parameters)
            sb.Append($"{p.name}({p.type}), ");
        return sb.Length > 0 ? sb.ToString() : "ninguno";
    }
}
