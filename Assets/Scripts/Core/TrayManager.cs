using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class TrayManager : MonoBehaviour
{
    public static TrayManager Instance { get; private set; }

    [SerializeField] private float _slotSpacing; // Khoảng cách giữa các slot
    [SerializeField] private GameObject _pizzaPlatePrefab; // Prefab đĩa pizza

    public static event Action OnRefillComplete;

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
        GameEvents.OnPlatePlaced += HandlePlatePlacedOnGrid;
        GameStateManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnPlatePlaced -= HandlePlatePlacedOnGrid;
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
                _pendingRefill = true; // Bật cờ refill ngay khi có 1 slot trống
                break;
            }
        }
    }

    private void HandleStateChanged(IGameState newState)
    {
        if (newState is PlayingState && _pendingRefill)
        {
            RefillTray();
        }
    }

    private void LateUpdate()
    {
        // Khắc phục lỗi thứ tự thực thi Event (Race Condition)
        // Nếu GridManager chạy trước và đưa state về PlayingState ngay lập tức,
        // TrayManager sẽ bị lỡ event OnStateChanged. LateUpdate sẽ bắt lại trường hợp này.
        if (_pendingRefill && GameStateManager.Instance.CurrentState is PlayingState)
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

    public bool IsPlateInTray(PizzaPlate plate)
    {
        if (_slotPlates == null) return false;
        for (int i = 0; i < _slotPlates.Length; i++)
        {
            if (_slotPlates[i] == plate) return true;
        }
        return false;
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
            plate.FitToSize(_slotSpacing);
            plate.Initialize(anchor.transform);
            plate.GenerateRandomSlices(); // Sinh bánh ngẫu nhiên từ JSON config

            _slotPlates[i] = plate;
            refillCount++;
        }

        Debug.Log($"[TrayManager] Refill: Sinh {refillCount} đĩa mới trên khay.");
        
        if (refillCount > 0)
        {
            OnRefillComplete?.Invoke();
        }
    }

    private void ClearTray()
    {
        // Trả đĩa về Pool trước khi xóa
        if (_slotPlates != null)
        {
            for (int i = 0; i < _slotPlates.Length; i++)
            {
                if (_slotPlates[i] != null)
                {
                    _slotPlates[i].transform.DOKill(); // Kill tween treo trước khi trả pool
                    _slotPlates[i].ClearSlices();
                    if (ObjectPoolManager.Instance != null)
                    {
                        ObjectPoolManager.Instance.ReturnPizzaPlate(_slotPlates[i]);
                    }
                    _slotPlates[i] = null;
                }
            }
        }

        // Hủy anchor
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

    public List<int[]> CaptureState()
    {
        List<int[]> traySlots = new List<int[]>();
        if (_slotPlates == null) return traySlots;
        
        int maxSlices = LevelManager.CurrentLevelData.maxSlices;
        for (int i = 0; i < _slotPlates.Length; i++)
        {
            if (_slotPlates[i] != null)
            {
                int[] types = new int[maxSlices];
                PizzaSliceVisual[] slices = _slotPlates[i].Slices;
                for (int s = 0; s < maxSlices; s++)
                {
                    if (s < slices.Length && slices[s] != null)
                    {
                        types[s] = slices[s].TypeIndex;
                    }
                    else
                    {
                        types[s] = -1;
                    }
                }
                traySlots.Add(types);
            }
            else
            {
                traySlots.Add(null); // Slot trống
            }
        }
        return traySlots;
    }

    public void RestoreState(List<int[]> savedSlots)
    {
        if (savedSlots == null || _slotAnchors == null || savedSlots.Count != _slotAnchors.Count) return;
        
        _pendingRefill = false;
        int maxSlices = LevelManager.CurrentLevelData.maxSlices;

        for (int i = 0; i < savedSlots.Count; i++)
        {
            int[] types = savedSlots[i];
            
            // Xoá đĩa cũ nếu có
            if (_slotPlates[i] != null)
            {
                _slotPlates[i].ClearSlices();
                ObjectPoolManager.Instance.ReturnPizzaPlate(_slotPlates[i]);
                _slotPlates[i] = null;
            }

            if (types == null)
            {
                _pendingRefill = true; // Có slot trống
                continue;
            }

            GameObject anchor = _slotAnchors[i];
            Vector3 worldPos = anchor.transform.position;

            PizzaPlate plate = ObjectPoolManager.Instance.GetPizzaPlate();
            plate.transform.position = worldPos;
            plate.transform.rotation = Quaternion.identity;
            plate.transform.SetParent(anchor.transform);
            
            plate.FitToSize(_slotSpacing);
            plate.Initialize(anchor.transform);
            
            plate.RestoreSlices(types); // Sinh bánh cố định theo data
            plate.ApplyCurrentSkin();

            _slotPlates[i] = plate;
        }

        // Kiểm tra xem tất cả có trống không
        if (IsAllSlotsEmpty())
        {
            _pendingRefill = true;
        }
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
