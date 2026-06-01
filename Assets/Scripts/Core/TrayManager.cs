using System.Collections.Generic;
using UnityEngine;

public class TrayManager : MonoBehaviour
{
    public static TrayManager Instance { get; private set; }

    [SerializeField] private float _slotSpacing; // Khoảng cách giữa các slot
    [SerializeField] private GameObject _pizzaPlatePrefab; // Prefab đĩa pizza

    // Lưu trữ các slot anchor (empty GO) để quản lý vòng đời
    private readonly List<GameObject> _slotAnchors = new List<GameObject>();

    // Theo dõi đĩa pizza trong từng slot (null = slot trống)
    private PizzaPlate[] _slotPlates;
    private int _holdSlotCount;
    private bool _pendingRefill;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        InputManager.OnPlatePlaced += HandlePlatePlacedOnGrid;
        GameStateManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        InputManager.OnPlatePlaced -= HandlePlatePlacedOnGrid;
        GameStateManager.OnStateChanged -= HandleStateChanged;
    }

    /// <summary>
    /// Khi người chơi đặt đĩa lên Grid → đánh dấu slot tương ứng là trống.
    /// Nếu cả 3 slot đều trống → bật cờ chờ refill.
    /// </summary>
    private void HandlePlatePlacedOnGrid(PizzaPlate plate, GridCell cell)
    {
        for (int i = 0; i < _slotPlates.Length; i++)
        {
            if (_slotPlates[i] == plate)
            {
                _slotPlates[i] = null;
                break;
            }
        }

        if (IsAllSlotsEmpty())
        {
            _pendingRefill = true;
        }
    }

    /// <summary>
    /// Lắng nghe FSM chuyển trạng thái.
    /// Khi về PlayingState + cờ refill = true → sinh batch đĩa mới.
    /// Đảm bảo đĩa mới chỉ xuất hiện SAU KHI merge/bloom animation xong hẳn.
    /// </summary>
    private void HandleStateChanged(IGameState newState)
    {
        if (newState is PlayingState && _pendingRefill)
        {
            RefillTray();
        }
    }

    /// <summary>
    /// Kiểm tra xem tất cả slot trên khay đã trống chưa.
    /// </summary>
    public bool IsAllSlotsEmpty()
    {
        if (_slotPlates == null) return false;
        for (int i = 0; i < _slotPlates.Length; i++)
        {
            if (_slotPlates[i] != null) return false;
        }
        return true;
    }

    /// <summary>
    /// Khởi tạo khay chứa: tạo các anchor slot + sinh batch đĩa đầu tiên.
    /// Được gọi bởi LevelManager khi load level.
    /// </summary>
    public void GenerateTray(int slotCount)
    {
        ClearTray();

        if (_pizzaPlatePrefab == null)
        {
            Debug.LogError("[TrayManager] _pizzaPlatePrefab chưa được gán!");
            return;
        }

        _holdSlotCount = slotCount;
        _slotPlates = new PizzaPlate[slotCount];

        // Tạo các anchor slot (empty GO) — giữ nguyên suốt màn chơi
        float offsetX = (slotCount - 1) * _slotSpacing * 0.5f;

        for (int i = 0; i < slotCount; i++)
        {
            Vector3 localPos = new Vector3(i * _slotSpacing - offsetX, 0, 0);
            Vector3 worldPos = transform.position + localPos;

            GameObject anchor = new GameObject($"TraySlot_{i}");
            anchor.transform.SetParent(transform);
            anchor.transform.position = worldPos;

            _slotAnchors.Add(anchor);
        }

        // Sinh batch đĩa đầu tiên
        RefillTray();

        Debug.Log($"[TrayManager] Đã tạo khay {slotCount} slot + sinh batch đĩa đầu tiên.");
    }

    /// <summary>
    /// Sinh đĩa pizza mới cho TẤT CẢ slot trống trên khay.
    /// Mỗi đĩa có các miếng pizza ngẫu nhiên theo cấu hình JSON của Level.
    /// </summary>
    private void RefillTray()
    {
        _pendingRefill = false;

        int refillCount = 0;

        for (int i = 0; i < _holdSlotCount; i++)
        {
            // Chỉ sinh đĩa cho slot đang trống
            if (_slotPlates[i] != null) continue;

            GameObject anchor = _slotAnchors[i];
            Vector3 worldPos = anchor.transform.position;

            PizzaPlate plate = ObjectPoolManager.Instance.GetPizzaPlate();
            plate.transform.position = worldPos;
            plate.transform.rotation = Quaternion.identity;
            plate.transform.SetParent(anchor.transform);
            
            GameObject plateObj = plate.gameObject;

            // Ép scale đĩa pizza theo kích thước slot
            FitPlateToSlot(plateObj);
            plate.Initialize(anchor.transform);
            plate.GenerateRandomSlices(); // Sinh bánh ngẫu nhiên từ JSON config

            _slotPlates[i] = plate;
            refillCount++;
        }

        Debug.Log($"[TrayManager] Refill: Sinh {refillCount} đĩa mới trên khay.");
    }

    /// <summary>
    /// Ép scale prefab vào đúng kích thước 1 slot dựa trên Renderer bounds.
    /// </summary>
    private void FitPlateToSlot(GameObject plateObj)
    {
        Renderer rend = plateObj.GetComponentInChildren<Renderer>();
        if (rend == null) return;

        Vector3 currentSize = rend.bounds.size;
        
        // Chỉ scale theo trục X và Z (mặt phẳng ngang), giữ nguyên tỷ lệ Y
        float scaleX = (currentSize.x > 0.001f) ? (_slotSpacing / currentSize.x) : 1f;
        float scaleZ = (currentSize.z > 0.001f) ? (_slotSpacing / currentSize.z) : 1f;
        
        // Dùng scale nhỏ nhất để giữ tỷ lệ, khít hoàn toàn
        float uniformScale = Mathf.Min(scaleX, scaleZ);
        
        plateObj.transform.localScale = plateObj.transform.localScale * uniformScale;
    }

    private void ClearTray()
    {
        // Xóa tham chiếu đĩa
        if (_slotPlates != null)
        {
            for (int i = 0; i < _slotPlates.Length; i++)
            {
                _slotPlates[i] = null;
            }
        }

        // Hủy anchor (và đĩa con theo hierarchy)
        foreach (var anchor in _slotAnchors)
        {
            if (anchor != null)
            {
                Destroy(anchor);
            }
        }
        _slotAnchors.Clear();
        _pendingRefill = false;
    }

#if UNITY_EDITOR
    public void DrawGizmos(int slotCount)
    {
        Gizmos.color = Color.cyan;
        float offsetX = (slotCount - 1) * _slotSpacing * 0.5f;

        for (int i = 0; i < slotCount; i++)
        {
            Vector3 localPos = new Vector3(i * _slotSpacing - offsetX, 0, 0);
            Vector3 worldPos = transform.position + localPos;
            
            // Vẽ khung vuông phẳng (2D trên mặt phẳng XZ)
            Vector3 size = new Vector3(_slotSpacing, 0f, _slotSpacing); 
            Gizmos.DrawWireCube(worldPos, size);
        }
    }
#endif
}
