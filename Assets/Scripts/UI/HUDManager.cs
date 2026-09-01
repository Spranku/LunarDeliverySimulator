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
    public Dropdown resolutionDropdown;
    public Button exitGameButton;
    public Button newGameButton;
    public Button settingsCloseButton;
    private Resolution[] resolutions;

    [Header("Base Panel")]
    public MoonBasePanelUI basePanel;

    [Header("Buttons")]
    public Button menuButton;
    public Button ordersButton;
    public Button statusButton;
    public Button statsButton;
    public Button settingsButton;
    public Button moonBaseButton;

    [Header("Menu")]
    public GameObject menuPanel;
    public GameObject sharedOverlay;
    public float animationDuration = 0.3f;

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
        if (basePanel == null)
            basePanel = FindFirstObjectByType<MoonBasePanelUI>();

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager3D>();

        orderPanel = FindFirstObjectByType<OrderPanelUI>();

        InitOrdersPanel();
        InitStatsPanel();
        InitRoversStatusPanel();
        InitSettingsPanel();
        InitMenu();
        InitResolutionDropdown();

        if (menuButton != null)
            menuButton.onClick.AddListener(ToggleMenu);

        if (ordersButton != null)
            ordersButton.onClick.AddListener(() => { OpenOrdersPanel(); CloseMenu(); });

        if (statusButton != null)
            statusButton.onClick.AddListener(() => { OpenRoversStatusPanel(); CloseMenu(); });

        if (statsButton != null)
            statsButton.onClick.AddListener(() => { OpenStatsPanel(); CloseMenu(); });

        if (settingsButton != null)
            settingsButton.onClick.AddListener(() => { OpenSettingsPanel(); CloseMenu(); });

        if (moonBaseButton != null)
            moonBaseButton.onClick.AddListener(() => { FocusOnBase(); CloseMenu(); });

        UpdateUI();
    }

    void Update()
    {
        if (Time.frameCount % 30 == 0)
            UpdateUI();
    }

    void FocusOnBase()
    {
        MoonBasePoint3D basePoint = FindFirstObjectByType<MoonBasePoint3D>();
        if (basePoint == null)
        {
            Debug.LogWarning("MoonBase not found!");
            return;
        }

        if (gameManager != null)
            gameManager.SelectBase(basePoint);
    }

    #region UI Update

    public void UpdateUI()
    {
        if (gameManager == null) return;
        var progress = gameManager.GetProgress();
        if (progress == null) return;

        if (dayText != null)
            dayText.text = $"Day {progress.Day}";

        if (moneyText != null)
            moneyText.text = $"{progress.Money}";

        if (ratingText != null)
            ratingText.text = $"{progress.BaseRating:F0}%";

        if (roversStatusText != null)
        {
            int total = progress.Rovers.Count;
            int alive = 0;
            int available = 0;
            int destroyed = 0;

            foreach (var rover in progress.Rovers)
            {
                if (rover.IsDestroyed)
                    destroyed++;
                else
                {
                    alive++;
                    if (!rover.IsBusy) available++;
                }
            }

            roversStatusText.text = $"🚀 {alive}/{total}";
            roversStatusText.color = available > 0 ? Color.green : Color.red;
        }
    }

    #endregion

    #region Orders Panel

    void InitOrdersPanel()
    {
        if (ordersPanel != null)
            ordersPanel.SetActive(false);

        if (ordersCloseButton != null)
            ordersCloseButton.onClick.AddListener(CloseOrdersPanel);
    }

    public void OpenOrdersPanel()
    {
        if (ordersPanel == null) return;

        CloseAllSubPanels();
        UpdateOrdersPanel();
        ordersPanel.SetActive(true);
        ShowOverlay();
    }

    public void CloseOrdersPanel()
    {
        if (ordersPanel != null)
            ordersPanel.SetActive(false);

        HideOverlay();
    }

    public void UpdateOrdersPanel()
    {
        if (ordersListParent == null || orderListItemPrefab == null) return;

        var progress = gameManager.GetProgress();
        if (progress == null) return;

        foreach (Transform child in ordersListParent)
            Destroy(child.gameObject);

        foreach (var order in progress.Orders)
        {
            GameObject item = Instantiate(orderListItemPrefab, ordersListParent);

            Button btn = item.GetComponent<Button>();
            if (btn == null) btn = item.AddComponent<Button>();

            OrderData localOrder = order;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnOrderListItemClicked(localOrder));

            CanvasGroup cg = item.GetComponent<CanvasGroup>();
            if (cg == null) cg = item.AddComponent<CanvasGroup>();

            bool isDone = order.IsCompleted || order.IsFailed;
            cg.alpha = isDone ? 0.5f : 1f;
            btn.interactable = !isDone;

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
        }
    }

    void OnOrderListItemClicked(OrderData order)
    {
        if (order == null || order.IsCompleted || order.IsFailed || order.IsBusy)
            return;

        CloseOrdersPanel();

        if (gameManager != null)
            gameManager.SelectOrder(order);
    }

    public bool IsOrdersPanelOpen()
    {
        return ordersPanel != null && ordersPanel.activeSelf;
    }

    #endregion

    #region Stats Panel

    void InitStatsPanel()
    {
        if (statsPanel != null)
            statsPanel.SetActive(false);

        if (statsCloseButton != null)
            statsCloseButton.onClick.AddListener(CloseStatsPanel);
    }

    public void OpenStatsPanel()
    {
        if (statsPanel == null) return;

        CloseAllSubPanels();
        UpdateStatsPanel();
        statsPanel.SetActive(true);
        ShowOverlay();
    }

    public void CloseStatsPanel()
    {
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

    #region Rovers Status Panel

    void InitRoversStatusPanel()
    {
        if (roversStatusPanel != null)
            roversStatusPanel.SetActive(false);

        if (roversStatusCloseButton != null)
            roversStatusCloseButton.onClick.AddListener(CloseRoversStatusPanel);
    }

    public void OpenRoversStatusPanel()
    {
        if (roversStatusPanel == null) return;

        CloseAllSubPanels();
        UpdateRoversStatusPanel();
        roversStatusPanel.SetActive(true);
        ShowOverlay();
    }

    public void CloseRoversStatusPanel()
    {
        if (roversStatusPanel != null)
            roversStatusPanel.SetActive(false);

        HideOverlay();
    }

    void UpdateRoversStatusPanel()
    {
        if (roversStatusListParent == null || roverStatusItemPrefab == null) return;

        var progress = gameManager.GetProgress();
        if (progress == null) return;

        foreach (Transform child in roversStatusListParent)
            Destroy(child.gameObject);

        foreach (var rover in progress.Rovers)
        {
            if (rover.IsDestroyed) continue;

            GameObject item = Instantiate(roverStatusItemPrefab, roversStatusListParent);

            Text nameText = item.transform.Find("NameText")?.GetComponent<Text>();
            Text batteryText = item.transform.Find("InfoRow/BatteryText")?.GetComponent<Text>();
            Text capacityText = item.transform.Find("InfoRow/CapacityText")?.GetComponent<Text>();
            Text statusText = item.transform.Find("InfoRow/StatusText")?.GetComponent<Text>();

            if (nameText != null)
                nameText.text = $"{rover.Name}";

            if (batteryText != null)
                batteryText.text = $"{rover.CurrentBattery:F0}/{rover.MaxBattery:F0}";

            if (capacityText != null)
                capacityText.text = $"{rover.CargoCapacity} kg";

            if (statusText != null)
            {
                statusText.text = rover.IsBusy ? "Busy" : "Idle";
                statusText.color = rover.IsBusy ? Color.yellow : Color.green;
            }
        }
    }

    #endregion

    #region Settings Panel

    void InitSettingsPanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (settingsCloseButton != null)
            settingsCloseButton.onClick.AddListener(CloseSettingsPanel);

        if (exitGameButton != null)
            exitGameButton.onClick.AddListener(ExitGame);

        if(newGameButton != null)
            newGameButton.onClick.AddListener(NewGame);

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            volumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        }
    }

    public void NewGame()
    {
        SaveManager.DeleteSave();
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    void InitResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        resolutions = Screen.resolutions;

        if (resolutions == null || resolutions.Length == 0)
        {
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
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    public void OpenSettingsPanel()
    {
        if (settingsPanel == null) return;

        CloseAllSubPanels();

        if (volumeSlider != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
            volumeSlider.value = savedVolume;
            AudioManager.Instance?.SetVolume(savedVolume);
        }

        settingsPanel.SetActive(true);
        ShowOverlay();
    }

    public void CloseSettingsPanel()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        HideOverlay();
    }

    public void OnVolumeChanged(float value)
    {
        AudioManager.Instance?.SetVolume(value);
    }

    public void OnResolutionChanged(int index)
    {
        if (resolutions == null || index >= resolutions.Length) return;

        Resolution resolution = resolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region Overlay

    public void ShowOverlay()
    {
        if (sharedOverlay == null || sharedOverlay.activeSelf) return;

        sharedOverlay.SetActive(true);

        CanvasGroup cg = sharedOverlay.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = sharedOverlay.AddComponent<CanvasGroup>();

        cg.blocksRaycasts = true;
        cg.interactable = true;
        cg.alpha = 0.5f;
    }

    public void HideOverlay()
    {
        bool menuOpen = isMenuOpen;
        bool orderOpen = orderPanel != null && orderPanel.IsPanelOpen();
        bool statsOpen = statsPanel != null && statsPanel.activeSelf;
        bool roversStatusOpen = roversStatusPanel != null && roversStatusPanel.activeSelf;
        bool ordersOpen = ordersPanel != null && ordersPanel.activeSelf;
        bool settingsOpen = settingsPanel != null && settingsPanel.activeSelf;
        bool baseOpen = basePanel != null && basePanel.panel != null && basePanel.panel.activeSelf;

        if (!menuOpen && !orderOpen && !statsOpen && !roversStatusOpen && !ordersOpen && !settingsOpen && !baseOpen)
        {
            if (sharedOverlay != null && sharedOverlay.activeSelf)
                sharedOverlay.SetActive(false);
        }
    }

    #endregion

    #region Menu

    void InitMenu()
    {
        if (menuPanel != null)
        {
            menuRect = menuPanel.GetComponent<RectTransform>();
            float width = menuRect != null && menuRect.rect.width > 0 ? menuRect.rect.width : 300f;

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
    }

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

                if (animCoroutine != null)
                    StopCoroutine(animCoroutine);

                animCoroutine = StartCoroutine(AnimateMenu(menuClosedPos, menuOpenPos, false));
            }
        }
    }

    public void CloseMenu()
    {
        if (!isMenuOpen) return;
        isMenuOpen = false;

        if (menuRect != null)
        {
            if (animCoroutine != null)
                StopCoroutine(animCoroutine);

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

    void HideMenuImmediate()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);

        HideOverlay();
    }

    #endregion

    #region Helpers

    void CloseAllSubPanels()
    {
        if (statsPanel != null) statsPanel.SetActive(false);
        if (roversStatusPanel != null) roversStatusPanel.SetActive(false);
        if (ordersPanel != null) ordersPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (basePanel != null)
            basePanel.ClosePanel();
    }

    #endregion

    #region Button Handlers

    void OnOrdersClick()
    {
        OpenOrdersPanel();
    }

    void OnOverlayClick()
    {
        if (orderPanel != null && orderPanel.IsPanelOpen())
        {
            orderPanel.ClosePanel();
            return;
        }

        if (basePanel != null && basePanel.panel != null && basePanel.panel.activeSelf)
        {
            return;
        }

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
            CloseMenu();
    }

    #endregion
}