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

                // Kiểm tra Game Over ngay sau khi xử lý xong toàn bộ combo trên lưới
                if (GridManager.Instance != null && GridManager.Instance.CheckGameOver())
                {
                    GameStateManager.Instance.TriggerGameOver();
                }
                else
                {
                    GameStateManager.Instance.ChangeState(GameStateManager.Instance.Playing);
                }
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
