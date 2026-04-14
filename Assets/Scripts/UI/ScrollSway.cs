using UnityEngine;
using Unity.Netcode;
using StarterAssets;

public class ScrollSway : MonoBehaviour
{
    [Header("Ajustes del Sway (Movimiento)")]
    [Tooltip("Intensidad del movimiento al girar la cámara.")]
    public float swayAmount = 2f;
    [Tooltip("Límite máximo de rotación para que no se pase de rosca.")]
    public float maxSwayAmount = 5f;
    [Tooltip("La suavidad con la que el pergamino vuelve a su sitio.")]
    public float smoothness = 6f;

    private Quaternion initialLocalRotation;
    private StarterAssetsInputs playerInputs;

    void Start()
    {
        // Guardamos la rotación base inicial que le pusiste en la escena
        initialLocalRotation = transform.localRotation;
    }

    void Update()
    {
        // 1. Intentamos buscar el input del jugador local si aún no lo tenemos
        if (playerInputs == null)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient && NetworkManager.Singleton.LocalClient != null)
            {
                if (NetworkManager.Singleton.LocalClient.PlayerObject != null)
                {
                    playerInputs = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<StarterAssetsInputs>();
                }
            }
            return; // Si aún no ha aparecido el jugador, no hacemos nada
        }

        // 2. Leemos hacia dónde está mirando el ratón/mando
        float mouseX = playerInputs.look.x * swayAmount;
        float mouseY = playerInputs.look.y * swayAmount;

        // Limitamos para que no gire de forma extrema
        mouseX = Mathf.Clamp(mouseX, -maxSwayAmount, maxSwayAmount);
        mouseY = Mathf.Clamp(mouseY, -maxSwayAmount, maxSwayAmount);

        // 3. Calculamos la rotación objetivo AL REVÉS de donde miramos (para hacer efecto arrastre)
        // Ojo al signo: rotar en el eje X es arriba/abajo, rotar en el eje Y es izquierda/derecha.
        Quaternion targetRotation = Quaternion.Euler(initialLocalRotation.eulerAngles.x + mouseY, initialLocalRotation.eulerAngles.y - mouseX, initialLocalRotation.eulerAngles.z);

        // 4. Aplicamos el giro suavecito (SmoothDamp de rotaciones)
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, smoothness * Time.deltaTime);
    }
}
