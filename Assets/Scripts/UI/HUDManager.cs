using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    private static HUDManager instance;
    public static HUDManager Instance => instance;

    [Header("References")]
    public GameManager3D gameManager;

    [Header("Top Panel")]
    public Text dayText;
    public Text moneyText;
    public Text ratingText;
    public Text roversStatusText;

    [Header("Orders Panel")]
    public GameObject ordersPanel;
    public Text ordersTitleText;
    public Transform ordersListParent;
    public GameObject orderListItemPrefab;
    public Button ordersCloseButton;

    [Header("Stats Panel")]
    public GameObject statsPanel;
    public Text statsTitleText;
    public Text statsTotalOrdersText;
    public Text statsCompletedText;
    public Text statsFailedText;
    public Text statsMoneyEarnedText;
    public Text statsRoversLostText;
    public Text statsDaysSurvivedText;
    public Button statsCloseButton;

    [Header("Rovers Status Panel")]
    public GameObject roversStatusPanel;
    public Text roversStatusTitleText;
    public Transform roversStatusListParent;
    public GameObject roverStatusItemPrefab;
    public Button roversStatusCloseButton;

    [Header("Settings Panel")]
    public GameObject settingsPanel;
    public Text settingsTitleText;
    public Slider volumeSlider;
    public Text volumeValueText;
    public Dropdown resolutionDropdown;
    public Button exitGameButton;
    public Button settingsCloseButton;
    private Resolution[] resolutions;

    [Header("Buttons")]
    public Button menuButton;
    public Button ordersButton;
    public Button statusButton;
    public Button statsButton;
    public Button settingsButton;

    [Header("Menu")]
    public GameObject menuPanel;
    public GameObject sharedOverlay;
    public float animationDuration = 0.3f;

    [Header("Base Panel")]
    public MoonBasePanelUI basePanel;

    private bool isMenuOpen = false;
    private RectTransform menuRect;
    private Vector2 menuClosedPos;
    private Vector2 menuOpenPos;
    private Coroutine animCoroutine;
    private OrderPanelUI orderPanel;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        /* Base Panel */
        if (basePanel != null && basePanel.panel != null)
            basePanel.panel.SetActive(false);

        /* Resolution Dropdown */
        resolutions = Screen.resolutions;
        Debug.Log($"Resolutions count: {resolutions.Length}");

        if (resolutionDropdown != null)
        {
            Debug.Log("ResolutionDropdown found!");
            resolutionDropdown.ClearOptions();
            List<string> options = new List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = resolutions[i].width + "x" + resolutions[i].height;
                options.Add(option);
                Debug.Log($"Added resolution: {option}");

                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            Debug.Log($"Dropdown populated with {options.Count} options");
        }
        else
        {
            Debug.LogError("ResolutionDropdown is null!");
        }


        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager3D>();

        orderPanel = FindFirstObjectByType<OrderPanelUI>();

        /* Orders Panel */
        if (ordersPanel != null)
            ordersPanel.SetActive(false);

        if (ordersCloseButton != null)
            ordersCloseButton.onClick.AddListener(CloseOrdersPanel);

        /* Stats Panel */
        if (statsPanel != null)
        {
            statsPanel.SetActive(false);
            Debug.Log("Stats panel disabled at start");
        }

        /* Rovers Status Panel */
        if (roversStatusPanel != null)
        {
            roversStatusPanel.SetActive(false);
            Debug.Log("Rovers status panel disabled at start");
        }

        /* Settings Panel */
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (settingsCloseButton != null)
            settingsCloseButton.onClick.AddListener(CloseSettingsPanel);

        if (exitGameButton != null)
            exitGameButton.onClick.AddListener(ExitGame);

        /* Volume Slider */
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            volumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
            UpdateVolumeText(volumeSlider.value);
        }

        /* Resolution Dropdown */
        resolutions = Screen.resolutions;
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            List<string> options = new List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = resolutions[i].width + "x" + resolutions[i].height;
                options.Add(option);

                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }

        if (statsCloseButton != null)
            statsCloseButton.onClick.AddListener(CloseStatsPanel);

        if (roversStatusCloseButton != null)
            roversStatusCloseButton.onClick.AddListener(CloseRoversStatusPanel);

        if (menuPanel != null)
        {
            menuRect = menuPanel.GetComponent<RectTransform>();
            float width = 300f;
            if (menuRect != null && menuRect.rect.width > 0)
                width = menuRect.rect.width;

            menuClosedPos = new Vector2(-width, 0);
            menuOpenPos = Vector2.zero;
            menuRect.anchoredPosition = menuClosedPos;
            menuPanel.SetActive(false);
        }

        if (sharedOverlay != null)
        {
            Image img = sharedOverlay.GetComponent<Image>();
            if (img == null)
            {
                img = sharedOverlay.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0);
            }
            img.raycastTarget = true;

            Button overlayBtn = sharedOverlay.GetComponent<Button>();
            if (overlayBtn == null)
                overlayBtn = sharedOverlay.AddComponent<Button>();

            overlayBtn.onClick.RemoveAllListeners();
            overlayBtn.onClick.AddListener(OnOverlayClick);
            sharedOverlay.SetActive(false);
        }

        if (menuButton != null)
            menuButton.onClick.AddListener(ToggleMenu);

        if (ordersButton != null)
            ordersButton.onClick.AddListener(() => { OnOrdersClick(); CloseMenu(); });

        if (statusButton != null)
            statusButton.onClick.AddListener(() => { OpenRoversStatusPanel(); CloseMenu(); });

        if (statsButton != null)
            statsButton.onClick.AddListener(() => { OpenStatsPanel(); CloseMenu(); });

        if (settingsButton != null)
            settingsButton.onClick.AddListener(() => { OnSettingsClick(); CloseMenu(); });

        UpdateUI();
    }

    void Update()
    {
        if (Time.frameCount % 30 == 0)
            UpdateUI();
    }

    public void UpdateUI()
    {
        if (gameManager == null) return;
        var progress = gameManager.GetProgress();
        if (progress == null) return;

        if (dayText != null)
            dayText.text = $"Day {progress.Day}";

        if (moneyText != null)
            moneyText.text = $"💰 {progress.Money}";

        if (ratingText != null)
            ratingText.text = $"⭐ {progress.BaseRating:F0}%";

        if (roversStatusText != null)
        {
            int total = progress.Rovers.Count;
            int alive = 0;
            int available = 0;
            int destroyed = 0;

            foreach (var rover in progress.Rovers)
            {
                if (rover.IsDestroyed)
                {
                    destroyed++;
                }
                else
                {
                    alive++;
                    if (!rover.IsBusy) available++;
                }
            }

            roversStatusText.text = $"🚀 {alive}/{total}";

            if (available > 0)
                roversStatusText.color = Color.green;
            else
                roversStatusText.color = Color.red;
        }
    }

    #region ORDERS PANEL
        public void OpenOrdersPanel()
        {
            Debug.Log("OpenOrdersPanel called");

            if (ordersPanel == null)
            {
                Debug.LogError("ordersPanel is null!");
                return;
            }

            CloseAllSubPanels();
            UpdateOrdersPanel();
            ordersPanel.SetActive(true);
            ShowOverlay();

            Debug.Log($"Orders panel active: {ordersPanel.activeSelf}");
        }

        public void CloseOrdersPanel()
        {
            Debug.Log("CloseOrdersPanel called");

            if (ordersPanel != null)
                ordersPanel.SetActive(false);

            HideOverlay();
        }

        public void UpdateOrdersPanel()
        {
        Debug.Log("UpdateOrdersPanel called");

        var progress = gameManager.GetProgress();
        if (progress == null) return;

        if (ordersListParent == null)
        {
            Debug.LogError("ordersListParent is null!");
            return;
        }

        if (orderListItemPrefab == null)
        {
            Debug.LogError("orderListItemPrefab is null!");
            return;
        }

        foreach (Transform child in ordersListParent)
            Destroy(child.gameObject);

        int count = 0;
        foreach (var order in progress.Orders)
        {
            GameObject item = Instantiate(orderListItemPrefab, ordersListParent);

            Button btn = item.GetComponent<Button>();
            if (btn == null)
                btn = item.AddComponent<Button>();

            OrderData localOrder = order;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                Debug.Log($"Order clicked in list: {localOrder.Title}");
                OnOrderListItemClicked(localOrder);
            });

            CanvasGroup cg = item.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = item.AddComponent<CanvasGroup>();

            if (order.IsCompleted || order.IsFailed)
            {
                cg.alpha = 0.5f;
                btn.interactable = false; 
            }
            else
            {
                cg.alpha = 1f;
                btn.interactable = true;
            }

            Text titleText = item.transform.Find("TitleText")?.GetComponent<Text>();
            Text infoText = item.transform.Find("InfoText")?.GetComponent<Text>();
            Text statusText = item.transform.Find("StatusText")?.GetComponent<Text>();

            if (titleText != null)
                titleText.text = order.Title;

            if (infoText != null)
                infoText.text = $"{order.Weight:F0}kg | {order.Reward} credits";

            if (statusText != null)
            {
                if (order.IsCompleted)
                {
                    statusText.text = "Done";
                    statusText.color = Color.white;
                }
                else if (order.IsFailed)
                {
                    statusText.text = "Failed";
                    statusText.color = Color.red;
                }
                else if (order.IsBusy)
                {
                    statusText.text = "In progress";
                    statusText.color = Color.yellow;
                }
                else
                {
                    statusText.text = "Open";
                    statusText.color = Color.green;
                }
            }

            count++;
        }

        Debug.Log($"Orders panel updated: {count} orders");
    }

    /* ===== ORDER LIST ITEM CLICK ===== */

    void OnOrderListItemClicked(OrderData order)
    {
        Debug.Log($"Order list item clicked: {order.Title}");

        if (order == null) return;

        if (order.IsCompleted || order.IsFailed || order.IsBusy)
        {
            Debug.Log($"Order {order.Title} is not available");
            return;
        }

        CloseOrdersPanel();

        if (gameManager != null)
        {
            gameManager.SelectOrder(order);
        }
    }

    #endregion

    #region STATS PANEL

    public void OpenStatsPanel()
    {
        Debug.Log("OpenStatsPanel called");

        if (statsPanel == null)
        {
            Debug.LogError("StatsPanel is null!");
            return;
        }

        CloseAllSubPanels();
        UpdateStatsPanel();
        statsPanel.SetActive(true);
        ShowOverlay();

        Debug.Log($"Stats panel active: {statsPanel.activeSelf}");
    }

    public void CloseStatsPanel()
    {
        Debug.Log("CloseStatsPanel called");

        if (statsPanel != null)
            statsPanel.SetActive(false);

        HideOverlay();
    }

    void UpdateStatsPanel()
    {
        var progress = gameManager.GetProgress();
        if (progress == null) return;

        int total = progress.TotalDeliveriesCompleted + progress.TotalDeliveriesFailed;
        int roversLost = 0;
        foreach (var rover in progress.Rovers)
        {
            if (rover.IsDestroyed) roversLost++;
        }

        if (statsTitleText != null)
            statsTitleText.text = "Stats";

        if (statsTotalOrdersText != null)
            statsTotalOrdersText.text = $"Total Orders: {total}";

        if (statsCompletedText != null)
            statsCompletedText.text = $"Completed: {progress.TotalDeliveriesCompleted}";

        if (statsFailedText != null)
            statsFailedText.text = $"Failed: {progress.TotalDeliveriesFailed}";

        if (statsMoneyEarnedText != null)
            statsMoneyEarnedText.text = $"Money Earned: {progress.Money}";

        if (statsRoversLostText != null)
            statsRoversLostText.text = $"Rovers Lost: {roversLost}";

        if (statsDaysSurvivedText != null)
            statsDaysSurvivedText.text = $"Days Survived: {progress.Day}";
    }

