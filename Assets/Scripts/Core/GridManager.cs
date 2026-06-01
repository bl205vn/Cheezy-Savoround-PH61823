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
    private List<GridCell> _cellsToProcess = new List<GridCell>();
    private HashSet<GridCell> _cellsInQueue = new HashSet<GridCell>();

    private void EnqueueCell(GridCell cell)
    {
        if (cell == null || !cell.IsOccupied) return;
        if (_cellsInQueue.Contains(cell)) return;
        _cellsToProcess.Add(cell);
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

    private void CalculatePriorities(GridCell startCell)
    {
        // Khởi tạo ưu tiên 0 cho toàn lưới
        foreach (var kvp in _gridCells)
        {
            if (kvp.Value.IsOccupied)
            {
                kvp.Value.CurrentPlate.Priority = 0;
            }
        }

        if (startCell == null || !startCell.IsOccupied) return;

        Queue<GridCell> bfsQueue = new Queue<GridCell>();
        HashSet<GridCell> visited = new HashSet<GridCell>();

        bfsQueue.Enqueue(startCell);
        visited.Add(startCell);
        startCell.CurrentPlate.Priority = 9; // Tâm chấn

        while (bfsQueue.Count > 0)
        {
            GridCell current = bfsQueue.Dequeue();
            int currentPrio = current.CurrentPlate.Priority;

            foreach (var dir in _directions)
            {
                GridCell neighbor = GetCell(current.GridPosition + dir);
                if (neighbor != null && neighbor.IsOccupied && !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    int nextPrio = Mathf.Max(0, currentPrio - 1);
                    neighbor.CurrentPlate.Priority = nextPrio;
                    if (nextPrio > 0)
                    {
                        bfsQueue.Enqueue(neighbor);
                    }
                }
            }
        }
    }

    private void HandlePlatePlaced(PizzaPlate plate, GridCell cell)
    {
        CalculatePriorities(cell); // Gán trọng số Dijkstra từ tâm chấn
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

        // Sắp xếp giảm dần theo Ưu tiên (Priority 9 xét trước)
        _cellsToProcess.Sort((a, b) => {
            int prioA = a.IsOccupied ? a.CurrentPlate.Priority : -1;
            int prioB = b.IsOccupied ? b.CurrentPlate.Priority : -1;
            return prioB.CompareTo(prioA);
        });

        GridCell centerCell = _cellsToProcess[0];
        _cellsToProcess.RemoveAt(0);
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
                EnqueueCell(centerCell);
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
                    // LUẬT "BLOOM SORT CHỐNG KẸT" KẾT HỢP ƯU TIÊN:
                    // Đĩa có Ưu tiên cao hơn luôn được quyền hút từ đĩa thấp hơn (hướng về tâm chấn).
                    // Nếu ưu tiên thấp hơn, chỉ được hút nếu số lượng lớn hơn HẲN.
                    // Nếu bằng nhau, số lượng lớn hơn hoặc bằng sẽ được hút.
                    bool canPull = false;
                    if (centerPlate.Priority > neighborPlate.Priority) canPull = true;
                    else if (centerPlate.Priority < neighborPlate.Priority) canPull = centerPlate.GetCountOf(type) > neighborPlate.GetCountOf(type);
                    else canPull = centerPlate.GetCountOf(type) >= neighborPlate.GetCountOf(type);

                    if (canPull)
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
                    // ĐẶC QUYỀN TRẠM TRUNG CHUYỂN: CHỈ Ưu tiên 9 không bị xóa ngay
                    if (neighbor.CurrentPlate.Priority == 9)
                    {
                        continue; 
                    }
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
                // ĐẶC QUYỀN TRẠM TRUNG CHUYỂN: CHỈ Ưu tiên 9 không nổ ngay
                if (centerPlate.Priority != 9)
                {
                    ExplodePlate(centerCell);
                }
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

            // TRẠM TRUNG CHUYỂN: Nếu đĩa lân cận TRỐNG (0 miếng)
            if (neighborPlate.GetTotalSlices() == 0)
            {
                // CHỈ cho phép đẩy vào đĩa trống nếu đĩa trống đó là Ưu tiên 9 (Tâm chấn)
                if (neighborPlate.Priority == 9)
                {
                    // Đẩy loại bánh ít nhất (thiểu số) sang đĩa trống
                    int minorityType = centerPlate.GetMinorityType(-1);
                    if (minorityType != -1)
                    {
                        PizzaSliceVisual pushSlice = centerPlate.RemoveSliceOfType(minorityType);
                        if (pushSlice != null)
                        {
                            neighborPlate.TryAddSlice(pushSlice, out _);
                            Vector3 targetPos = neighborPlate.transform.position + new Vector3(0, neighborPlate.SliceYOffset, 0);
                            BezierTween.Instance.StartTween(pushSlice.transform, targetPos, onComplete: (t) => {
                                pushSlice.transform.localPosition = new Vector3(0, neighborPlate.SliceYOffset, 0);
                            });
                            EnqueueCell(neighbor); // Hàng xóm nay đã có bánh, cần xét lại
                            return true;
                        }
                    }
                }
            }

            // Tìm loại bánh mà Center muốn gom thêm (Neighbor phải có)
            foreach (int pullType in centerTypes)
            {
                if (neighborPlate.HasType(pullType))
                {
                    // LUẬT 1: "Kẻ mạnh hút kẻ yếu" kết hợp Ưu Tiên
                    bool canPullSwap = false;
                    if (centerPlate.Priority > neighborPlate.Priority) canPullSwap = true;
                    else if (centerPlate.Priority < neighborPlate.Priority) canPullSwap = centerPlate.GetCountOf(pullType) > neighborPlate.GetCountOf(pullType);
                    else canPullSwap = centerPlate.GetCountOf(pullType) >= neighborPlate.GetCountOf(pullType);

                    if (!canPullSwap)
                    {
                        continue; 
                    }

                    // Tìm loại bánh ít nhất trên Center để đẩy sang Neighbor
                    int pushType = centerPlate.GetMinorityType(pullType);
                    if (pushType != -1)
                    {
                        // LUẬT 2: KHÔNG BAO GIỜ phá vỡ đa số
                        // Không đẩy đi loại bánh mà mình đang có nhiều hơn loại bánh hút vào
                        // (VD: Không đẩy Orange x4 để lấy Green x2)
                        if (centerPlate.GetCountOf(pushType) > centerPlate.GetCountOf(pullType))
                        {
                            continue;
                        }
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

    public bool CleanupPrivilegedPlates()
    {
        bool anyExploded = false;
        foreach (var kvp in _gridCells)
        {
            GridCell cell = kvp.Value;
            if (cell.IsOccupied)
            {
                PizzaPlate plate = cell.CurrentPlate;
                
                if (plate.Priority == 9)
                {
                    if (plate.GetTotalSlices() == 0)
                    {
                        ObjectPoolManager.Instance.ReturnPizzaPlate(plate);
                        cell.ClearPlate();
                    }
                    else if (plate.IsFullAndPure())
                    {
                        ExplodePlate(cell);
                        anyExploded = true;
                    }
                }
                
                if (cell.IsOccupied)
                {
                    cell.CurrentPlate.Priority = 0;
                }
            }
        }
        return anyExploded;
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
