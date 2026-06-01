using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private float _cellSpacing = 1.0f; // Khoảng cách giữa các ô
    
    [Header("Visual")]
    [SerializeField] private Color _lightCellColor = new Color(0.9f, 0.85f, 0.7f); // Màu sáng (lẻ)
    [SerializeField] private Color _darkCellColor = new Color(0.55f, 0.5f, 0.35f);  // Màu tối hơn (chẵn)

    // Dictionary lưu trạng thái các ô
    private Dictionary<Vector2Int, GridCell> _gridCells = new Dictionary<Vector2Int, GridCell>();
    private Queue<GridCell> _cellsToProcess = new Queue<GridCell>();
    private HashSet<GridCell> _cellsInQueue = new HashSet<GridCell>();

    private void EnqueueCell(GridCell cell)
    {
        if (cell == null || !cell.IsOccupied) return;
        if (_cellsInQueue.Contains(cell)) return;
        _cellsToProcess.Enqueue(cell);
        _cellsInQueue.Add(cell);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Cache cho thuật toán quét để tránh cấp phát rác (Zero GC)
    private static readonly Vector2Int[] _directions = new Vector2Int[]
    {
        Vector2Int.up,    // (0, 1)
        Vector2Int.down,  // (0, -1)
        Vector2Int.left,  // (-1, 0)
        Vector2Int.right  // (1, 0)
    };
    private readonly List<GridCell> _matchingCells = new List<GridCell>();

    public void GenerateGrid(int levelId, int width, int height)
    {
        ClearGrid();

        // Tính toán offset để căn lưới ra giữa (center)
        float offsetX = (width - 1) * _cellSpacing * 0.5f;
        float offsetZ = (height - 1) * _cellSpacing * 0.5f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector3 localPos = new Vector3(x * _cellSpacing - offsetX, 0, y * _cellSpacing - offsetZ);
                Vector3 worldPos = transform.position + localPos; // Sinh lưới theo vị trí thực của GridManager
                
                GameObject cellObj = Instantiate(_cellPrefab, worldPos, Quaternion.identity, transform);
                cellObj.name = $"Cell_{x}_{y}";
                
                // Ép scale prefab theo kích thước ô grid
                FitPrefabToCell(cellObj);
                
                // Checkerboard: chẵn = tối, lẻ = sáng
                bool isEven = (x + y) % 2 == 0;
                ApplyCellColor(cellObj, isEven ? _darkCellColor : _lightCellColor);
                
                GridCell cellComp = cellObj.GetComponent<GridCell>();
                if (cellComp == null) 
                {
                    cellComp = cellObj.AddComponent<GridCell>();
                }
                cellComp.Initialize(gridPos);
                
                _gridCells[gridPos] = cellComp;
            }
        }
        
        Debug.Log($"[GridManager] Đã tạo thành công lưới {width}x{height} cho màn {levelId}");
    }

    /// <summary>
    /// Ép scale prefab vào đúng kích thước 1 ô grid dựa trên Renderer bounds.
    /// </summary>
    private void FitPrefabToCell(GameObject cellObj)
    {
        Renderer rend = cellObj.GetComponentInChildren<Renderer>();
        if (rend == null) return;

        Vector3 currentSize = rend.bounds.size;
        
        // Chỉ scale theo trục X và Z (mặt phẳng ngang), giữ nguyên tỷ lệ Y
        float scaleX = (currentSize.x > 0.001f) ? (_cellSpacing / currentSize.x) : 1f;
        float scaleZ = (currentSize.z > 0.001f) ? (_cellSpacing / currentSize.z) : 1f;
        
        // Dùng scale nhỏ nhất để giữ tỷ lệ, khít hoàn toàn
        float uniformScale = Mathf.Min(scaleX, scaleZ);
        
        cellObj.transform.localScale = cellObj.transform.localScale * uniformScale;
    }

    /// <summary>
    /// Đổi màu tất cả Renderer con của cell (Material).
    /// Dùng MaterialPropertyBlock để tránh tạo instance material mới gây rác.
    /// </summary>
    private void ApplyCellColor(GameObject cellObj, Color color)
    {
        Renderer[] renderers = cellObj.GetComponentsInChildren<Renderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        
        foreach (var rend in renderers)
        {
            rend.GetPropertyBlock(block);
            block.SetColor("_Color", color);     // Standard shader
            block.SetColor("_BaseColor", color);  // URP/HDRP shader
            rend.SetPropertyBlock(block);
        }
    }

    private void ClearGrid()
    {
        foreach (var cell in _gridCells.Values)
        {
            if (cell != null)
            {
                Destroy(cell.gameObject);
            }
        }
        _gridCells.Clear();
    }

    private void OnEnable()
    {
        InputManager.OnPlatePlaced += HandlePlatePlaced;
    }

    private void OnDisable()
    {
        InputManager.OnPlatePlaced -= HandlePlatePlaced;
    }

    public GridCell GetCell(Vector2Int gridPos)
    {
        if (_gridCells.TryGetValue(gridPos, out GridCell cell))
        {
            return cell;
        }
        return null;
    }

    private void HandlePlatePlaced(PizzaPlate plate, GridCell cell)
    {
        EnqueueCell(cell);
        
        // Đưa các hàng xóm vào hàng đợi. Đảm bảo luật "Kẻ mạnh hút kẻ yếu" được thực thi 2 chiều
        foreach (var dir in _directions)
        {
            GridCell neighbor = GetCell(cell.GridPosition + dir);
            if (neighbor != null && neighbor.IsOccupied)
            {
                EnqueueCell(neighbor);
            }
        }
        
        GameStateManager.Instance.ChangeState(GameStateManager.Instance.CheckingCombo);
    }

    public bool ProcessNextMerge()
    {
        if (_cellsToProcess.Count == 0) return false;

        GridCell centerCell = _cellsToProcess.Dequeue();
        _cellsInQueue.Remove(centerCell);
        
        if (centerCell == null || !centerCell.IsOccupied) return ProcessNextMerge();

        PizzaPlate centerPlate = centerCell.CurrentPlate;
        if (centerPlate.IsFull())
        {
            if (centerPlate.IsFullAndPure())
            {
                ExplodePlate(centerCell);
                return ProcessNextMerge();
            }
            
            // --- NEW LOGIC: SWAP SLICES WHEN FULL BUT NOT PURE ---
            bool swapped = TrySwapMinoritySlice(centerCell, centerPlate);
            if (swapped)
            {
                // Bỏ lại vào hàng đợi để check tiếp sau khi tween bay xong
                _cellsToProcess.Enqueue(centerCell);
                return true;
            }
            
            return ProcessNextMerge();
        }

        // --- BƯỚC 1: Thu thập tất cả transfer cần làm ---
        // Mỗi hướng chỉ 1 miếng (giống Bloom Sort)
        var pendingTransfers = new List<(int typeIndex, PizzaPlate source)>();
        List<int> centerTypes = centerPlate.GetAvailableTypes();
        
        foreach (var dir in _directions)
        {
            GridCell neighbor = GetCell(centerCell.GridPosition + dir);
            if (neighbor == null || !neighbor.IsOccupied) continue;

            PizzaPlate neighborPlate = neighbor.CurrentPlate;

            // Tìm 1 loại pizza trong đĩa giữa mà đĩa lân cận cũng có
            foreach (int type in centerTypes)
            {
                if (neighborPlate.HasType(type))
                {
                    // LUẬT "BLOOM SORT CHỐNG KẸT":
                    // Chỉ cho phép Đĩa đang xét hút nếu số lượng bánh loại đó của nó >= số lượng của hàng xóm.
                    // Nếu ít hơn, đĩa hàng xóm sẽ là người hút (vì hàng xóm cũng nằm trong _cellsToProcess).
                    // Điều này ngăn chặn hoàn toàn vòng lặp hút vô tận qua lại.
                    if (centerPlate.GetCountOf(type) >= neighborPlate.GetCountOf(type))
                    {
                        pendingTransfers.Add((type, neighborPlate));
                        break; // 1 hướng chỉ cho 1 miếng di chuyển mỗi lượt
                    }
                }
            }
        }

        // --- BƯỚC 2: Thực thi tất cả transfer cùng lúc ---
        bool anyTransfer = false;
        foreach (var transfer in pendingTransfers)
        {
            if (centerPlate.IsFull()) break; // Đĩa giữa đã đầy thì ngưng

            PizzaSliceVisual slice = transfer.source.RemoveSliceOfType(transfer.typeIndex);
            if (slice != null)
            {
                if (centerPlate.TryAddSlice(slice, out int addedIndex))
                {
                    Vector3 targetWorldPos = centerPlate.transform.position + new Vector3(0, centerPlate.SliceYOffset, 0);
                    BezierTween.Instance.StartTween(slice.transform, targetWorldPos, onComplete: (t) => {
                        slice.transform.localPosition = new Vector3(0, centerPlate.SliceYOffset, 0);
                    });
                    anyTransfer = true;
                }
            }
        }

        // Dọn dẹp các đĩa bị hút sạch bánh
        foreach (var dir in _directions)
        {
            GridCell neighbor = GetCell(centerCell.GridPosition + dir);
            if (neighbor != null && neighbor.IsOccupied)
            {
                if (neighbor.CurrentPlate.GetTotalSlices() == 0)
                {
                    ObjectPoolManager.Instance.ReturnPizzaPlate(neighbor.CurrentPlate);
                    neighbor.ClearPlate();
                }
            }
        }

        // --- BƯỚC 3: Xử lý Cascade / Kiểm tra nổ đĩa ---
        if (anyTransfer)
        {
            // Bỏ lại vào hàng đợi để check tiếp sau khi tween bay xong
            EnqueueCell(centerCell);
            return true;
        }
        else
        {
            if (centerPlate.IsFullAndPure())
            {
                ExplodePlate(centerCell);
            }
            return ProcessNextMerge();
        }
    }

    private bool TrySwapMinoritySlice(GridCell centerCell, PizzaPlate centerPlate)
    {
        List<int> centerTypes = centerPlate.GetAvailableTypes(); // Đã sort giảm dần theo số lượng

        foreach (var dir in _directions)
        {
            GridCell neighbor = GetCell(centerCell.GridPosition + dir);
            if (neighbor == null || !neighbor.IsOccupied) continue;

            PizzaPlate neighborPlate = neighbor.CurrentPlate;

            // Tìm loại bánh mà Center muốn gom thêm (Neighbor phải có)
            foreach (int pullType in centerTypes)
            {
                if (neighborPlate.HasType(pullType))
                {
                    // Tìm loại bánh ít nhất trên Center để đẩy sang Neighbor
                    int pushType = centerPlate.GetMinorityType(pullType);
                    if (pushType != -1)
                    {
                        PizzaSliceVisual pullSlice = neighborPlate.RemoveSliceOfType(pullType);
                        if (pullSlice != null)
                        {
                            PizzaSliceVisual pushSlice = centerPlate.RemoveSliceOfType(pushType);
                            if (pushSlice != null)
                            {
                                centerPlate.TryAddSlice(pullSlice, out _);
                                neighborPlate.TryAddSlice(pushSlice, out _);

                                Vector3 targetCenterPos = centerPlate.transform.position + new Vector3(0, centerPlate.SliceYOffset, 0);
                                BezierTween.Instance.StartTween(pullSlice.transform, targetCenterPos, onComplete: (t) => {
                                    pullSlice.transform.localPosition = new Vector3(0, centerPlate.SliceYOffset, 0);
                                });

                                Vector3 targetNeighborPos = neighborPlate.transform.position + new Vector3(0, neighborPlate.SliceYOffset, 0);
                                BezierTween.Instance.StartTween(pushSlice.transform, targetNeighborPos, onComplete: (t) => {
                                    pushSlice.transform.localPosition = new Vector3(0, neighborPlate.SliceYOffset, 0);
                                });

                                EnqueueCell(neighbor);
                                return true;
                            }
                            else
                            {
                                neighborPlate.TryAddSlice(pullSlice, out _);
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    private void ExplodePlate(GridCell cell)
    {
        Debug.Log($"[Merge] NỔ ĐĨA tại {cell.GridPosition}! Giải phóng ô.");
        PizzaPlate plate = cell.CurrentPlate;
        plate.ClearSlices(); // Trả pool miếng bánh
        ObjectPoolManager.Instance.ReturnPizzaPlate(plate);
        cell.ClearPlate();
        
        // Combo Cascade: Re-check các đĩa xung quanh
        foreach(var dir in _directions)
        {
            GridCell neighbor = GetCell(cell.GridPosition + dir);
            if (neighbor != null && neighbor.IsOccupied)
            {
                EnqueueCell(neighbor);
            }
        }
    }

#if UNITY_EDITOR
    public void DrawGizmos(int width, int height)
    {
        Gizmos.color = Color.green;
        float offsetX = (width - 1) * _cellSpacing * 0.5f;
        float offsetZ = (height - 1) * _cellSpacing * 0.5f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 localPos = new Vector3(x * _cellSpacing - offsetX, 0, y * _cellSpacing - offsetZ);
                Vector3 worldPos = transform.position + localPos; // Vẽ theo vị trí của GridManager
                
                // Vẽ khung vuông phẳng (2D trên mặt phẳng XZ) kích thước bằng 95% ô lưới
                Vector3 size = new Vector3(_cellSpacing, 0f, _cellSpacing) * 1f; 
                Gizmos.DrawWireCube(worldPos, size);
            }
        }
    }
#endif
}
