using UnityEngine;

public class AnimatingState : IGameState
{
    public void Enter()
    {
        // Lắng nghe sự kiện bay xong của BezierTween (Observer Pattern)
        BezierTween.OnAllTweensCompleted += HandleTweensCompleted;
    }

    public void Execute()
    {
        // Input bị lock do InputManager chỉ hoạt động khi ở PlayingState
        // Logic Tween đã được BezierTween tự xử lý trong Update của nó.
    }

    public void Exit()
    {
        // Hủy lắng nghe để tránh memory leak
        BezierTween.OnAllTweensCompleted -= HandleTweensCompleted;
    }

    private void HandleTweensCompleted()
    {
        // Khi bay xong -> quay lại check combo (Combo Cascade)
        GameStateManager.Instance.ChangeState(GameStateManager.Instance.CheckingCombo);
    }
}
