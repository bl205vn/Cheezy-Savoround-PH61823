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
            GameStateManager.Instance.ChangeState(GameStateManager.Instance.Playing);
        }
    }

    public void Execute()
    {
    }

    public void Exit()
    {
    }
}
