using System;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public IGameState CurrentState { get; private set; }

    // Danh sách các trạng thái của game
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
        Playing = new PlayingState();
        Animating = new AnimatingState();
        CheckingCombo = new CheckingComboState();
        GameOver = new GameOverState();
    }

    private void Start()
    {
        // Trạng thái mặc định khi bắt đầu game
        ChangeState(Playing);
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
}
