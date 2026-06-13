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

    [Header("Backgrounds")]
    [SerializeField] private GameObject _lobbyBackground;
    [SerializeField] private GameObject _inGameBackground;

    [Header("In-Game")]
    [SerializeField] private GameObject _hudCanvas;

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
    }

    // Các hàm Public để gắn vào OnClick() của các Button trong Lobby
    public void OpenShop() => ShowPanel(_shopPanel);
    public void OpenAchievement() => ShowPanel(_achievementPanel);
    public void OpenHowToPlay() => ShowPanel(_howToPlayPanel);
    public void BackToStarter() => ShowPanel(_starterPanel);

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
}
