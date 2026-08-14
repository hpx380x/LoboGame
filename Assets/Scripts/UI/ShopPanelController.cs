using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core.Enums;
using System.Collections.Generic;

namespace Core.UI
{
    /// <summary>
    /// Controlador de UI para el panel de la tienda del mercader.
    /// Crea el panel dinámicamente sobre el Canvas de gameplay para no ensuciar la escena.
    /// </summary>
    public class ShopPanelController : MonoBehaviour
    {
        public bool PanelActivo { get; private set; } = false;

        private GameObject panelRoot;
        private GameObject playerLocal;
        private List<ShopItemUI> spawnedItems = new List<ShopItemUI>();

        private int activeTab = 0; // 0: Pergaminos, 1: Consumibles

        // Elementos de la UI cacheados para actualizar dinámicamente
        private TextMeshProUGUI goldText;
        private GameObject contentContainer;
        private Image tabScrollsBg;
        private TextMeshProUGUI tabScrollsText;
        private Image tabConsumablesBg;
        private TextMeshProUGUI tabConsumablesText;

        private struct ScrollItemData
        {
            public TipoObjeto tipo;
            public string nombre;
            public string descripcion;
            public int coste;

            public ScrollItemData(TipoObjeto t, string n, string d, int c)
            {
                tipo = t;
                nombre = n;
                descripcion = d;
                coste = c;
            }
        }

        private List<ScrollItemData> itemsPergaminos = new List<ScrollItemData>()
        {
            new ScrollItemData(TipoObjeto.PergaminoCupido, "Arco de Cupido", "Permite forjar el arco místico. Tus flechas enamorarán a dos objetivos, vinculando sus destinos.", 15),
            new ScrollItemData(TipoObjeto.PergaminoDaga, "Daga de Cazador", "Permite forjar la daga de plata. Podrás defenderte del lobo si te ataca.", 15),
            new ScrollItemData(TipoObjeto.PergaminoManzana, "Manzana Dorada", "Permite bendecir la manzana de oro. Concede curación e inmunidad temporal.", 15),
            new ScrollItemData(TipoObjeto.PergaminoPocion, "Poción de Alquimista", "Permite destilar la poción mixta. Puede curar a un aliado o envenenar a un sospechoso.", 15),
            new ScrollItemData(TipoObjeto.PergaminoAntifaz, "Antifaz de Ladrón", "Permite tejer el antifaz de sombras. Podrás ver en la oscuridad y robar objetos.", 15),
            new ScrollItemData(TipoObjeto.PergaminoRelicario, "Relicario Sagrado", "Permite forjar el relicario protector. Te protege de una muerte nocturna.", 15),
            new ScrollItemData(TipoObjeto.PergaminoSombrero, "Sombrero de Tonto", "Permite tejer el sombrero místico. Protege de sospechas pero anula tu voto.", 15)
        };

        private List<ScrollItemData> itemsConsumibles = new List<ScrollItemData>()
        {
            new ScrollItemData(TipoObjeto.PocionVelocidad, "Poción de Velocidad", "Aumenta la velocidad de movimiento significativamente durante unos segundos.", 10),
            new ScrollItemData(TipoObjeto.PocionVision, "Poción de Visión", "Permite ver mejor en la oscuridad y vislumbrar amenazas ocultas.", 10),
            new ScrollItemData(TipoObjeto.BombaApestosa, "Bomba Apestosa", "Genera una cortina de humo cegadora para despistar a tus perseguidores.", 8),
            new ScrollItemData(TipoObjeto.BotasSilenciosas, "Botas Silenciosas", "Silencia tus pasos temporalmente permitiéndote moverte sin ser detectado.", 12),
            new ScrollItemData(TipoObjeto.CotaDeMalla, "Cota de Malla", "Armadura ligera que absorbe y bloquea el primer ataque letal que recibas.", 15)
        };

