using UnityEngine;
using StarterAssets;

/// <summary>
/// Receptor de AnimationEvents para el modelo del jugador (Loboplayer).
/// 
/// PROBLEMA: Unity envía los AnimationEvents al GameObject que tiene el Animator.
/// En esta arquitectura, el Animator está en el hijo "Loboplayer", pero los métodos
/// OnFootstep/OnLand están en ThirdPersonController que está en el GameObject PADRE.
/// Unity no sube en la jerarquía para buscar receptores, por eso el error "has no receiver".
///
/// SOLUCIÓN: Este script vive en el mismo GameObject que el Animator (Loboplayer)
/// y reenvía los eventos al ThirdPersonController del padre.
/// </summary>
public class AnimationEventRelay : MonoBehaviour
{
    private ThirdPersonController _controller;

    private void Awake()
    {
        // Buscar el ThirdPersonController en el padre (puede estar varios niveles arriba)
        _controller = GetComponentInParent<ThirdPersonController>();

        if (_controller == null)
        {
            Debug.LogWarning($"[AnimationEventRelay] No se encontró ThirdPersonController en los padres de '{gameObject.name}'. " +
                             "Los sonidos de pasos y aterrizaje no funcionarán.", gameObject);
        }
    }

    // Receptor de evento de paso - llamado desde las animaciones Walk_N y Run_N
    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (_controller == null) return;
        _controller.RelayOnFootstep(animationEvent);
    }

    // Receptor de evento de aterrizaje - llamado desde JumpLand, Walk_N_Land, Run_N_Land
    private void OnLand(AnimationEvent animationEvent)
    {
        if (_controller == null) return;
        _controller.RelayOnLand(animationEvent);
    }
}
