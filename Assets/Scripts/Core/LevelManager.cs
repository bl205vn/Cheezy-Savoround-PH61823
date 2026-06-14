using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    public static LevelData CurrentLevelData { get; private set; }

    [SerializeField] private GridManager _gridManager;
    [SerializeField] private TrayManager _trayManager;
    
    [Header("Debug")]
    [Tooltip("Bật để sử dụng file Test và vẽ Gizmos")]
    [SerializeField] private bool _enableDebug = false; 
    [SerializeField] private TextAsset _testLevelJson; 

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (_enableDebug && _testLevelJson != null)
        {
            LoadFromTextAsset(_testLevelJson);
        }
        else
        {
            // Liên kết file Save với Level Loading
            int currentLevel = SaveLoadManager.Data != null ? SaveLoadManager.Data.CurrentLevel : 1;
            if (currentLevel > 30) currentLevel = 30; // Tạm giới hạn 30 level
            LoadLevel(currentLevel); 
        }
    }

    public void LoadNextLevel()
    {
        int nextLevel = SaveLoadManager.Data != null ? SaveLoadManager.Data.CurrentLevel : 1;
        if (nextLevel > 30) nextLevel = 30;
        
        LoadLevel(nextLevel);
    }

    public void LoadLevel(int levelId)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>($"Levels/level_{levelId}");
        if (jsonFile == null)
        {
            Debug.LogError($"[LevelManager] Không tìm thấy file JSON cho Level {levelId}");
            return;
        }
        LoadFromTextAsset(jsonFile);
    }

    public void LoadFromTextAsset(TextAsset jsonFile)
    {
        LevelData data = JsonUtility.FromJson<LevelData>(jsonFile.text);
        if (data == null) return;
        
        CurrentLevelData = data; // Cache data cho các class khác sử dụng (Data-Driven)

        // Khởi tạo Pool động dựa trên LevelData
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.InitializePool(data.gridWidth, data.gridHeight, data.holdSlotCount, data.maxSlices);
        }

        if (_gridManager != null)
        {
            _gridManager.GenerateGrid(data.levelId, data.gridWidth, data.gridHeight);
        }
        
        if (_trayManager != null)
        {
            _trayManager.GenerateTray(data.holdSlotCount);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!_enableDebug) return;
        
        // Preview level trong Editor mà không cần Play
        if (_testLevelJson != null && _gridManager != null && _trayManager != null)
        {
            try
            {
                LevelData data = JsonUtility.FromJson<LevelData>(_testLevelJson.text);
                if (data != null)
                {
                    if (data.gridWidth > 0 && data.gridHeight > 0)
                        _gridManager.DrawGizmos(data.gridWidth, data.gridHeight);
                        
                    if (data.holdSlotCount > 0)
                        _trayManager.DrawGizmos(data.holdSlotCount);
                }
            }
            catch
            {
                // Bỏ qua lỗi parse JSON khi đang gõ text
            }
        }
    }
#endif
}
