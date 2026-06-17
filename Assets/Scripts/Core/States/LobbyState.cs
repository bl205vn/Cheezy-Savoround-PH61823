using UnityEngine;

public class LobbyState : IGameState
{
    public void Enter()
    {
        // Đang ở sảnh chính (Lobby), khóa tương tác game
        Debug.Log("[LobbyState] Nhập sảnh chính. Chờ người chơi nhấn Start.");
    }

    public void Execute()
    {
    }

    public void Exit()
    {
        // Khi rời sảnh (nhấn Start)
        Debug.Log("[LobbyState] Rời sảnh chính, bắt đầu game!");
    }
}
