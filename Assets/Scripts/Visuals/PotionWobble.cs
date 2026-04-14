using UnityEngine;

public class PotionWobble : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    [Tooltip("Velocidad a la que el líquido se bambolea.")]
    public float velocidad = 1.8f;
    
    [Tooltip("Qué tan fuerte es el movimiento (en grados).")]
    public float intensidad = 6.0f;

    private Vector3 rotacionInicial;

    void Start()
    {
        // Guardamos la rotación que tiene el líquido al empezar para no perder el offset
        rotacionInicial = transform.localEulerAngles;
    }

    void Update()
    {
        // Cálculo de doble seno para un movimiento menos predecible
        float wobbleX = Mathf.Sin(Time.time * velocidad) * intensidad;
        float wobbleZ = Mathf.Cos(Time.time * velocidad * 0.75f) * intensidad;

        // Aplicamos la rotación local. Al ser Local, se suma a la rotación del padre (la mano/botella)
        transform.localRotation = Quaternion.Euler(
            rotacionInicial.x + wobbleX,
            rotacionInicial.y,
            rotacionInicial.z + wobbleZ
        );
    }
}
