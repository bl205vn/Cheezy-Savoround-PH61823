using System;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public IGameState CurrentState { get; private set; }

    // Danh sách các trạng thái của game
    public LobbyState Lobby { get; private set; }
    public PlayingState Playing { get; private set; }
    public AnimatingState Animating { get; private set; }
    public CheckingComboState CheckingCombo { get; private set; }
    public GameOverState GameOver { get; private set; }

    /// <summary>
    /// Observer Event: Phát mỗi khi FSM chuyển trạng thái.
    /// TrayManager lắng nghe để biết khi nào PlayingState → sinh batch đĩa mới.
    /// </summary>
    public static event Action<IGameState> OnStateChanged;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Khởi tạo các State class riêng biệt (Không dùng nested booleans)
        Lobby = new LobbyState();
        Playing = new PlayingState();
        Animating = new AnimatingState();
        CheckingCombo = new CheckingComboState();
        GameOver = new GameOverState();
    }

    private void OnEnable()
    {
        TrayManager.OnRefillComplete += CheckAndTriggerGameOver;
    }

    private void OnDisable()
    {
        TrayManager.OnRefillComplete -= CheckAndTriggerGameOver;
    }

    private void CheckAndTriggerGameOver()
    {
        if (GridManager.Instance != null && GridManager.Instance.CheckGameOver())
        {
            TriggerGameOver();
        }
    }

    private void Start()
    {
        // Trạng thái mặc định khi bắt đầu game là Lobby (chờ người chơi nhấn Start)
        ChangeState(Lobby);
    }

    private void Update()
    {
        // Cho phép State hiện tại thực thi logic mỗi frame (nếu có)
        CurrentState?.Execute();
    }

    public void ChangeState(IGameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState?.Enter();

        // Phát event SAU Enter() để subscriber nhận đúng trạng thái cuối cùng
        OnStateChanged?.Invoke(newState);
        
        Debug.Log($"[FSM] Changed State to: {newState.GetType().Name}");
    }

    public void TriggerGameOver()
    {
        ChangeState(GameOver);
        GameEvents.TriggerGameOver();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // Khi người chơi vuốt ra màn hình chính, vuốt control center, hoặc có cuộc gọi tới
        // pauseStatus = true tức là App bị đẩy xuống chạy ngầm (Background)
        if (pauseStatus)
        {
            if (CurrentState is GameOverState)
            {
                // Nếu đang ở màn hình thua mà thoát app ra nền -> Xoá file save ván này.
                // Nếu app bị hệ điều hành tắt luôn, lần sau mở lên sẽ tự Restart.
                // Nếu người chơi chỉ vuốt ra xem tin nhắn rồi quay lại (app chưa chết) thì trên màn hình vẫn giữ nguyên bảng GameOver.
                if (SaveLoadManager.Data != null)
                {
                    SaveLoadManager.Data.CurrentLevel = 1;
                    SaveLoadManager.Data.CurrentLevelProgress = null;
                    SaveLoadManager.Data.TotalScore = 0;
                }
            }
            SaveLoadManager.Save();
            Debug.Log("[FSM] Ứng dụng đưa vào nền (Background). Đã kích hoạt lưu tiến trình tự động.");
        }
    }

    private void OnApplicationQuit()
    {
        // Đề phòng trường hợp hiếm hoi OS gọi hàm này thay vì Pause
        if (CurrentState is GameOverState)
        {
            if (SaveLoadManager.Data != null)
            {
                SaveLoadManager.Data.CurrentLevel = 1;
                SaveLoadManager.Data.CurrentLevelProgress = null;
                SaveLoadManager.Data.TotalScore = 0;
            }
        }
        SaveLoadManager.Save();
        Debug.Log("[FSM] Ứng dụng chuẩn bị tắt. Đã kích hoạt lưu tiến trình tự động.");
    }
}
