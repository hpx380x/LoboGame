using UnityEngine;
using System.Collections;

public class ScrollVisual : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El SkinnedMeshRenderer que contiene el BlendShape 'Abierto'")]
    public SkinnedMeshRenderer scrollRenderer;
    
    [Header("Ajustes de Animación")]
    [Tooltip("Tiempo en segundos que tarda en abrirse o cerrarse")]
    public float animationDuration = 0.5f;

    // Queremos que el índice del BlendShape sea el primero (0)
    private int blendShapeIndex = 0;
    private Coroutine currentAnimation;
    private bool isOpen = false;

    private void Start()
    {
        if (scrollRenderer == null)
        {
            scrollRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }
    }

    /// <summary>
    /// Inicia la animación visual para abrir el pergamino. (Se llama localmente).
    /// </summary>
    public void OpenScroll()
    {
        if (isOpen) return;
        isOpen = true;

        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(AnimateBlendShape(100f));
    }

    /// <summary>
    /// Inicia la animación visual para cerrar el pergamino. (Se llama localmente).
    /// </summary>
    public void CloseScroll()
    {
        if (!isOpen) return;
        isOpen = false;

        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(AnimateBlendShape(0f));
    }

    // Corrutina que transiciona suavemente el valor del BlendShape
    private IEnumerator AnimateBlendShape(float targetWeight)
    {
        if (scrollRenderer == null) yield break;

        float startWeight = scrollRenderer.GetBlendShapeWeight(blendShapeIndex);
        float timeElapsed = 0f;

        while (timeElapsed < animationDuration)
        {
            float currentWeight = Mathf.Lerp(startWeight, targetWeight, timeElapsed / animationDuration);
            // Aplicar una curva suave (Ease In / Ease Out) matemáticamente
            float smoothStepWeight = Mathf.SmoothStep(startWeight, targetWeight, timeElapsed / animationDuration);
            
            scrollRenderer.SetBlendShapeWeight(blendShapeIndex, smoothStepWeight);
            timeElapsed += Time.deltaTime;
            
            yield return null;
        }

        // Aseguramos que termine exactamente en el valor objetivo
        scrollRenderer.SetBlendShapeWeight(blendShapeIndex, targetWeight);
    }
}