        private class ShopItemUI
        {
            public TipoObjeto tipo;
            public int coste;
            public Button botonCompra;
            public TextMeshProUGUI textoPrecio;
            public Image backgroundCard;
        }

        private void Start()
        {
            BuildShopPanel();
        }

        private void BuildShopPanel()
        {
            // Panel de fondo oscuro translúcido
            panelRoot = new GameObject("ShopPanel_Root", typeof(RectTransform), typeof(CanvasRenderer));
            panelRoot.transform.SetParent(this.transform, false);

            RectTransform rt = panelRoot.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            Image bgImage = panelRoot.AddComponent<Image>();
            bgImage.color = new Color(0.04f, 0.04f, 0.06f, 0.92f); // Más oscuro para enfocar la UI

            // Ventana principal
            GameObject window = new GameObject("Window", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            window.transform.SetParent(panelRoot.transform, false);
            Image winBg = window.GetComponent<Image>();
            winBg.color = new Color(0.09f, 0.09f, 0.12f, 0.98f);
            
            Outline outline = window.AddComponent<Outline>();
            outline.effectColor = new Color(0.85f, 0.65f, 0.15f, 0.4f); // Dorado premium brillante
            outline.effectDistance = new Vector2(3f, 3f);

            RectTransform winRt = window.GetComponent<RectTransform>();
            winRt.anchorMin = new Vector2(0.5f, 0.5f);
            winRt.anchorMax = new Vector2(0.5f, 0.5f);
            winRt.sizeDelta = new Vector2(850f, 555f); // Un poco más alto para pestañas y monedas
            winRt.anchoredPosition = Vector2.zero;

            // --- HEADER PANEL (Título, subtítulo, y monedas) ---
            GameObject header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(window.transform, false);
            RectTransform headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.sizeDelta = new Vector2(0f, 80f);
            headerRt.anchoredPosition = new Vector2(0f, -40f);

            // Título
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(header.transform, false);
            TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
            titleText.text = "TIENDA DE LA ALDEA";
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(1f, 0.82f, 0.2f, 1f); // Dorado brillante
            titleText.alignment = TextAlignmentOptions.Left;

            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(0.6f, 1f);
            titleRt.sizeDelta = new Vector2(0f, 35f);
            titleRt.anchoredPosition = new Vector2(30f, -25f);

            // Subtítulo
            GameObject subObj = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(header.transform, false);
            TextMeshProUGUI subText = subObj.GetComponent<TextMeshProUGUI>();
            subText.text = "Consigue objetos para defenderte o pergaminos para misiones especiales";
            subText.fontSize = 11.5f;
            subText.color = new Color(0.6f, 0.6f, 0.7f, 1f);
            subText.alignment = TextAlignmentOptions.Left;

            RectTransform subRt = subObj.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 0f);
            subRt.anchorMax = new Vector2(0.6f, 0f);
            subRt.sizeDelta = new Vector2(0f, 25f);
            subRt.anchoredPosition = new Vector2(30f, 15f);

            // Indicador de monedas (HUD Oro)
            GameObject goldIndicator = new GameObject("GoldHUD", typeof(RectTransform), typeof(Image));
            goldIndicator.transform.SetParent(header.transform, false);
            Image goldHudBg = goldIndicator.GetComponent<Image>();
            goldHudBg.color = new Color(0.14f, 0.14f, 0.18f, 0.8f);

            Outline goldHudOutline = goldIndicator.AddComponent<Outline>();
            goldHudOutline.effectColor = new Color(1f, 0.85f, 0.2f, 0.2f);
            goldHudOutline.effectDistance = new Vector2(1f, 1f);

            RectTransform goldRt = goldIndicator.GetComponent<RectTransform>();
            goldRt.anchorMin = new Vector2(1f, 0.5f);
            goldRt.anchorMax = new Vector2(1f, 0.5f);
            goldRt.sizeDelta = new Vector2(180f, 40f);
            goldRt.anchoredPosition = new Vector2(-100f, 0f); // Dejar espacio para botón de cerrar

            // Texto oro
            GameObject goldTextObj = new GameObject("GoldText", typeof(RectTransform), typeof(TextMeshProUGUI));
            goldTextObj.transform.SetParent(goldIndicator.transform, false);
            goldText = goldTextObj.GetComponent<TextMeshProUGUI>();
            goldText.text = "Tus Monedas: 0";
            goldText.fontSize = 13.5f;
            goldText.fontStyle = FontStyles.Bold;
            goldText.color = new Color(1f, 0.85f, 0.2f, 1f);
            goldText.alignment = TextAlignmentOptions.Center;

            RectTransform goldTextRt = goldTextObj.GetComponent<RectTransform>();
            goldTextRt.anchorMin = Vector2.zero;
            goldTextRt.anchorMax = Vector2.one;
            goldTextRt.sizeDelta = Vector2.zero;
            goldTextRt.anchoredPosition = Vector2.zero;

            // Botón cerrar (X)
            GameObject closeBtnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            closeBtnObj.transform.SetParent(header.transform, false);
            Image closeImg = closeBtnObj.GetComponent<Image>();
            closeImg.color = new Color(0.75f, 0.2f, 0.2f, 0.95f);

            RectTransform closeRt = closeBtnObj.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.sizeDelta = new Vector2(35f, 35f);
            closeRt.anchoredPosition = new Vector2(-30f, 0f);

            GameObject closeTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeTextObj.transform.SetParent(closeBtnObj.transform, false);
            TextMeshProUGUI closeText = closeTextObj.GetComponent<TextMeshProUGUI>();
            closeText.text = "×";
            closeText.fontSize = 24;
            closeText.color = Color.white;
            closeText.alignment = TextAlignmentOptions.Center;

            RectTransform closeTextRt = closeTextObj.GetComponent<RectTransform>();
            closeTextRt.anchorMin = Vector2.zero;
            closeTextRt.anchorMax = Vector2.one;
            closeTextRt.sizeDelta = Vector2.zero;
            closeTextRt.anchoredPosition = Vector2.zero;

            Button closeBtn = closeBtnObj.GetComponent<Button>();
            closeBtn.onClick.AddListener(CerrarTienda);

            // --- NAVEGACIÓN POR PESTAÑAS (TABS) ---
            GameObject tabContainer = new GameObject("TabContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabContainer.transform.SetParent(window.transform, false);
            
            HorizontalLayoutGroup tabHlg = tabContainer.GetComponent<HorizontalLayoutGroup>();
            tabHlg.spacing = 10;
            tabHlg.childControlWidth = true;
            tabHlg.childControlHeight = true;
            tabHlg.childForceExpandWidth = true;
            tabHlg.childForceExpandHeight = true;

            RectTransform tabContainerRt = tabContainer.GetComponent<RectTransform>();
            tabContainerRt.anchorMin = new Vector2(0f, 1f);
            tabContainerRt.anchorMax = new Vector2(1f, 1f);
            tabContainerRt.sizeDelta = new Vector2(-60f, 40f);
            tabContainerRt.anchoredPosition = new Vector2(0f, -105f);

            // Botón Pestaña 1: Pergaminos
            GameObject tabScrolls = new GameObject("Tab_Scrolls", typeof(RectTransform), typeof(Image), typeof(Button));
            tabScrolls.transform.SetParent(tabContainer.transform, false);
            tabScrollsBg = tabScrolls.GetComponent<Image>();
            
            GameObject scrollsTabTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            scrollsTabTextObj.transform.SetParent(tabScrolls.transform, false);
            tabScrollsText = scrollsTabTextObj.GetComponent<TextMeshProUGUI>();
            tabScrollsText.text = "📜 PERGAMINOS DE CONTRATO";
            tabScrollsText.fontSize = 12.5f;
            tabScrollsText.fontStyle = FontStyles.Bold;
            tabScrollsText.alignment = TextAlignmentOptions.Center;
            
            RectTransform scrollsTextRt = scrollsTabTextObj.GetComponent<RectTransform>();
            scrollsTextRt.anchorMin = Vector2.zero;
            scrollsTextRt.anchorMax = Vector2.one;
            scrollsTextRt.sizeDelta = Vector2.zero;

            Button btnTabScrolls = tabScrolls.GetComponent<Button>();
            btnTabScrolls.onClick.AddListener(() => SwitchTab(0));

            // Botón Pestaña 2: Consumibles
            GameObject tabConsumables = new GameObject("Tab_Consumibles", typeof(RectTransform), typeof(Image), typeof(Button));
            tabConsumables.transform.SetParent(tabContainer.transform, false);
            tabConsumablesBg = tabConsumables.GetComponent<Image>();

            GameObject consumablesTabTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            consumablesTabTextObj.transform.SetParent(tabConsumables.transform, false);
            tabConsumablesText = consumablesTabTextObj.GetComponent<TextMeshProUGUI>();
            tabConsumablesText.text = "🧪 OBJETOS CONSUMIBLES";
            tabConsumablesText.fontSize = 12.5f;
            tabConsumablesText.fontStyle = FontStyles.Bold;
            tabConsumablesText.alignment = TextAlignmentOptions.Center;

            RectTransform consumablesTextRt = consumablesTabTextObj.GetComponent<RectTransform>();
            consumablesTextRt.anchorMin = Vector2.zero;
            consumablesTextRt.anchorMax = Vector2.one;
            consumablesTextRt.sizeDelta = Vector2.zero;

            Button btnTabConsumables = tabConsumables.GetComponent<Button>();
            btnTabConsumables.onClick.AddListener(() => SwitchTab(1));

            // --- ÁREA DEL SCROLL VIEW ---
            GameObject scrollArea = new GameObject("ScrollArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            scrollArea.transform.SetParent(window.transform, false);
            Image scrollBg = scrollArea.GetComponent<Image>();
            scrollBg.color = new Color(0f, 0f, 0f, 0.25f);

            RectTransform scrollRt = scrollArea.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.sizeDelta = new Vector2(-60f, -220f);
            scrollRt.anchoredPosition = new Vector2(0f, -40f);

            ScrollRect scrollRect = scrollArea.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Viewport
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewport.transform.SetParent(scrollArea.transform, false);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            Image viewImg = viewport.GetComponent<Image>();
            viewImg.color = new Color(1f, 1f, 1f, 0.01f);

            RectTransform viewRt = viewport.GetComponent<RectTransform>();
            viewRt.anchorMin = Vector2.zero;
            viewRt.anchorMax = Vector2.one;
            viewRt.sizeDelta = Vector2.zero;
            viewRt.anchoredPosition = Vector2.zero;

            scrollRect.viewport = viewRt;

            // Content Container
            contentContainer = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentContainer.transform.SetParent(viewport.transform, false);

            RectTransform contentRt = contentContainer.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 0f);
            contentRt.pivot = new Vector2(0.5f, 1f);

            VerticalLayoutGroup vlg = contentContainer.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(15, 15, 15, 15);
            vlg.spacing = 10;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;

            ContentSizeFitter csf = contentContainer.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRt;

            // Configurar primer estado visual de pestañas
            UpdateTabHeadersVisuals();

            // Rellenar items iniciales
            PopulateCurrentTabItems();

            // Iniciar apagado
            panelRoot.SetActive(false);
        }

        private void SwitchTab(int newTab)
        {
            if (activeTab == newTab) return;
            activeTab = newTab;
            
            UpdateTabHeadersVisuals();
            PopulateCurrentTabItems();
            ActualizarBotonesCompra();
        }

        private void UpdateTabHeadersVisuals()
        {
            if (activeTab == 0) // Pergaminos activo
            {
                tabScrollsBg.color = new Color(0.85f, 0.65f, 0.15f, 0.85f); // Dorado brillante
                tabScrollsText.color = Color.black;

                tabConsumablesBg.color = new Color(0.14f, 0.14f, 0.18f, 0.8f);
                tabConsumablesText.color = new Color(0.7f, 0.7f, 0.8f, 1f);
            }
            else // Consumibles activo
            {
                tabScrollsBg.color = new Color(0.14f, 0.14f, 0.18f, 0.8f);
                tabScrollsText.color = new Color(0.7f, 0.7f, 0.8f, 1f);

                tabConsumablesBg.color = new Color(0.15f, 0.6f, 0.75f, 0.85f); // Azul/Cian místico
                tabConsumablesText.color = Color.black;
            }
        }

        private void PopulateCurrentTabItems()
        {
            // Limpiar anteriores
            foreach (Transform child in contentContainer.transform)
            {
                Destroy(child.gameObject);
            }
            spawnedItems.Clear();

            List<ScrollItemData> currentList = (activeTab == 0) ? itemsPergaminos : itemsConsumibles;

            foreach (var item in currentList)
            {
                // Card de item
                GameObject itemCard = new GameObject("Card_" + item.nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup));
                itemCard.transform.SetParent(contentContainer.transform, false);
                Image cardBg = itemCard.GetComponent<Image>();
                cardBg.color = new Color(0.13f, 0.13f, 0.17f, 1f);
                
                // Outline para darle un aspecto más pulido y premium
                Outline cardOutline = itemCard.AddComponent<Outline>();
                cardOutline.effectColor = (activeTab == 0) 
                    ? new Color(0.85f, 0.65f, 0.15f, 0.15f) // Borde dorado sutil
                    : new Color(0.15f, 0.6f, 0.75f, 0.15f); // Borde azul sutil
                cardOutline.effectDistance = new Vector2(1f, 1f);

                HorizontalLayoutGroup hlg = itemCard.GetComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(20, 20, 10, 10);
                hlg.spacing = 20;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandHeight = true;
                hlg.childForceExpandWidth = false;

                LayoutElement leCard = itemCard.AddComponent<LayoutElement>();
                leCard.preferredHeight = 90f;

                // Contenedor texto
                GameObject textContainer = new GameObject("TextContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
                textContainer.transform.SetParent(itemCard.transform, false);
                VerticalLayoutGroup vlgText = textContainer.GetComponent<VerticalLayoutGroup>();
                vlgText.spacing = 4;
                vlgText.childControlHeight = true;
                vlgText.childControlWidth = true;
                vlgText.childForceExpandHeight = false;
                vlgText.childForceExpandWidth = true;

                LayoutElement leText = textContainer.AddComponent<LayoutElement>();
                leText.flexibleWidth = 1f;

                // Nombre del item
                GameObject nameObj = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
                nameObj.transform.SetParent(textContainer.transform, false);
                TextMeshProUGUI nameText = nameObj.GetComponent<TextMeshProUGUI>();
                
                if (activeTab == 0)
                {
                    nameText.text = $"📜 {item.nombre}";
                    nameText.color = new Color(1f, 0.85f, 0.3f, 1f); // Dorado cálido
                }
                else
                {
                    nameText.text = $"🧪 {item.nombre}";
                    nameText.color = new Color(0.3f, 0.85f, 1f, 1f); // Cian místico
                }
                
                nameText.fontSize = 16;
                nameText.fontStyle = FontStyles.Bold;

                // Descripción
                GameObject descObj = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
                descObj.transform.SetParent(textContainer.transform, false);
                TextMeshProUGUI descText = descObj.GetComponent<TextMeshProUGUI>();
                descText.text = item.descripcion;
                descText.fontSize = 11f;
                descText.color = new Color(0.7f, 0.7f, 0.75f, 1f);
                descText.textWrappingMode = TextWrappingModes.Normal;
                descText.overflowMode = TextOverflowModes.Ellipsis;

                // Contenedor acción (Derecha)
                GameObject actionContainer = new GameObject("ActionContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
                actionContainer.transform.SetParent(itemCard.transform, false);
                VerticalLayoutGroup vlgAction = actionContainer.GetComponent<VerticalLayoutGroup>();
                vlgAction.spacing = 5;
                vlgAction.childAlignment = TextAnchor.MiddleRight;
                vlgAction.childControlHeight = true;
                vlgAction.childControlWidth = true;
                vlgAction.childForceExpandHeight = false;
                vlgAction.childForceExpandWidth = true;

                LayoutElement leAction = actionContainer.AddComponent<LayoutElement>();
                leAction.preferredWidth = 120f;

                // Precio (Badge)
                GameObject priceObj = new GameObject("Price", typeof(RectTransform), typeof(TextMeshProUGUI));
                priceObj.transform.SetParent(actionContainer.transform, false);
                TextMeshProUGUI priceText = priceObj.GetComponent<TextMeshProUGUI>();
                priceText.text = $"🪙 {item.coste} Oro";
                priceText.fontSize = 14;
                priceText.fontStyle = FontStyles.Bold;
                priceText.color = new Color(1f, 0.85f, 0.2f, 1f);
                priceText.alignment = TextAlignmentOptions.Right;

                // Botón comprar
                GameObject buyBtnObj = new GameObject("BuyButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                buyBtnObj.transform.SetParent(actionContainer.transform, false);
                
                LayoutElement leBuy = buyBtnObj.AddComponent<LayoutElement>();
                leBuy.preferredHeight = 30f;

                Image buyImg = buyBtnObj.GetComponent<Image>();
                buyImg.color = new Color(0.2f, 0.6f, 0.3f, 1f);

                // Outline en botón de compra
                Outline buyBtnOutline = buyBtnObj.AddComponent<Outline>();
                buyBtnOutline.effectColor = new Color(1f, 1f, 1f, 0.1f);
                buyBtnOutline.effectDistance = new Vector2(1f, 1f);

                GameObject buyTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                buyTextObj.transform.SetParent(buyBtnObj.transform, false);
                TextMeshProUGUI buyText = buyTextObj.GetComponent<TextMeshProUGUI>();
                buyText.text = "ADQUIRIR";
                buyText.fontSize = 11.5f;
                buyText.fontStyle = FontStyles.Bold;
                buyText.color = Color.white;
                buyText.alignment = TextAlignmentOptions.Center;

                RectTransform buyTextRt = buyTextObj.GetComponent<RectTransform>();
                buyTextRt.anchorMin = Vector2.zero;
                buyTextRt.anchorMax = Vector2.one;
                buyTextRt.sizeDelta = Vector2.zero;
                buyTextRt.anchoredPosition = Vector2.zero;

                Button buyBtn = buyBtnObj.GetComponent<Button>();
                TipoObjeto tipoObj = item.tipo;
                int coste = item.coste;
                buyBtn.onClick.AddListener(() => ComprarObjeto(tipoObj, coste));

                // Registrar item
                ShopItemUI shopItemUI = new ShopItemUI();
                shopItemUI.tipo = item.tipo;
                shopItemUI.coste = item.coste;
                shopItemUI.botonCompra = buyBtn;
                shopItemUI.textoPrecio = priceText;
                shopItemUI.backgroundCard = cardBg;
                spawnedItems.Add(shopItemUI);
            }
        }

        public void AbrirTienda(GameObject player)
        {
            if (PanelActivo) return;

            playerLocal = player;
            PanelActivo = true;
            panelRoot.SetActive(true);

            LockPlayerControl(true);

            // Vincular evento de cambio de monedas
            PlayerInventory inv = playerLocal.GetComponent<PlayerInventory>();
            if (inv != null)
            {
                inv.monedas.OnValueChanged += OnPlayerGoldChanged;
                ActualizarGoldText(inv.monedas.Value);
            }

            // Forzar pestaña inicial a Pergaminos
            activeTab = 0;
            UpdateTabHeadersVisuals();
            PopulateCurrentTabItems();
            ActualizarBotonesCompra();
        }

        public void CerrarTienda()
        {
            if (!PanelActivo) return;

            // Desvincular evento para evitar Memory Leaks (Regra #28)
            if (playerLocal != null)
            {
                PlayerInventory inv = playerLocal.GetComponent<PlayerInventory>();
                if (inv != null)
                {
                    inv.monedas.OnValueChanged -= OnPlayerGoldChanged;
                }
            }

            PanelActivo = false;
            panelRoot.SetActive(false);

            LockPlayerControl(false);
            playerLocal = null;
        }

        private void OnPlayerGoldChanged(int oldVal, int newVal)
        {
            ActualizarGoldText(newVal);
            ActualizarBotonesCompra();
        }

        private void ActualizarGoldText(int amount)
        {
            if (goldText != null)
            {
                goldText.text = $"💰 Tu Oro: {amount}";
            }
        }

        private void ComprarObjeto(TipoObjeto objectType, int coste)
        {
            if (playerLocal == null) return;

            Debug.Log($"[ShopPanel] Intentando comprar {objectType} por {coste} oro...");
            PlayerInventory inv = playerLocal.GetComponent<PlayerInventory>();
            if (inv != null && inv.monedas.Value >= coste)
            {
                inv.ComprarObjetoServerRpc(objectType, coste);

                GameplayUI ui = Object.FindAnyObjectByType<GameplayUI>();
                if (ui != null)
                {
                    ui.MostrarMensajeTarea($"¡Has adquirido {objectType}!", 3f);
                }
                
                Debug.Log("[ShopPanel] Compra realizada exitosamente. Cerrando tienda...");
            }
            else
            {
                Debug.LogWarning("[ShopPanel] No tienes suficiente oro.");
            }

            CerrarTienda();
        }

        private void ActualizarBotonesCompra()
        {
            if (playerLocal == null) return;

            PlayerInventory inv = playerLocal.GetComponent<PlayerInventory>();
            int playerCoins = inv != null ? inv.monedas.Value : 0;

            foreach (var item in spawnedItems)
            {
                bool tieneOro = playerCoins >= item.coste;
                item.botonCompra.interactable = tieneOro;
                
                // Color y feedback visual
                if (tieneOro)
                {
                    item.botonCompra.GetComponent<Image>().color = new Color(0.18f, 0.58f, 0.28f, 1f); // Verde pulido
                    item.textoPrecio.color = new Color(1f, 0.85f, 0.2f, 1f); // Dorado brillante
                    item.backgroundCard.color = new Color(0.13f, 0.13f, 0.18f, 1f); // Fondo regular
                }
                else
                {
                    item.botonCompra.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.28f, 0.6f); // Gris apagado
                    item.textoPrecio.color = new Color(0.6f, 0.5f, 0.4f, 1f); // Dorado apagado / grisáceo
                    item.backgroundCard.color = new Color(0.1f, 0.1f, 0.13f, 0.9f); // Fondo ligeramente oscurecido
                }
            }
        }

        private void LockPlayerControl(bool lockControl)
        {
            if (playerLocal == null) return;

            // Pausar movimiento del personaje de forma segura
            var tpc = playerLocal.GetComponent<StarterAssets.ThirdPersonController>();
            if (tpc != null) tpc.enabled = !lockControl;

            // Desactivar controles de ratón de StarterAssets
            var inputs = playerLocal.GetComponent<StarterAssets.StarterAssetsInputs>();
            if (inputs != null)
            {
                inputs.cursorLocked = !lockControl;
                inputs.cursorInputForLook = !lockControl;
            }

            if (lockControl)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
