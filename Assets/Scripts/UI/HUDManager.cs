using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HUDManager : MonoBehaviour
{
    [Header("References")]
    public GameManager3D gameManager;

    [Header("Top Panel")]
    public Text dayText;
    public Text moneyText;
    public Text ratingText;
    public Text roversStatusText;

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

    [Header("Day Timer")]
    public Text dayTimerText;

    private bool isMenuOpen = false;
    private RectTransform menuRect;
    private Vector2 menuClosedPos;
    private Vector2 menuOpenPos;
    private Coroutine animCoroutine;

    private OrderPanelUI orderPanel;
    private float overlayBlockTime = 0f;
    private bool isOverlayClickBlocked = false;

    private static HUDManager instance;
    public static HUDManager Instance => instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager3D>();

        orderPanel = FindFirstObjectByType<OrderPanelUI>();

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

        /* Shared Overlay setup */
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
            statusButton.onClick.AddListener(() => { OnStatusClick(); CloseMenu(); });

        if (statsButton != null)
            statsButton.onClick.AddListener(() => { OnStatsClick(); CloseMenu(); });

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
            int available = 0;
            int destroyed = 0;

            foreach (var rover in progress.Rovers)
            {
                if (rover.IsDestroyed) destroyed++;
                else if (!rover.IsBusy) available++;
            }

            roversStatusText.text = $"🚀 {available}/{total}";
            roversStatusText.color = destroyed > 0 ? Color.red : (available == 0 ? Color.yellow : Color.white);
        }
    }

    public void UpdateDayTimer(float timeLeft)
    {
        if (dayTimerText != null)
        {
            int minutes = Mathf.FloorToInt(timeLeft / 60f);
            int seconds = Mathf.FloorToInt(timeLeft % 60f);
            dayTimerText.text = $"⏱️ {minutes:00}:{seconds:00}";
        }
    }

    /* ===== OVERLAY ===== */

    void OnOverlayClick()
    {
        if (isOverlayClickBlocked)
        {
            return;
        }

        if (Time.time - overlayBlockTime < 0.3f)
        {
            return;
        }

        if (orderPanel != null && orderPanel.IsPanelOpen())
        {
            orderPanel.ClosePanel();
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
            overlayBlockTime = Time.time;
            isOverlayClickBlocked = true;

            StartCoroutine(UnblockOverlayAfterDelay(0.3f));
        }
    }

    IEnumerator UnblockOverlayAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isOverlayClickBlocked = false;
    }

    public void HideOverlay()
    {
        bool menuOpen = isMenuOpen;
        bool orderOpen = orderPanel != null && orderPanel.IsPanelOpen();

        if (!menuOpen && !orderOpen)
        {
            if (sharedOverlay != null && sharedOverlay.activeSelf)
            {
                sharedOverlay.SetActive(false);
                isOverlayClickBlocked = false;
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

    void OnStatusClick()
    {
        Debug.Log("Status button clicked");
    }

    void OnStatsClick()
    {
        Debug.Log("Stats button clicked");
    }

    void OnSettingsClick()
    {
        Debug.Log("Settings button clicked");
    }
}