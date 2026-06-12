using System;

[Serializable]
public class GameSettings
{
    public bool IsMusicOn;
    public bool IsSoundOn;
    public bool IsVibrationOn;

    public GameSettings()
    {
        // Mặc định bật hết khi mới tải game
        IsMusicOn = true;
        IsSoundOn = true;
        IsVibrationOn = true;
    }
}
