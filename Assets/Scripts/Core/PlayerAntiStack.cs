using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerAntiStack : NetworkBehaviour
{
    private CharacterController characterController;
    private Vector3 vectorResbalon = Vector3.zero;
    
    [Tooltip("La fuerza con la que el muñeco resbala de las cabezas ajenas")]
    public float fuerzaResbalon = 4f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // Regla de Netcode: Solo el dueño de este cuerpo procesa sus físicas contra otros
        if (!IsOwner) return;

        // Si tenemos un vector de resbalón acumulado, empujamos al jugador físicamente sin interrumpir su salto
        if (vectorResbalon != Vector3.zero)
        {
            characterController.Move(vectorResbalon * fuerzaResbalon * Time.deltaTime);
            
            // Freno automático: Suavizamos la fuerza para no salir disparados infinitamente al tocar el suelo
            vectorResbalon = Vector3.Lerp(vectorResbalon, Vector3.zero, Time.deltaTime * 5f);
        }
    }

    // Esta función la llama el CharacterController de Unity cada vez que roza otro colisionador
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!IsOwner) return;

        // 1. Preguntamos: ¿El objeto pesado que pisamos (o rozamos) tiene sangre de Jugador?
        NetworkObject otroJugador = hit.collider.GetComponentInParent<NetworkObject>();

        // Si es un jugador y NO somos nosotros mismos frotándonos el brazo
        if (otroJugador != null && otroJugador.gameObject != this.gameObject)
        {
            // 2. ¿Chocamos con la parte superior de su cuerpo? 
            // Las normales (hit.normal) van de 0 (Pared) a 1 (Suelo plano total).
            // Si el choque viene de abajo hacia arriba (es decir, nosotros estamos encima), lo empujamos
            if (hit.normal.y > 0.1f)
            {
                // Calculamos hacia dónde nos caemos: Del centro geométrico del tipo de abajo, hacia nosotros
                Vector3 direccionEscape = new Vector3(transform.position.x - hit.transform.position.x, 0f, transform.position.z - hit.transform.position.z).normalized;

                // Margen de Desempate: Si Mario salta PERFECTAMENTE exacto en el centro del eje de Luigi, 
                // las matemáticas darían cero y el motor colapsaría. Aquí lo empujamos al azar si pasa eso.
                if (direccionEscape == Vector3.zero)
                {
                    direccionEscape = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
                }

                // Inyectamos el vector en la memoria para que el Update empiece a deslizar el cuerpo
                vectorResbalon = direccionEscape;
            }
        }
    }
}
