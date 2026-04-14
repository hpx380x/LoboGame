using UnityEngine;

public class TapestryRestorer : MonoBehaviour
{
    [Header("Configuración")]
    public SkinnedMeshRenderer brokenMesh;
    public ParticleSystem magicParticles;
    public float joinDuration = 1.8f;

    private MinigameTrigger trigger;

    private void Awake()
    {
        trigger = GetComponent<MinigameTrigger>();
        // Inicialmente roto (100%)
        if (brokenMesh != null) brokenMesh.SetBlendShapeWeight(0, 100f);
    }

    // El Trigger debe llamar a este método cuando abre la UI
    // O mejor, nos suscribimos al minijuego cuando se instancia
    public void SuscribirseAlMinijuego(TapestryMinigame localMinigame)
    {
        localMinigame.OnMinigameWon += IniciarRestauracion;
    }

    private void IniciarRestauracion()
    {
        StartCoroutine(AnimarUnion());
    }

    private System.Collections.IEnumerator AnimarUnion()
    {
        if (magicParticles != null) magicParticles.Play();

        float elapsed = 0;
        while (elapsed < joinDuration)
        {
            elapsed += Time.deltaTime;
            float weight = Mathf.Lerp(100f, 0f, elapsed / joinDuration);
            if (brokenMesh != null) brokenMesh.SetBlendShapeWeight(0, weight);
            yield return null;
        }

        if (brokenMesh != null) brokenMesh.SetBlendShapeWeight(0, 0f);
        Debug.Log("[Tapestry] Restauración Mágica Completada");
    }
}
