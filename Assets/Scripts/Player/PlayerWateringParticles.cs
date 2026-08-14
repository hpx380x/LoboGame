using UnityEngine;

namespace Core.QuestSystem
{
    /// <summary>
    /// Crea y configura un sistema de partículas de agua dinámicamente
    /// sobre el balde de agua o regadera del personaje al iniciar la partida.
    /// </summary>
    public class PlayerWateringParticles : MonoBehaviour
    {
        [Header("Ajuste de Partículas (Local)")]
        [Tooltip("Ajusta la posición de donde sale el agua relativo a la regadera")]
        public Vector3 localPositionOffset = new Vector3(0f, 0.25f, 0.2f);
        
        [Tooltip("Ajusta la rotación del chorro de agua")]
        public Vector3 localRotationOffset = new Vector3(45f, 0f, 0f);

        private ParticleSystem waterParticles;
        private GameObject wateringCanObject;

        private void Start()
        {
            InitializeParticles();
        }

        private void InitializeParticles()
        {
            // Como el script ahora se coloca DIRECTAMENTE en el objeto de la regadera,
            // ya no necesitamos buscarlo por la jerarquía.
            wateringCanObject = this.gameObject;

            if (wateringCanObject != null)
            {
                // 2. Crear el punto emisor de agua (pivote) en la punta
                GameObject emitterPivot = new GameObject("WaterSpout_Particles");
                emitterPivot.transform.SetParent(wateringCanObject.transform, false);
                
                // Lo posicionamos usando las variables expuestas en el Inspector
                emitterPivot.transform.localPosition = localPositionOffset;
                emitterPivot.transform.localRotation = Quaternion.Euler(localRotationOffset);

                // 3. Crear el ParticleSystem
                waterParticles = emitterPivot.AddComponent<ParticleSystem>();
                waterParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // Detener el PlayOnAwake por defecto
                
                // 4. Configurar el Sistema de Partículas por Código (Premium & Ajustado)
                var main = waterParticles.main;
                main.duration = 1.5f;
                main.loop = false;
                main.playOnAwake = false;
                main.startLifetime = 0.8f;
                main.startSpeed = 2.5f;
                main.startSize = 0.08f;
                main.gravityModifier = 0.8f; // Cae con gravedad
                main.simulationSpace = ParticleSystemSimulationSpace.World; // Las partículas se quedan en el mundo

                // Color azul translúcido de agua
                main.startColor = new Color(0.3f, 0.6f, 1f, 0.5f);

                // Configuración de la forma de emisión (cono cerrado)
                var shape = waterParticles.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 12f;
                shape.radius = 0.04f;

                // Emisión de flujo continuo
                var emission = waterParticles.emission;
                emission.enabled = true;
                emission.rateOverTime = 80f; // Flujo denso de gotas

                // Colisiones opcionales con el suelo para realismo
                var collision = waterParticles.collision;
                collision.enabled = true;
                collision.type = ParticleSystemCollisionType.World;
                collision.mode = ParticleSystemCollisionMode.Collision3D;
                collision.bounce = 0.1f;
                
                // Color sobre el tiempo (se desvanece al caer)
                var colorOverLifetime = waterParticles.colorOverLifetime;
                colorOverLifetime.enabled = true;
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(new Color(0.3f, 0.6f, 1f), 0.0f), new GradientColorKey(new Color(0.2f, 0.5f, 0.9f), 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
                );
                colorOverLifetime.color = gradient;

                // Asignar el Material default de partículas de Unity (compatible con URP y Legacy)
                ParticleSystemRenderer psRenderer = emitterPivot.GetComponent<ParticleSystemRenderer>();
                if (psRenderer != null)
                {
                    Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                    if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
                    if (particleShader == null) particleShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");

                    if (particleShader != null)
                    {
                        psRenderer.material = new Material(particleShader);
                    }
                    else
                    {
                        Debug.LogWarning("[PlayerWateringParticles] No se encontró ningún Shader de partículas compatible. Se usará el material por defecto.");
                    }
                }

                Debug.Log($"[PlayerWateringParticles] Sistema de partículas de agua configurado exitosamente en {wateringCanObject.name}");
            }
            else
            {
                Debug.LogError("[PlayerWateringParticles] ¡No se pudo inicializar las partículas porque no se encontró el modelo de la regadera en el personaje!");
            }
        }

        /// <summary>
        /// Activa la expulsión de agua.
        /// </summary>
        public void PlayWaterParticles()
        {
            if (waterParticles != null)
            {
                waterParticles.Play();
                Debug.Log("[PlayerWateringParticles] ¡Regando! Expulsando partículas de agua.");
            }
        }

        /// <summary>
        /// Detiene la expulsión de agua.
        /// </summary>
        public void StopWaterParticles()
        {
            if (waterParticles != null)
            {
                waterParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}
