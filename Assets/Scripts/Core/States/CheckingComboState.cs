using UnityEngine;

public class CheckingComboState : IGameState
{
    public void Enter()
    {
        bool isTweening = GridManager.Instance.ProcessNextMerge();
        
        if (isTweening)
        {
            GameStateManager.Instance.ChangeState(GameStateManager.Instance.Animating);
        }
        else
        {
            bool hasExploded = GridManager.Instance.CleanupPrivilegedPlates();
            if (hasExploded)
            {
                // Vụ nổ đĩa reset mức ưu tiên của các đĩa lân cận về 0 và đưa chúng vào hàng đợi.
                // Ta phải chạy lại CheckingCombo để xử lý chúng tiếp.
                if (BezierTween.Instance != null && BezierTween.Instance.HasActiveTweens)
                {
                    GameStateManager.Instance.ChangeState(GameStateManager.Instance.Animating);
                }
                else
                {
                    // Không có tween nào đang chạy, kiểm tra combo ngay lập tức
                    GameStateManager.Instance.ChangeState(GameStateManager.Instance.CheckingCombo);
                }
            }
            else
            {
                // Gọi GridManager tổng kết combo và cộng điểm thưởng
                if (GridManager.Instance != null)
                {
                    GridManager.Instance.EvaluateTurnCombo();
                }

                // Luôn trả về PlayingState để TrayManager có cơ hội Refill khay đĩa (Khay đầy)
                // Sau khi Refill xong, GameStateManager sẽ tự động check Game Over qua event OnRefillComplete.
                GameStateManager.Instance.ChangeState(GameStateManager.Instance.Playing);
            }
        }
    }

    public void Execute()
    {
    }

    public void Exit()
    {
    }
}
