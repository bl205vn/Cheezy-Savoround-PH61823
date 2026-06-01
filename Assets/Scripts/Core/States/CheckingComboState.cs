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
                // Explosion resets neighboring priorities to 0 and might trigger combo logic in next frame
                GameStateManager.Instance.ChangeState(GameStateManager.Instance.Animating);
            }
            else
            {
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
