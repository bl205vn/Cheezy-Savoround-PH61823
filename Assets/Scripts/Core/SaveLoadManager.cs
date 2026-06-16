using UnityEngine;
using System.IO;

public static class SaveLoadManager
{
    public static PlayerData Data { get; private set; }

    private static string _saveFilePath;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        _saveFilePath = Path.Combine(Application.persistentDataPath, "playerdata.json");
        Load();

        // Đăng ký nhận Vàng khi nổ đĩa (tránh duplicate nếu tắt Domain Reload)
        GameEvents.OnPlateExploded -= HandlePlateExploded;
        GameEvents.OnPlateExploded += HandlePlateExploded;

        GameStateManager.OnStateChanged -= HandleStateChanged;
        GameStateManager.OnStateChanged += HandleStateChanged;
    }

    private static void HandlePlateExploded(int pizzaType, int scoreAdded, int goldAdded)
    {
        if (Data != null)
        {
            Data.Gold += goldAdded;
            Data.TotalScore += scoreAdded;
            if (Data.TotalScore > Data.BestScore)
            {
                Data.BestScore = Data.TotalScore;
            }
            // Không Save() liên tục để tránh I/O lag. Thay vào đó, gọi UpdateAll UI.
            GoldDisplay.UpdateAll();
        }
    }

    private static void HandleStateChanged(IGameState state)
    {
        // Khi FSM quay về trạng thái ổn định (PlayingState), cập nhật trạng thái bàn chơi vào RAM
        if (state is PlayingState)
        {
            if (Data != null && GridManager.Instance != null && TrayManager.Instance != null && LevelManager.Instance != null)
            {
                if (Data.CurrentLevelProgress == null)
                    Data.CurrentLevelProgress = new LevelProgressData();

                Data.CurrentLevelProgress.levelId = SaveLoadManager.Data.CurrentLevel;
                Data.CurrentLevelProgress.occupiedCells = GridManager.Instance.CaptureState();
                Data.CurrentLevelProgress.traySlots = TrayManager.Instance.CaptureState();

                var levelProgressUI = LevelProgressUI.Instance;
                if (levelProgressUI != null)
                {
                    Data.CurrentLevelProgress.currentScore = levelProgressUI.CurrentScore;
                }
            }
        }
    }

    /// <summary>
    /// Đọc dữ liệu từ file JSON vào bộ nhớ. Nếu chưa có file sẽ tạo mới.
    /// </summary>
    public static void Load()
    {
        if (File.Exists(_saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(_saveFilePath);
                Data = JsonUtility.FromJson<PlayerData>(json);
                Debug.Log($"[SaveLoadManager] Load data success: {_saveFilePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveLoadManager] Load error: {e.Message}. Creating new default data.");
                ResetData();
            }
        }
        else
        {
            ResetData();
        }

        // Validate essential fields in case of corrupted/old saves
        if (Data.UnlockedSkins == null || Data.UnlockedSkins.Count == 0)
        {
            Data.UnlockedSkins = new System.Collections.Generic.List<string>() { "plate_01" };
        }
        if (string.IsNullOrEmpty(Data.CurrentSkinId))
        {
            Data.CurrentSkinId = "plate_01";
        }
        // Apply TargetFPS (hardcoded in GameSettings)
        Application.targetFrameRate = GameSettings.TargetFPS;
    }

    /// <summary>
    /// Ghi dữ liệu hiện tại xuống file JSON.
    /// </summary>
    public static void Save()
    {
        if (Data == null) return;
        
        try
        {
            string json = JsonUtility.ToJson(Data, true); // true for pretty print
            File.WriteAllText(_saveFilePath, json);
            Debug.Log("[SaveLoadManager] Save data success.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveLoadManager] Save error: {e.Message}");
        }
    }

    /// <summary>
    /// Khôi phục toàn bộ dữ liệu về trạng thái ban đầu.
    /// </summary>
    public static void ResetData()
    {
        Data = new PlayerData();
        Save();
        Debug.Log("[SaveLoadManager] Reset to default data.");
    }
}
