using System;

[Serializable]
public class GameSettings
{
    public bool IsMusicOn;
    public bool IsSoundOn;
    public bool IsVibrationOn;
    public int TargetFPS;

    public GameSettings()
    {
        // Mặc định bật hết khi mới tải game
        IsMusicOn = true;
        IsSoundOn = true;
        IsVibrationOn = true;
        TargetFPS = 60; // Mặc định 60 FPS
    }
}
