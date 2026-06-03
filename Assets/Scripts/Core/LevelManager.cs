using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelData CurrentLevelData { get; private set; }

    [SerializeField] private GridManager _gridManager;
    [SerializeField] private TrayManager _trayManager;
    
    [Header("Testing")]
    [SerializeField] private TextAsset _testLevelJson; // Kéo file JSON vào đây để test trực tiếp
    
    [Header("Debug")]
    [SerializeField] private bool _showDebugGizmos = false; // Tích để hiện đường Gizmo trong Scene

    private void Start()
    {
        if (_testLevelJson != null)
        {
            LoadFromTextAsset(_testLevelJson);
        }
        else
        {
            LoadLevel(1); // Mặc định load màn 1
        }
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
        if (!_showDebugGizmos) return;
        
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
