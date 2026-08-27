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
    public GameObject menuOverlay;
    public float animationDuration = 0.3f;

    private bool isMenuOpen = false;
    private RectTransform menuRect;
    private Vector2 menuClosedPos;
    private Vector2 menuOpenPos;
    private Coroutine animCoroutine;

    void Start()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager3D>();

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

        /* Overlay click -> close menu */
        if (menuOverlay != null)
        {
            Button overlayBtn = menuOverlay.GetComponent<Button>();
            if (overlayBtn == null)
                overlayBtn = menuOverlay.AddComponent<Button>();

            overlayBtn.onClick.AddListener(CloseMenu);
            menuOverlay.SetActive(false);
        }

        /* Buttons */
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

    void UpdateUI()
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

    /* ===== MENU ===== */

    void ToggleMenu()
    {
        Debug.Log($"ToggleMenu: isOpen={isMenuOpen}");

        if (isMenuOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    void OpenMenu()
    {
        if (isMenuOpen)
        {
            Debug.Log("OpenMenu: already open");
            return;
        }

        isMenuOpen = true;
        Debug.Log("OpenMenu: opening");

        if (menuOverlay != null)
            menuOverlay.SetActive(true);

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
        if (!isMenuOpen)
        {
            Debug.Log("CloseMenu: already closed");
            return;
        }

        Debug.Log("CloseMenu: closing");
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
        Debug.Log($"Animation: from {start} to {end}");

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

        Debug.Log($"Animation complete at {end}");

        if (hideOnComplete)
        {
            HideMenuImmediate();
        }

        animCoroutine = null;
    }

    void HideMenuImmediate()
    {
        Debug.Log("HideMenuImmediate");

        if (menuPanel != null)
            menuPanel.SetActive(false);

        if (menuOverlay != null)
            menuOverlay.SetActive(false);
    }

    /* ===== BUTTON HANDLERS ===== */

    void OnOrdersClick()
    {
        Debug.Log("Orders button clicked");
        /* TODO: Show all orders list */
    }

    void OnStatusClick()
    {
        Debug.Log("Status button clicked");
        /* TODO: Show rovers status panel */
    }

    void OnStatsClick()
    {
        Debug.Log("Stats button clicked");
        /* TODO: Show statistics panel */
    }

    void OnSettingsClick()
    {
        Debug.Log("Settings button clicked");
        /* TODO: Show settings panel */
    }
}