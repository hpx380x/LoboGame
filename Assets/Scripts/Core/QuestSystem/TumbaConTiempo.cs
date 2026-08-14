using UnityEngine;
using Unity.Netcode;
using Core.Environment;
using Core.Enums;
using System.Collections;

namespace Core.QuestSystem
{
    /// <summary>
    /// Controlador para la misión de "La Cripta del Tiempo" en el Cementerio.
    /// Al interactuar o recoger el objeto sagrado, se activa un temporizador (ej: 30s).
    /// La tapa o puerta de la tumba se cierra gradualmente.
    /// Si el jugador no introduce el objeto a tiempo, la tumba se cierra y se resetea.
    /// </summary>
    public class TumbaConTiempo : NetworkBehaviour
    {
        [Header("Configuración del Tiempo")]
        [Tooltip("Tiempo en segundos antes de que la tumba se cierre completamente.")]
        public float tiempoLimiteSegundos = 30f;

        [Header("Referencias Visuales de la Tumba")]
        [Tooltip("Transform de la tapa o puerta que se moverá/rotará progresivamente.")]
        public Transform tapaTumba;

        [Tooltip("Posición inicial (abierta) de la tapa.")]
        public Vector3 posicionAbierta = new Vector3(0f, 1.2f, 0f);

        [Tooltip("Posición final (cerrada) de la tapa.")]
        public Vector3 posicionCerrada = Vector3.zero;

        [Tooltip("Rotación inicial (abierta) de la tapa (Euler).")]
        public Vector3 rotacionAbierta = new Vector3(-45f, 0f, 0f);

        [Tooltip("Rotación final (cerrada) de la tapa (Euler).")]
        public Vector3 rotacionCerrada = Vector3.zero;

        [Header("Componente de Entrega (PuntoEntrega)")]
        [Tooltip("Componente UniversalQuestInteractable en esta tumba configurado como PuntoEntrega.")]
        public UniversalQuestInteractable puntoEntrega;

        [Header("HUD Flotante 3D opcional")]
        public TMPro.TextMeshPro textoCuentaAtras;

        // Variables de estado sincronizadas
        private NetworkVariable<bool> estaAbierta = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> tiempoRestante = new NetworkVariable<float>(30f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> temporizadorActivo = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private Camera cachedCamMain;

        private void Start()
        {
            cachedCamMain = Camera.main;
            if (tapaTumba != null)
            {
                tapaTumba.localPosition = posicionAbierta;
                tapaTumba.localRotation = Quaternion.Euler(rotacionAbierta);
            }
        }

        public override void OnNetworkSpawn()
        {
            estaAbierta.OnValueChanged += OnEstadoAbiertaChanged;
            tiempoRestante.OnValueChanged += OnTiempoRestanteChanged;
        }

        public override void OnNetworkDespawn()
        {
            estaAbierta.OnValueChanged -= OnEstadoAbiertaChanged;
            tiempoRestante.OnValueChanged -= OnTiempoRestanteChanged;
        }

        private void Update()
        {
            // Efecto Billboard para el texto 3D del temporizador
            if (textoCuentaAtras != null && textoCuentaAtras.gameObject.activeSelf)
            {
                if (cachedCamMain == null) cachedCamMain = Camera.main;
                if (cachedCamMain != null) textoCuentaAtras.transform.rotation = cachedCamMain.transform.rotation;
            }

            // El Servidor procesa la cuenta atrás
            if (IsServer && temporizadorActivo.Value)
            {
                tiempoRestante.Value -= Time.deltaTime;

                // Animación suave de la tapa en el servidor (sincronizada mediante tiempoRestante)
                ActualizarVisualesTapa(tiempoRestante.Value / tiempoLimiteSegundos);

                if (tiempoRestante.Value <= 0f)
                {
                    FallarMisionServer();
                }
            }
            else if (!IsServer && temporizadorActivo.Value)
            {
                // Clientes interpolan la animación suavemente
                ActualizarVisualesTapa(tiempoRestante.Value / tiempoLimiteSegundos);
            }
        }

        /// <summary>
        /// Inicia el temporizador de 30 segundos (llamado cuando el jugador interactúa o recoge la reliquia).
        /// </summary>
        public void IniciarTemporizador()
        {
            if (IsServer)
            {
                tiempoRestante.Value = tiempoLimiteSegundos;
                temporizadorActivo.Value = true;
                estaAbierta.Value = true;
            }
            else
            {
                IniciarTemporizadorServerRpc();
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void IniciarTemporizadorServerRpc()
        {
            IniciarTemporizador();
        }

        private void FallarMisionServer()
        {
            if (!IsServer) return;

            temporizadorActivo.Value = false;
            tiempoRestante.Value = 0f;
            estaAbierta.Value = false;

            Debug.Log("[TumbaConTiempo] ¡Se acabó el tiempo! La tumba se ha cerrado.");

            // Reiniciar automáticamente tras 4 segundos de espera
            StartCoroutine(ResetearTumbaTrasEspera(4f));
        }

        private IEnumerator ResetearTumbaTrasEspera(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (IsServer)
            {
                tiempoRestante.Value = tiempoLimiteSegundos;
                estaAbierta.Value = true;
                temporizadorActivo.Value = false;
                ActualizarVisualesTapa(1.0f);
            }
        }

        private void ActualizarVisualesTapa(float porcentajeProgreso)
        {
            porcentajeProgreso = Mathf.Clamp01(porcentajeProgreso);
            // 1.0 = Totalmente abierta, 0.0 = Totalmente cerrada

            if (tapaTumba != null)
            {
                tapaTumba.localPosition = Vector3.Lerp(posicionCerrada, posicionAbierta, porcentajeProgreso);
                tapaTumba.localRotation = Quaternion.Euler(Vector3.Lerp(rotacionCerrada, rotacionAbierta, porcentajeProgreso));
            }
        }

        private void OnTiempoRestanteChanged(float prev, float current)
        {
            if (textoCuentaAtras != null)
            {
                if (temporizadorActivo.Value && current > 0f)
                {
                    textoCuentaAtras.gameObject.SetActive(true);
                    textoCuentaAtras.text = $"⏱️ {Mathf.CeilToInt(current)}s";
                    textoCuentaAtras.color = current <= 5f ? Color.red : Color.yellow;
                }
                else
                {
                    textoCuentaAtras.gameObject.SetActive(false);
                }
            }
        }

        private void OnEstadoAbiertaChanged(bool prev, bool current)
        {
            if (puntoEntrega != null)
            {
                // Si la tumba está cerrada, desactivamos la posibilidad de entregar
                puntoEntrega.enabled = current;
            }
        }
    }
}
