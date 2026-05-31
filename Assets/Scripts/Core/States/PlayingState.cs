using UnityEngine;

public class PlayingState : IGameState
{
    public void Enter()
    {
        // Mở khóa input (InputManager sẽ check xem CurrentState có phải là PlayingState không)
    }

    public void Execute()
    {
        // Các logic cập nhật liên tục khi đang chơi có thể để ở đây
    }

    public void Exit()
    {
        // Dọn dẹp nếu cần khi rời khỏi trạng thái chơi
    }
}
