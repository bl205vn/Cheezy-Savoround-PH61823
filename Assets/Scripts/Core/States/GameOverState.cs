using UnityEngine;

public class GameOverState : IGameState
{
    public void Enter()
    {
        Debug.Log("[FSM] GAME OVER! Khóa toàn bộ tương tác.");
        // Hiển thị màn hình UI Game Over
    }

    public void Execute()
    {
    }

    public void Exit()
    {
    }
}
