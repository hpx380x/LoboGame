using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace UI.Minigames
{
    public class MinijuegoAfilarUI : MonoBehaviour
    {
        [Header("Contenedor Principal Canvas UI")]
        [SerializeField] private GameObject mainPanel;

        [Header("Carriles (0=W, 1=A, 2=S, 3=D)")]
        [SerializeField] private RectTransform[] laneSpawnPoints = new RectTransform[4];
        [SerializeField] private RectTransform[] laneHitZones = new RectTransform[4];

        [Header("Prefabs de Notas")]
        [SerializeField] private GameObject notePrefab;

        [Header("Textos de Feedback UI")]
        [SerializeField] private TextMeshProUGUI txtProgreso;
        [SerializeField] private TextMeshProUGUI txtCombo;
        [SerializeField] private TextMeshProUGUI txtFeedbackHit;

        [Header("Parámetros de Dificultad")]
        [SerializeField] private int aciertosRequeridos = 8;
        [SerializeField] private float velocidadInicial = 450f;
        [SerializeField] private float incrementoVelocidadPorAcierto = 45f;
        [SerializeField] private float intervaloSpawnInicial = 0.9f;
        [SerializeField] private float toleranciaFranjaY = 55f; // Margen de acierto alrededor del centro de la hitZone

        // Callbacks de evento
        public Action OnMinijuegoCompletado;
        public Action OnMinijuegoCancelado;
        public Action OnMinijuegoFallo;

        // Estado interno
        private bool _juegoActivo = false;
        private bool _enPausaPorFallo = false;
        private int _aciertosActuales = 0;
        private int _comboActual = 0;
        private float _velocidadActual = 450f;
        private float _timerSpawn = 0f;

        // Estructura interna de Nota
        private class NotaActiva
        {
            public int carril; // 0=W, 1=A, 2=S, 3=D
            public RectTransform rect;
            public Key tecla;
        }

        private readonly List<NotaActiva> _notas = new List<NotaActiva>();
        private readonly Key[] _teclasCarril = new Key[4] { Key.W, Key.A, Key.S, Key.D };
        private readonly string[] _nombresCarril = new string[4] { "W", "A", "S", "D" };

        private void Awake()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            gameObject.SetActive(false);
        }

        public void IniciarMinijuego()
        {
            _juegoActivo = true;
            _enPausaPorFallo = false;
            _aciertosActuales = 0;
            _comboActual = 0;
            _velocidadActual = velocidadInicial;
            _timerSpawn = 0f;

            LimpiarNotas();

            gameObject.SetActive(true);
            if (mainPanel != null) mainPanel.SetActive(true);
            ActualizarUI();
            if (txtFeedbackHit != null) txtFeedbackHit.text = "";
        }

        public void DetenerMinijuego()
        {
            _juegoActivo = false;
            _enPausaPorFallo = false;
            LimpiarNotas();
            if (mainPanel != null) mainPanel.SetActive(false);
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_juegoActivo) return;

            // 1. Cancelación manual con ESC
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                DetenerMinijuego();
                OnMinijuegoCancelado?.Invoke();
                return;
            }

            if (_enPausaPorFallo) return;

            // 2. Mover notas cayendo
            float delta = Time.deltaTime;
            _timerSpawn += delta;

            float intervaloActual = Mathf.Max(0.4f, intervaloSpawnInicial - (_aciertosActuales * 0.04f));
            if (_timerSpawn >= intervaloActual)
            {
                _timerSpawn = 0f;
                SpawnNotaAleatoria();
            }

            for (int i = _notas.Count - 1; i >= 0; i--)
            {
                var n = _notas[i];
                if (n.rect == null)
                {
                    _notas.RemoveAt(i);
                    continue;
                }

                // Mover hacia abajo en Y usando anchoredPosition
                Vector2 pos = n.rect.anchoredPosition;
                pos.y -= _velocidadActual * delta;
                n.rect.anchoredPosition = pos;

                // Comprobar si se pasó por debajo de la franja de acierto (Fallo por omisión)
                float targetY = laneHitZones[n.carril] != null ? laneHitZones[n.carril].anchoredPosition.y : -250f;
                if (pos.y < (targetY - toleranciaFranjaY))
                {
                    ProcesarFallo("MISS");
                    return;
                }
            }

            // 3. Lectura de teclas W, A, S, D
            if (Keyboard.current != null)
            {
                for (int c = 0; c < 4; c++)
                {
                    if (Keyboard.current[_teclasCarril[c]].wasPressedThisFrame)
                    {
                        ProcesarPulsacionCarril(c);
                        if (_enPausaPorFallo) return;
                    }
                }
            }
        }

        private void SpawnNotaAleatoria()
        {
            int carril = UnityEngine.Random.Range(0, 4);
            if (laneSpawnPoints[carril] == null || notePrefab == null) return;

            GameObject obj = Instantiate(notePrefab, laneSpawnPoints[carril].parent);
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect == null) return;

            rect.anchoredPosition = laneSpawnPoints[carril].anchoredPosition;

            var tmp = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = _nombresCarril[carril];

            _notas.Add(new NotaActiva
            {
                carril = carril,
                rect = rect,
                tecla = _teclasCarril[carril]
            });
        }

        private void ProcesarPulsacionCarril(int carril)
        {
            if (laneHitZones[carril] == null) return;

            float targetY = laneHitZones[carril].anchoredPosition.y;
            NotaActiva mejorNota = null;
            float menorDistancia = float.MaxValue;

            for (int i = 0; i < _notas.Count; i++)
            {
                if (_notas[i].carril == carril)
                {
                    float dist = Mathf.Abs(_notas[i].rect.anchoredPosition.y - targetY);
                    if (dist < menorDistancia)
                    {
                        menorDistancia = dist;
                        mejorNota = _notas[i];
                    }
                }
            }

            if (mejorNota != null && menorDistancia <= toleranciaFranjaY)
            {
                // ✅ ACIERTO
                _aciertosActuales++;
                _comboActual++;
                _velocidadActual += incrementoVelocidadPorAcierto;

                if (txtFeedbackHit != null)
                {
                    txtFeedbackHit.text = menorDistancia < (toleranciaFranjaY * 0.4f) ? "<color=green>PERFECT!</color>" : "<color=yellow>GOOD!</color>";
                }

                Destroy(mejorNota.rect.gameObject);
                _notas.Remove(mejorNota);
                ActualizarUI();

                if (_aciertosActuales >= aciertosRequeridos)
                {
                    DetenerMinijuego();
                    OnMinijuegoCompletado?.Invoke();
                }
            }
            else
            {
                // ❌ PULSACIÓN EN FALSO
                ProcesarFallo("WRONG KEY");
            }
        }

        private void ProcesarFallo(string msg)
        {
            if (_enPausaPorFallo || !_juegoActivo) return;

            _aciertosActuales = 0;
            _comboActual = 0;
            _velocidadActual = velocidadInicial;
            _enPausaPorFallo = true;

            LimpiarNotas();

            if (txtFeedbackHit != null) txtFeedbackHit.text = $"<color=red>¡FALLO! REINICIANDO...</color>";
            ActualizarUI();

            OnMinijuegoFallo?.Invoke();

            StartCoroutine(RutinaReinicioFallo());
        }

        private System.Collections.IEnumerator RutinaReinicioFallo()
        {
            yield return new WaitForSeconds(2.5f);
            if (_juegoActivo)
            {
                _enPausaPorFallo = false;
                _timerSpawn = 0f;
                if (txtFeedbackHit != null) txtFeedbackHit.text = "";
            }
        }

        private void ActualizarUI()
        {
            if (txtProgreso != null) txtProgreso.text = $"Progreso: {_aciertosActuales}/{aciertosRequeridos}";
            if (txtCombo != null) txtCombo.text = _comboActual > 1 ? $"Combo x{_comboActual}" : "";
        }

        private void LimpiarNotas()
        {
            for (int i = 0; i < _notas.Count; i++)
            {
                if (_notas[i]?.rect != null)
                    Destroy(_notas[i].rect.gameObject);
            }
            _notas.Clear();
        }
    }
}
