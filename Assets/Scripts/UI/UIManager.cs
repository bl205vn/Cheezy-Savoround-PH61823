using DG.Tweening;
using UnityEngine;

/// <summary>
/// UIManager chỉ làm nhiệm vụ ĐIỀU HƯỚNG (Router) bật/tắt các Canvas và Panel chính.
/// TUYỆT ĐỐI KHÔNG nhét logic xử lý Shop, Thành tựu hay Điểm số vào đây để tránh God Class.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Lobby Panels (nằm trong TileCanvas)")]
    [SerializeField] private GameObject _starterPanel;
    [SerializeField] private GameObject _shopPanel;
    [SerializeField] private GameObject _achievementPanel;
    [SerializeField] private GameObject _howToPlayPanel;
    [SerializeField] private GameObject _dailyPanel; // Thêm Daily Panel vào UIManager

    [Header("Backgrounds")]
    [SerializeField] private GameObject _lobbyBackground;
    [SerializeField] private GameObject _inGameBackground;

    [Header("In-Game")]
    [SerializeField] private GameObject _hudCanvas;
    [SerializeField] private ShopManager _shopManager; // Reference để gọi SwitchTab từ UIManager
    [SerializeField] private DailyRewardManager _dailyRewardManager; // Reference để mở Daily Reward

    private bool _shopOpenedFromGame; // Cờ đánh dấu Shop được mở từ giữa game

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Hiển thị sảnh chờ mặc định, tắt HUD
        ShowPanel(_starterPanel);
        if (_hudCanvas != null) _hudCanvas.SetActive(false);

        // Cài đặt Backgrounds cho Lobby
        if (_lobbyBackground != null) _lobbyBackground.SetActive(true);
        if (_inGameBackground != null) _inGameBackground.SetActive(false);
    }

    /// <summary>
    /// Hàm đa năng để bật 1 panel trong Lobby và tắt các panel còn lại.
    /// </summary>
    private void ShowPanel(GameObject panelToShow)
    {
        if (_starterPanel != null) _starterPanel.SetActive(_starterPanel == panelToShow);
        if (_shopPanel != null) _shopPanel.SetActive(_shopPanel == panelToShow);
        if (_achievementPanel != null) _achievementPanel.SetActive(_achievementPanel == panelToShow);
        if (_howToPlayPanel != null) _howToPlayPanel.SetActive(_howToPlayPanel == panelToShow);
        if (_dailyPanel != null) _dailyPanel.SetActive(_dailyPanel == panelToShow);
    }

    // Các hàm Public để gắn vào OnClick() của các Button trong Lobby
    public void OpenShop() => ShowPanel(_shopPanel);
    public void OpenAchievement() => ShowPanel(_achievementPanel);
    public void OpenHowToPlay() => ShowPanel(_howToPlayPanel);
    public void BackToStarter() => ShowPanel(_starterPanel);

    /// <summary>
    /// Mở bảng Daily Reward (Được quản lý như 1 panel của Lobby).
    /// Gắn vào OnClick() của nút Daily trên Lobby.
    /// </summary>
    public void OpenDailyReward()
    {
        ShowPanel(_dailyPanel);
        
        // Gọi Manager để làm mới dữ liệu khi vừa mở lên
        if (_dailyRewardManager != null)
        {
            _dailyRewardManager.CheckDailyReward();
        }
    }

    /// <summary>
    /// Gọi từ code: UIManager.Instance.OpenShopFromGame(tabIndex)
    /// Dành riêng cho Unity Event Inspector.
    /// </summary>
    public void OpenShopFromGame(int tabIndex)
    {
        OpenShopFromGameWithItem(tabIndex, 0);
    }

    /// <summary>
    /// Mở Shop overlay ngay giữa game, nhảy vào Tab và Item chỉ định.
    /// Gọi từ code: UIManager.Instance.OpenShopFromGameWithItem(tabIndex, itemIndex)
    /// </summary>
    public void OpenShopFromGameWithItem(int tabIndex, int itemIndex)
    {
        _shopOpenedFromGame = true;

        // Tắt HUD để tránh chồng chéo UI
        if (_hudCanvas != null) _hudCanvas.SetActive(false);

        // Bật panel Shop
        if (_shopPanel != null) _shopPanel.SetActive(true);

        // Chuyển sang Tab + Item chỉ định (0: Boost, 1: Coin, 2: Skin)
        if (_shopManager != null) _shopManager.SwitchTabAndItem(tabIndex, itemIndex);
    }

    /// <summary>
    /// Overload không tham số để gắn OnClick Button (mặc định mở Tab Coin).
    /// </summary>
    public void OpenShopFromGame()
    {
        OpenShopFromGame(1); // Tab Coin
    }

    /// <summary>
    /// Được gọi bởi Button "Exit" (dấu X) trong Shop.
    /// Thay thế BackToStarter() để xử lý đúng ngữ cảnh.
    /// </summary>
    public void CloseShop()
    {
        if (_shopOpenedFromGame)
        {
            // Mở từ giữa game → tắt Shop, bật lại HUD, quay lại chơi tiếp
            _shopOpenedFromGame = false;
            if (_shopPanel != null) _shopPanel.SetActive(false);
            if (_hudCanvas != null) _hudCanvas.SetActive(true);
            SaveLoadManager.Save(); // Lưu vàng/dữ liệu khi vừa rời Shop để an toàn
        }
        else
        {
            // Mở từ Lobby → quay về Starter Panel như bình thường
            BackToStarter();
        }
    }

    /// <summary>
    /// Được gọi bởi Button "Start" ở Starter Panel
    /// </summary>
    public void StartGame()
    {
        // Tắt toàn bộ UI Lobby
        ShowPanel(null);
        
        // Đổi background
        if (_lobbyBackground != null) _lobbyBackground.SetActive(false);
        if (_inGameBackground != null) _inGameBackground.SetActive(true);

        // Bật HUD trong game
        if (_hudCanvas != null) _hudCanvas.SetActive(true);

        // Chuyển FSM sang trạng thái chơi
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ChangeState(GameStateManager.Instance.Playing);
        }
    }

    /// <summary>
    /// Được gọi bởi Button "Home" trong HUD (Góc trên bên trái)
    /// </summary>
    public void QuitToLobby()
    {
        // Bật lại sảnh chờ
        ShowPanel(_starterPanel);
        
        SaveLoadManager.Save(); // Lưu vàng kiếm được trong level vừa qua

        // Đổi background về sảnh
        if (_lobbyBackground != null) _lobbyBackground.SetActive(true);
        if (_inGameBackground != null) _inGameBackground.SetActive(false);

        // Tắt HUD
        if (_hudCanvas != null) _hudCanvas.SetActive(false);

        // Chuyển FSM về sảnh
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ChangeState(GameStateManager.Instance.Lobby);
        }
        
        // Ghi chú: Logic xóa lưới (Clear Grid/Tray) để reset màn chơi sẽ được bổ sung sau
    }

    /// <summary>
    /// Quay về sảnh chờ (Lobby) ĐỒNG THỜI xóa trắng toàn bộ dữ liệu để bắt đầu lại từ Level 1.
    /// Dùng riêng cho nút Home khi người chơi muốn Reset & Quit cùng lúc.
    /// </summary>
    public void QuitAndResetToLobby()
    {
        // 1. Dọn dẹp hiệu ứng và dữ liệu hiện tại
        DOTween.KillAll();
        if (SaveLoadManager.Data != null)
        {
            SaveLoadManager.Data.CurrentLevel = 1;
            SaveLoadManager.Data.CurrentLevelProgress = null;
            SaveLoadManager.Data.TotalScore = 0;
            SaveLoadManager.Save();
        }

        // 2. Load lại Level 1 để clear toàn bộ Grid/Tray trên màn hình
        var levelProgress = FindFirstObjectByType<LevelProgressUI>();
        if (levelProgress != null) levelProgress.ResetProgress();
        if (LevelManager.Instance != null) LevelManager.Instance.LoadLevel(1);

        // 3. Bật giao diện Lobby, tắt giao diện In-Game
        ShowPanel(_starterPanel);
        if (_lobbyBackground != null) _lobbyBackground.SetActive(true);
        if (_inGameBackground != null) _inGameBackground.SetActive(false);
        if (_hudCanvas != null) _hudCanvas.SetActive(false);

        // 4. Quan trọng nhất: Đưa máy trạng thái về LOBBY (Không phải Playing)
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ChangeState(GameStateManager.Instance.Lobby);
        }
    }

    /// <summary>
    /// Được gọi bởi Button "Restart" trong HUD (Góc trên bên phải)
    /// </summary>
    public void RestartGame()
    {
        // 0. Kill MỌI tween đang chạy để tránh MissingReferenceException
        // (Các đĩa đã rời cell qua PlayShrinkAndReturn vẫn giữ DOScale tween treo)
        DOTween.KillAll();

        // 1. Đặt level về 1 theo yêu cầu, xoá tiến trình
        if (SaveLoadManager.Data != null)
        {
            SaveLoadManager.Data.CurrentLevel = 1;
            SaveLoadManager.Data.CurrentLevelProgress = null;
            SaveLoadManager.Data.TotalScore = 0;
            SaveLoadManager.Save();
        }

        // 2. Reset thanh tiến trình
        var levelProgress = FindFirstObjectByType<LevelProgressUI>();
        if (levelProgress != null)
        {
            levelProgress.ResetProgress();
        }

        // 3. Load lại level 1 (Hàm LoadLevel đã bao gồm ClearGrid và ClearTray)
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadLevel(1);
        }

        // 4. Chuyển FSM sang trạng thái chơi
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ChangeState(GameStateManager.Instance.Playing);
        }
    }
}
