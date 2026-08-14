using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace UI.Minigames
{
    public class MinijuegoRestregarUI : MonoBehaviour
    {
        [Header("Contenedor Principal Canvas UI")]
        [SerializeField] private GameObject mainPanel;

        [Header("Elementos de UI")]
        [SerializeField] private Image imgBarraSuciedad;
        [SerializeField] private Image imgBarraDesgaste;
        [SerializeField] private TextMeshProUGUI txtSuciedad;
        [SerializeField] private TextMeshProUGUI txtDesgaste;
        [SerializeField] private TextMeshProUGUI txtFeedback;
        [SerializeField] private TextMeshProUGUI txtInstruccion;

        [Header("Parámetros de Juego")]
        [SerializeField] private float suciedadInicial = 100f;
        [SerializeField] private float reduccionRitmoPerfecto = 1.5f; // Requiere ~67 alternancias perfectas (~30s barriendo)
        [SerializeField] private float reduccionRitmoIrregular = 0.4f; // Ritmo flojo prácticamente no avanza
        [SerializeField] private float incrementoDesgastePorRepeticion = 50.0f; // 2 fallos/spam = Desgaste Máximo / Strike
        [SerializeField] private float penalizacionSuciedadDesgasteMax = 25.0f; // Se ensucia +25% al fallar

        // Callbacks de evento
        public Action<float> OnProgresoSuciedadChanged; // Transmite normalizado [0..1] donde 1 es totalmente limpio
        public Action OnMinijuegoCompletado;
        public Action OnMinijuegoCancelado;
        public Action OnMinijuegoFallo;
        public Action<Key> OnTeclaPulsada; // Notifica la pulsación de A/D para el micro-movimiento del personaje

        // Estado interno
        private bool _juegoActivo = false;
        private bool _enPausaPorFallo = false;
        private float _suciedad = 100f;
        private float _desgaste = 0f;

        private Key _ultimaTecla = Key.None;
        private float _ultimoTiempoPulsacion = 0f;
        [Header("Strikes")]
        [SerializeField] private int maxStrikes = 3;
        private int _strikes = 0;

        private void Awake()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            gameObject.SetActive(false);
        }

        public void IniciarMinijuego()
        {
            _juegoActivo = true;
            _enPausaPorFallo = false;
            _suciedad = suciedadInicial;
            _desgaste = 0f;
            _strikes = 0;
            _ultimaTecla = Key.None;
            _ultimoTiempoPulsacion = 0f;

            gameObject.SetActive(true);
            if (mainPanel != null) mainPanel.SetActive(true);

            if (txtInstruccion != null) txtInstruccion.text = "¡Limpia alternando [A] y [D] con ritmo preciso!";
            if (txtFeedback != null) txtFeedback.text = "";

            ActualizarUI();
            OnProgresoSuciedadChanged?.Invoke(0f); // 0% limpio
        }

        public void DetenerMinijuego()
        {
            _juegoActivo = false;
            _enPausaPorFallo = false;
            _strikes = 0;
            if (mainPanel != null) mainPanel.SetActive(false);
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_juegoActivo) return;

            // 1. Cancelación con ESC
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                DetenerMinijuego();
                OnMinijuegoCancelado?.Invoke();
                return;
            }

            if (_enPausaPorFallo) return;
            if (Keyboard.current == null) return;

            bool presionoA = Keyboard.current.aKey.wasPressedThisFrame;
            bool presionoD = Keyboard.current.dKey.wasPressedThisFrame;

            if (presionoA && presionoD)
            {
                ProcesarPulsacion(Key.A);
            }
            else if (presionoA)
            {
                ProcesarPulsacion(Key.A);
            }
            else if (presionoD)
            {
                ProcesarPulsacion(Key.D);
            }
        }

        private void ProcesarPulsacion(Key tecla)
        {
            float ahora = Time.time;
            float deltaTiempo = _ultimoTiempoPulsacion > 0f ? (ahora - _ultimoTiempoPulsacion) : 0.3f;
            _ultimoTiempoPulsacion = ahora;

            OnTeclaPulsada?.Invoke(tecla);

            // Ventana súper estrecha y exigente conforme se limpia (de 200-420ms a 280-320ms)
            float progresoLimpieza = 1f - (_suciedad / 100f);
            float winMin = Mathf.Lerp(0.20f, 0.28f, progresoLimpieza);
            float winMax = Mathf.Lerp(0.42f, 0.32f, progresoLimpieza);

            bool esMismaTecla = (_ultimaTecla != Key.None && tecla == _ultimaTecla);
            bool esDemasiadoRapido = deltaTiempo < 0.16f; // Spam <160ms

            if (esMismaTecla || esDemasiadoRapido)
            {
                // ❌ MISMA TECLA REPETIDA O SPAM DEMASIADO RÁPIDO (<160ms): NO restar Suciedad, sumar a Desgaste
                _desgaste += incrementoDesgastePorRepeticion;

                if (txtFeedback != null)
                {
                    txtFeedback.text = esMismaTecla 
                        ? "<color=orange>¡ALTERNA [A] Y [D]!</color>" 
                        : "<color=orange>¡SPAM RÁPIDO! (<160ms)</color>";
                }

                if (_desgaste >= 100f)
                {
                    ProcesarDesgasteMaximo();
                    return;
                }
            }
            else
            {
                // ✅ TECLA ALTERNADA CORRECTAMENTE (A->D o D->A) Y TIEMPO VÁLIDO (>=160ms)
                _ultimaTecla = tecla;
                _desgaste = Mathf.Max(0f, _desgaste - 5f);

                bool enVentanaOptima = deltaTiempo >= winMin && deltaTiempo <= winMax;
                if (enVentanaOptima)
                {
                    _suciedad = Mathf.Max(0f, _suciedad - reduccionRitmoPerfecto);
                    if (txtFeedback != null) txtFeedback.text = "<color=green>¡RITMO PERFECTO! (-1.5%)</color>";
                }
                else
                {
                    // Válida pero fuera de ventana óptima: resta muy poca Suciedad
                    _suciedad = Mathf.Max(0f, _suciedad - reduccionRitmoIrregular);
                    if (txtFeedback != null)
                    {
                        txtFeedback.text = deltaTiempo < winMin ? "<color=yellow>¡CASI RÁPIDO! (-0.4%)</color>" : "<color=yellow>¡DEMASIADO LENTO! (-0.4%)</color>";
                    }
                }
            }

            ActualizarUI();

            float normalizadoLimpio = Mathf.Clamp01(1f - (_suciedad / 100f));
            OnProgresoSuciedadChanged?.Invoke(normalizadoLimpio);

            if (_suciedad <= 0f)
            {
                DetenerMinijuego();
                OnMinijuegoCompletado?.Invoke();
            }
        }

        private void ProcesarDesgasteMaximo()
        {
            _desgaste = 0f;
            _strikes++;
            _enPausaPorFallo = true;

            if (_strikes >= maxStrikes)
            {
                // 3 STRIKES: Reset completo de suciedad al 100%, reproducir headno y reiniciar minijuego
                _suciedad = 100f;
                if (txtFeedback != null) txtFeedback.text = $"<color=red>¡3 STRIKES! RESTREGADO REINICIADO (3/{maxStrikes})</color>";
                _strikes = 0;
            }
            else
            {
                // Strike parcial: +15% Suciedad
                _suciedad = Mathf.Min(100f, _suciedad + penalizacionSuciedadDesgasteMax);
                if (txtFeedback != null) txtFeedback.text = $"<color=red>¡DESGASTE MÁXIMO! STRIKE ({_strikes}/{maxStrikes})</color>";
            }

            ActualizarUI();

            float normalizadoLimpio = Mathf.Clamp01(1f - (_suciedad / 100f));
            OnProgresoSuciedadChanged?.Invoke(normalizadoLimpio);

            OnMinijuegoFallo?.Invoke(); // Transmite animación 'headno' local

            StartCoroutine(RutinaReinicioPausa());
        }

        private System.Collections.IEnumerator RutinaReinicioPausa()
        {
            yield return new WaitForSeconds(2.0f);
            if (_juegoActivo)
            {
                _enPausaPorFallo = false;
                _ultimaTecla = Key.None;
                _ultimoTiempoPulsacion = 0f;
                if (txtFeedback != null) txtFeedback.text = "";
            }
        }

        private void ActualizarUI()
        {
            float pctSuciedad = Mathf.Clamp01(_suciedad / 100f);
            float pctDesgaste = Mathf.Clamp01(_desgaste / 100f);

            if (imgBarraSuciedad != null) imgBarraSuciedad.fillAmount = pctSuciedad;
            if (imgBarraDesgaste != null) imgBarraDesgaste.fillAmount = pctDesgaste;

            if (txtSuciedad != null) txtSuciedad.text = $"Suciedad: {Mathf.RoundToInt(_suciedad)}%";
            if (txtDesgaste != null) txtDesgaste.text = $"Desgaste: {Mathf.RoundToInt(_desgaste)}%";
        }
    }
}