#endregion

    #region ROVERS STATUS PANEL

    void UpdateRoversStatusPanel()
    {
        Debug.Log("UpdateRoversStatusPanel called");

        var progress = gameManager.GetProgress();
        if (progress == null) return;

        if (roversStatusListParent == null)
        {
            Debug.LogError("roversStatusListParent is null!");
            return;
        }

        if (roverStatusItemPrefab == null)
        {
            Debug.LogError("roverStatusItemPrefab is null!");
            return;
        }

        foreach (Transform child in roversStatusListParent)
            Destroy(child.gameObject);

        int count = 0;
        foreach (var rover in progress.Rovers)
        {
            if (rover.IsDestroyed) continue;

            GameObject item = Instantiate(roverStatusItemPrefab, roversStatusListParent);

            Text nameText = item.transform.Find("NameText")?.GetComponent<Text>();
            Text batteryText = item.transform.Find("InfoRow/BatteryText")?.GetComponent<Text>();
            Text capacityText = item.transform.Find("InfoRow/CapacityText")?.GetComponent<Text>();
            Text statusText = item.transform.Find("InfoRow/StatusText")?.GetComponent<Text>();

            if (nameText != null)
                nameText.text = $"🚀 {rover.Name}";

            if (batteryText != null)
                batteryText.text = $"🔋 {rover.CurrentBattery:F0}/{rover.MaxBattery:F0}";

            if (capacityText != null)
                capacityText.text = $"📦 {rover.CargoCapacity} kg";

            if (statusText != null)
            {
                statusText.text = rover.IsBusy ? "⏳ Busy" : "✅ Idle";
                statusText.color = rover.IsBusy ? Color.yellow : Color.green;
            }

            count++;
        }

        Debug.Log($"Rovers status panel updated: {count} rovers");
    }

    public void OpenRoversStatusPanel()
    {
        Debug.Log("OpenRoversStatusPanel called");

        if (roversStatusPanel == null)
        {
            Debug.LogError("roversStatusPanel is null!");
            return;
        }

        CloseAllSubPanels();
        UpdateRoversStatusPanel();
        roversStatusPanel.SetActive(true);
        ShowOverlay();

        Debug.Log($"Rovers status panel active: {roversStatusPanel.activeSelf}");
    }

    public void CloseRoversStatusPanel()
    {
        Debug.Log("CloseRoversStatusPanel called");

        if (roversStatusPanel != null)
            roversStatusPanel.SetActive(false);

        HideOverlay();
    }

    #endregion

    #region SETTINGS PANEL

    public void OpenSettingsPanel()
    {
        Debug.Log("OpenSettingsPanel called");

        if (settingsPanel == null)
        {
            Debug.LogError("settingsPanel is null!");
            return;
        }

        CloseAllSubPanels();

        PopulateResolutionDropdown();

        if (volumeSlider != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
            UpdateVolumeText(volumeSlider.value);
        }

        settingsPanel.SetActive(true);
        ShowOverlay();

        Debug.Log($"Settings panel active: {settingsPanel.activeSelf}");
    }

    void PopulateResolutionDropdown()
    {
        if (resolutionDropdown == null)
        {
            Debug.LogError("ResolutionDropdown is null!");
            return;
        }

        resolutions = Screen.resolutions;

        if (resolutions == null || resolutions.Length == 0)
        {
            Debug.LogWarning("No resolutions found, using test values");
            resolutions = new Resolution[]
            {
            new Resolution { width = 1920, height = 1080 },
            new Resolution { width = 1280, height = 720 },
            new Resolution { width = 1024, height = 768 }
            };
        }

        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + "x" + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;

        Debug.Log($"Dropdown populated with {options.Count} options");
    }

    public void CloseSettingsPanel()
    {
        Debug.Log("CloseSettingsPanel called");

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        HideOverlay();
    }

    /* ===== SETTINGS EVENTS ===== */

    void UpdateVolumeText(float value)
    {
        if (volumeValueText != null)
            volumeValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    public void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
        UpdateVolumeText(value);
        Debug.Log($"Volume: {Mathf.RoundToInt(value * 100)}%");
    }

    public void OnResolutionChanged(int index)
    {
        if (resolutions == null || index >= resolutions.Length) return;

        Resolution resolution = resolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        Debug.Log($"Resolution: {resolution.width}x{resolution.height}");
    }

    public void ExitGame()
    {
        Debug.Log("Exit game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    #endregion

    #region OVERLAY

    public void ShowOverlay()
    {
        if (sharedOverlay != null && !sharedOverlay.activeSelf)
        {
            sharedOverlay.SetActive(true);
            Debug.Log("Overlay shown");
        }
    }

    public void HideOverlay()
    {
        bool menuOpen = isMenuOpen;
        bool orderOpen = orderPanel != null && orderPanel.IsPanelOpen();
        bool statsOpen = statsPanel != null && statsPanel.activeSelf;
        bool roversStatusOpen = roversStatusPanel != null && roversStatusPanel.activeSelf;
        bool ordersOpen = ordersPanel != null && ordersPanel.activeSelf;
        bool settingsOpen = settingsPanel != null && settingsPanel.activeSelf;

        if (!menuOpen && !orderOpen && !statsOpen && !roversStatusOpen && !ordersOpen && !settingsOpen)
        {
            if (sharedOverlay != null && sharedOverlay.activeSelf)
            {
                sharedOverlay.SetActive(false);
                Debug.Log("Overlay hidden");
            }
        }
    }

    #endregion

    #region MENU

    void ToggleMenu()
    {
        if (isMenuOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    void OpenMenu()
    {
        if (isMenuOpen) return;
        isMenuOpen = true;

        ShowOverlay();

        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
            if (menuRect != null)
            {
                menuRect.anchoredPosition = menuClosedPos;
                if (animCoroutine != null) StopCoroutine(animCoroutine);
                animCoroutine = StartCoroutine(AnimateMenu(menuClosedPos, menuOpenPos, false));
            }
        }
    }

    void CloseMenu()
    {
        if (!isMenuOpen) return;
        isMenuOpen = false;

        if (menuRect != null)
        {
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(AnimateMenu(menuRect.anchoredPosition, menuClosedPos, true));
        }
        else
        {
            HideMenuImmediate();
        }
    }

    IEnumerator AnimateMenu(Vector2 start, Vector2 end, bool hideOnComplete = false)
    {
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            float smoothT = t * t * (3f - 2f * t);

            if (menuRect != null)
                menuRect.anchoredPosition = Vector2.Lerp(start, end, smoothT);

            yield return null;
        }

        if (menuRect != null)
            menuRect.anchoredPosition = end;

        if (hideOnComplete)
            HideMenuImmediate();

        animCoroutine = null;
    }

    #endregion

    public bool IsOrdersPanelOpen()
    {
        return ordersPanel != null && ordersPanel.activeSelf;
    }

    void HideMenuImmediate()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);
        HideOverlay();
    }

    void CloseAllSubPanels()
    {
        if (statsPanel != null) statsPanel.SetActive(false);
        if (roversStatusPanel != null) roversStatusPanel.SetActive(false);
        if (ordersPanel != null) ordersPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (basePanel != null)
        {
            basePanel.ClosePanel();
        }
    }

    #region BUTTON HANDLERS

        void OnOverlayClick()
        {
            if (statsPanel != null && statsPanel.activeSelf)
            {
                CloseStatsPanel();
                return;
            }

            if (roversStatusPanel != null && roversStatusPanel.activeSelf)
            {
                CloseRoversStatusPanel();
                return;
            }

            if (ordersPanel != null && ordersPanel.activeSelf)
            {
                CloseOrdersPanel();
                return;
            }

            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettingsPanel();
                return;
            }

        if (isMenuOpen)
            {
                CloseMenu();
            }
        }

        void OnOrdersClick()
        {
            Debug.Log("Orders button clicked");
            OpenOrdersPanel();
            CloseMenu();
        }

        void OnSettingsClick()
        {
            Debug.Log("Settings button clicked");
            OpenSettingsPanel();
            CloseMenu();
        }

    #endregion
}