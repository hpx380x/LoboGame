using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Cola global compartida entre TODOS los personajes del Lobby.
// Evita que dos personajes reproduzcan el mismo clip a la vez.
static class LobbyClipQueue
{
    public static readonly HashSet<AnimationClip> EnUso = new HashSet<AnimationClip>();
    public static bool Reservar(AnimationClip clip) => EnUso.Add(clip);
    public static void Liberar(AnimationClip clip) => EnUso.Remove(clip);
}

/// <summary>
/// Animaciones aleatorias de sentado para el Lobby.
/// Usa un Blend Tree 1D (SitActionBlend 0=Idle, 1=Accion) + AnimatorOverrideController.
/// El intercambio de clip ocurre cuando blend=0 (invisible), evitando saltos visuales.
/// </summary>
[RequireComponent(typeof(Animator))]
public class RandomLobbySit : MonoBehaviour
{
    [Header("Animaciones")]
    [Tooltip("El clip BASE de sentado (Ranura 0 del Blend Tree, threshold 0).")]
    [SerializeField] private AnimationClip baseSittingIdle;

    [Tooltip("El clip PLACEHOLDER de la Ranura 1 del Blend Tree (threshold 1). Sera reemplazado en runtime.")]
    [SerializeField] private AnimationClip actionSlotPlaceholder;

    [Tooltip("Todos los clips de accion aleatorios.")]
    [SerializeField] private List<AnimationClip> randomSittingClips = new List<AnimationClip>();

    [Header("Transicion")]
    [Tooltip("Duracion del fundido entre Idle y Accion (segundos).")]
    [SerializeField] [Range(0.1f, 2f)] private float smoothTransitionTime = 0.5f;

    [Tooltip("Tiempo minimo de reposo en Idle.")]
    [SerializeField] private float minIdleTime = 3f;

    [Tooltip("Tiempo maximo de reposo en Idle.")]
    [SerializeField] private float maxIdleTime = 8f;

    [Tooltip("Desincronizacion inicial maxima entre personajes.")]
    [SerializeField] private float maxStartupDelay = 4f;

    [Header("Exclusiones")]
    [Tooltip("Nombre del GameObject que NO tendra animaciones aleatorias.")]
    [SerializeField] private string excludedObjectName = "SM_Chr_Peasant_Male_01 (0)";

    // --- Privadas ---
    private Animator _animator;
    private AnimatorOverrideController _overrideController;
    private int _blendHash;
    private bool _initialized = false;
    private bool _ready = false;
    private AnimationClip _lastClip = null;
    private AnimationClip _clipActivo = null;

    private void OnEnable()
    {
        // [CLIENT] Al volver de SetActive(false), relanzamos la corrutina
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
        // Liberar clip de la cola global al desactivarse
        if (_clipActivo != null)
        {
            LobbyClipQueue.Liberar(_clipActivo);
            _clipActivo = null;
        }
        _ready = false;
    }

    private IEnumerator Start()
    {
        _animator = GetComponent<Animator>();

        if (gameObject.name == excludedObjectName || gameObject.name.Contains(excludedObjectName))
            yield break;

        yield return null; // Frame de gracia para que el Animator arranque

        if (_animator == null || _animator.runtimeAnimatorController == null
            || baseSittingIdle == null || actionSlotPlaceholder == null
            || randomSittingClips == null || randomSittingClips.Count == 0)
        {
            Debug.LogWarning($"[RandomSit] {gameObject.name}: Faltan referencias en el Inspector.");
            yield break;
        }

        // Override Controller: permite cambiar clips sin editar el AnimatorController
        // [CLIENT] Se ejecuta localmente, no requiere red
        _overrideController = new AnimatorOverrideController(_animator.runtimeAnimatorController);
        _animator.runtimeAnimatorController = _overrideController;
        _animator.applyRootMotion = false;

        _blendHash = Animator.StringToHash("SitActionBlend");

        if (!HasParameter(_blendHash))
        {
            Debug.LogError($"[RandomSit] {gameObject.name}: Parametro 'SitActionBlend' no existe en el Animator. " +
                           $"Parametros actuales: {GetAllParameterNames()}");
            yield break;
        }

        _animator.SetFloat(_blendHash, 0f);
        Debug.Log($"[RandomSit] {gameObject.name}: OK. {randomSittingClips.Count} clips disponibles.");

        _initialized = true;
        _ready = true;
        StartCoroutine(RandomSitRoutine());
    }

    private IEnumerator RandomSitRoutine()
    {
        yield return new WaitForSeconds(Random.Range(0f, maxStartupDelay));

        while (_ready)
        {
            // 1. REPOSO: Fundimos al Idle (blend -> 0)
            yield return StartCoroutine(FadeBlend(0f));
            yield return new WaitForSeconds(Random.Range(minIdleTime, maxIdleTime));

            if (!_ready) yield break;

            // 2. PREPARACION: Con blend=0 (Idle visible), intercambiamos el clip silenciosamente
            AnimationClip clipElegido = ElegirClipUnico();
            _clipActivo = clipElegido;
            LobbyClipQueue.Reservar(_clipActivo);
            _overrideController[actionSlotPlaceholder] = clipElegido;

            // 3. ACCION: Fundimos hacia el clip de accion (blend -> 1)
            yield return StartCoroutine(FadeBlend(1f));

            // Esperamos a que termine la animacion
            float espera = Mathf.Max(0.1f, clipElegido.length - smoothTransitionTime);
            yield return new WaitForSeconds(espera);

            // 4. Liberamos el clip para que otros puedan usarlo
            LobbyClipQueue.Liberar(_clipActivo);
            _lastClip = _clipActivo;
            _clipActivo = null;
        }
    }

    private IEnumerator FadeBlend(float objetivo)
    {
        if (!HasParameter(_blendHash)) yield break;
        float inicio = _animator.GetFloat(_blendHash);
        float elapsed = 0f;
        while (elapsed < smoothTransitionTime)
        {
            elapsed += Time.deltaTime;
            _animator.SetFloat(_blendHash, Mathf.Lerp(inicio, objetivo, elapsed / smoothTransitionTime));
            yield return null;
        }
        _animator.SetFloat(_blendHash, objetivo);
    }

    private AnimationClip ElegirClipUnico()
    {
        var candidatos = new List<AnimationClip>();
        foreach (var clip in randomSittingClips)
            if (!LobbyClipQueue.EnUso.Contains(clip) && clip != _lastClip)
                candidatos.Add(clip);

        if (candidatos.Count > 0) return candidatos[Random.Range(0, candidatos.Count)];

        var sinRepetir = new List<AnimationClip>();
        foreach (var clip in randomSittingClips)
            if (clip != _lastClip) sinRepetir.Add(clip);

        if (sinRepetir.Count > 0) return sinRepetir[Random.Range(0, sinRepetir.Count)];
        return randomSittingClips[Random.Range(0, randomSittingClips.Count)];
    }

    private bool HasParameter(int hash)
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
