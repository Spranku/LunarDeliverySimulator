using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager3D>();

        orderPanel = FindFirstObjectByType<OrderPanelUI>();

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

    /* ===== STATS PANEL ===== */

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
            statsTitleText.text = "📊 Stats";

        if (statsTotalOrdersText != null)
            statsTotalOrdersText.text = $"📦 Total Orders: {total}";

        if (statsCompletedText != null)
            statsCompletedText.text = $"✅ Completed: {progress.TotalDeliveriesCompleted}";

        if (statsFailedText != null)
            statsFailedText.text = $"❌ Failed: {progress.TotalDeliveriesFailed}";

        if (statsMoneyEarnedText != null)
            statsMoneyEarnedText.text = $"💰 Money Earned: {progress.Money}";

        if (statsRoversLostText != null)
            statsRoversLostText.text = $"💀 Rovers Lost: {roversLost}";

        if (statsDaysSurvivedText != null)
            statsDaysSurvivedText.text = $"📅 Days Survived: {progress.Day}";
    }

    /* ===== ROVERS STATUS PANEL ===== */

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

        /* Очищаем список */
        foreach (Transform child in roversStatusListParent)
            Destroy(child.gameObject);

        int count = 0;
        foreach (var rover in progress.Rovers)
        {
            if (rover.IsDestroyed) continue;

            GameObject item = Instantiate(roverStatusItemPrefab, roversStatusListParent);
            Text[] texts = item.GetComponentsInChildren<Text>();

            if (texts.Length >= 4)
            {
                texts[0].text = rover.Name;
                texts[1].text = $"🔋 {rover.CurrentBattery:F0}/{rover.MaxBattery:F0}";
                texts[2].text = $"📦 {rover.CargoCapacity} kg";
                texts[3].text = rover.IsBusy ? "⏳ Busy" : "✅ Idle";
                texts[3].color = rover.IsBusy ? Color.yellow : Color.green;
                count++;
            }
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

    void CloseAllSubPanels()
    {
        if (statsPanel != null) statsPanel.SetActive(false);
        if (roversStatusPanel != null) roversStatusPanel.SetActive(false);
    }

    /* ===== OVERLAY ===== */

    void OnOverlayClick()
    {
        Debug.Log("Overlay clicked");

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

        if (isMenuOpen)
        {
            CloseMenu();
        }
    }

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

        if (!menuOpen && !orderOpen && !statsOpen && !roversStatusOpen)
        {
            if (sharedOverlay != null && sharedOverlay.activeSelf)
            {
                sharedOverlay.SetActive(false);
                Debug.Log("Overlay hidden");
            }
        }
    }

    /* ===== MENU ===== */

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

    void HideMenuImmediate()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);
        HideOverlay();
    }

    /* ===== BUTTON HANDLERS ===== */

    void OnOrdersClick()
    {
        Debug.Log("Orders button clicked");
    }

    void OnSettingsClick()
    {
        Debug.Log("Settings button clicked");
    }
}