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
    private int _mergeSequenceCount = 0; // Đếm số lần merge liên tiếp để tăng tốc độ bay

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
                
                GridCell cellComp = cellObj.GetComponent<GridCell>();
                if (cellComp == null) 
                {
                    cellComp = cellObj.AddComponent<GridCell>();
                }
                cellComp.Initialize(gridPos, isEven ? _darkCellColor : _lightCellColor);
                
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

    /// (Hàm ApplyCellColor đã bị xóa do GridCell tự quản lý màu sắc)

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
        _mergeSequenceCount = 0; // Reset bộ đếm khi bắt đầu một combo mới
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
                BezierTween.Instance.StartTween(centerCell.transform, centerCell.transform.position, arcHeight: 0, duration: 0.4f);
                return true;
            }
            
            // Nếu đĩa đầy mà chứa rác -> Bật chế độ xả rác liên tục (Áp dụng cho mọi cấp độ ưu tiên)
            centerPlate.IsPurging = true;

            // --- PUSH SLICES WHEN FULL BUT NOT PURE ---
            bool pushed = TryPushMinoritySlice(centerCell, centerPlate);
            if (pushed)
            {
                return true;
            }
            
            return ProcessNextMerge();
        }

        // --- KIỂM TRA TRẠNG THÁI XẢ RÁC ---
        if (centerPlate.IsPurging)
        {
            if (centerPlate.GetAvailableTypes().Count <= 1)
            {
                // Đã xả hết rác (chỉ còn 1 loại) -> Tắt chế độ xả rác, quay lại chế độ Hút
                centerPlate.IsPurging = false;
            }
            else
            {
                // Vẫn còn rác -> Tiếp tục Push thay vì Hút
                bool pushed = TryPushMinoritySlice(centerCell, centerPlate);
                if (pushed)
                {
                    return true;
                }
                else
                {
                    // Kẹt không xả được nữa (các đĩa xung quanh đều đầy), tạm thời tắt cờ để có thể Hút
                    centerPlate.IsPurging = false;
                }
            }
        }

        // --- BƯỚC 1 & 2: Tìm MỘT giao dịch (transfer) tốt nhất ---
        // Sửa lỗi: Quét QUA TẤT CẢ các loại bánh có trên đĩa (ưu tiên loại nhiều nhất trước).
        List<int> centerTypes = centerPlate.GetAvailableTypes();

        // ANTI-BOUNCE LOOP: Nếu đĩa đang có 5 miếng (chỉ còn 1 chỗ trống),
        // nó sẽ CHỈ HÚT loại bánh chiếm đa số. Nếu hút loại thiểu số, nó sẽ bị đầy 6/6
        // và lập tức xả loại thiểu số đó ra, tạo vòng lặp vô tận (Bounce Loop 4:2 vs 4:4).
        if (centerPlate.GetTotalSlices() == 5 && centerTypes.Count > 0)
        {
            int majorityType = centerTypes[0];
            centerTypes.Clear();
            centerTypes.Add(majorityType);
        }

        bool anyTransfer = false;
        
        foreach (int targetPullType in centerTypes)
        {
            if (centerPlate.IsFull()) break;

            foreach (var dir in _directions)
            {
                GridCell neighbor = GetCell(centerCell.GridPosition + dir);
                if (neighbor == null || !neighbor.IsOccupied) continue;

                PizzaPlate neighborPlate = neighbor.CurrentPlate;

                if (neighborPlate.HasType(targetPullType))
                {
                    // Ưu tiên 9 hút mạnh hơn
                    bool canPull = false;
                    if (centerPlate.Priority > neighborPlate.Priority) canPull = true;
                    else if (centerPlate.Priority < neighborPlate.Priority) canPull = centerPlate.GetCountOf(targetPullType) > neighborPlate.GetCountOf(targetPullType);
                    else canPull = centerPlate.GetCountOf(targetPullType) >= neighborPlate.GetCountOf(targetPullType);

                    if (canPull)
                    {
                        PizzaSliceVisual slice = neighborPlate.RemoveSliceOfType(targetPullType);
                        if (slice != null)
                        {
                            if (centerPlate.TryAddSlice(slice, out int addedIndex))
                            {
                                Vector3 targetWorldPos = centerPlate.transform.position + new Vector3(0, centerPlate.SliceYOffset, 0);
                                
                                // Tăng tốc độ bay: Mỗi lần bay nhanh hơn một chút, tối đa 0.08s
                                float flyDuration = Mathf.Max(0.08f, 0.25f - (_mergeSequenceCount * 0.02f));
                                _mergeSequenceCount++;

                                BezierTween.Instance.StartTween(slice.transform, targetWorldPos, arcHeight: 1.5f, duration: flyDuration, onComplete: (t) => {
                                    slice.transform.localPosition = new Vector3(0, centerPlate.SliceYOffset, 0);
                                });
                                
                                anyTransfer = true;
                                break; // Chỉ hút 1 miếng cho mỗi lượt để tạo hiệu ứng bay lần lượt!
                            }
                        }
                    }
                }
            }
            if (anyTransfer) break; // Chỉ hút 1 miếng cho mỗi lượt!
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
                    neighbor.CurrentPlate.PlayShrinkAndReturn();
                    neighbor.ClearPlate();
                }
            }
        }

        // --- BƯỚC 3: Xử lý Cascade / Trả về kết quả ---
        if (anyTransfer)
        {
            // Bỏ lại vào hàng đợi để check tiếp sau khi tween bay xong
            EnqueueCell(centerCell);
            return true;
        }
        else
        {
            // Không có bất kỳ giao dịch Hút/Đẩy nào xảy ra, chuyển sang xử lý đĩa tiếp theo trong queue
            return ProcessNextMerge();
        }
    }

    private bool TryPushMinoritySlice(GridCell centerCell, PizzaPlate centerPlate)
    {
        int pushType = centerPlate.GetMinorityType(-1);
        if (pushType == -1) return false;

        GridCell bestPushNeighbor = null;
        int bestPushScore = int.MaxValue;

        foreach (var dir in _directions)
        {
            GridCell neighbor = GetCell(centerCell.GridPosition + dir);
            if (neighbor == null || !neighbor.IsOccupied) continue;

            PizzaPlate neighborPlate = neighbor.CurrentPlate;
            if (neighborPlate.IsFull()) continue; // Phải còn chỗ mới đẩy được

            // CHỐNG DỘI RÁC (BOUNCE LOOP):
            // Nếu đĩa lân cận chỉ còn 1 chỗ trống (5/6), việc đẩy rác vào sẽ làm nó ĐẦY (6/6).
            // Khi bị ĐẦY, nó sẽ bật IsPurging và dội ngược rác lại cho chính mình.
            // NGOẠI TRỪ trường hợp: đĩa kia đang có 5 miếng cùng loại với miếng sắp đẩy, đẩy vào là Tinh Khiết và Nổ!
            if (neighborPlate.GetTotalSlices() == 5 && neighborPlate.GetCountOf(pushType) != 5)
            {
                continue; // Cấm đẩy rác chót vào đĩa sắp đầy để tránh loop!
            }

            // Tính điểm chọn "thùng rác" tốt nhất theo ý người dùng
            int score = 0;
            int countOnNeighbor = neighborPlate.GetCountOf(pushType);
            
            if (countOnNeighbor == 5)
            {
                // Cực kỳ ưu tiên: Xả miếng này vào là đủ 6/6 tinh khiết -> Nổ luôn!
                score = -100;
            }
            else if (countOnNeighbor > 0)
            {
                // Ưu tiên đĩa đang có NHIỀU bánh cùng loại hơn (để gom lại cho nhanh nổ)
                // Số càng nhỏ càng ưu tiên -> lấy 10 trừ đi
                // VD: Có 4 miếng -> score = 6. Có 1 miếng -> score = 9.
                score = 10 - countOnNeighbor; 
            }
            else
            {
                // Phạt nếu đĩa lân cận chưa có loại bánh này (100)
                // NHƯNG cộng thêm số lượng tổng bánh đang có, để ưu tiên đĩa càng trống càng tốt!
                score = 100 + neighborPlate.GetTotalSlices(); 
            }

            if (score < bestPushScore)
            {
                bestPushScore = score;
                bestPushNeighbor = neighbor;
            }
        }

        if (bestPushNeighbor != null)
        {
            PizzaPlate neighborPlate = bestPushNeighbor.CurrentPlate;
            PizzaSliceVisual pushSlice = centerPlate.RemoveSliceOfType(pushType);
            if (pushSlice != null)
            {
                neighborPlate.TryAddSlice(pushSlice, out _);
                Vector3 targetPos = neighborPlate.transform.position + new Vector3(0, neighborPlate.SliceYOffset, 0);
                
                float flyDuration = Mathf.Max(0.08f, 0.25f - (_mergeSequenceCount * 0.02f));
                _mergeSequenceCount++;
                
                BezierTween.Instance.StartTween(pushSlice.transform, targetPos, arcHeight: 1.5f, duration: flyDuration, onComplete: (t) => {
                    pushSlice.transform.localPosition = new Vector3(0, neighborPlate.SliceYOffset, 0);
                });
                
                EnqueueCell(bestPushNeighbor);
                EnqueueCell(centerCell); // Đĩa không còn Full, cần cho vào queue để tiếp tục xả rác hoặc hút
                return true;
            }
        }
        
        return false;
    }

    private void ExplodePlate(GridCell cell)
    {
        Debug.Log($"[Merge] NỔ ĐĨA tại {cell.GridPosition}! Giải phóng ô.");
        PizzaPlate plate = cell.CurrentPlate;
        
        // --- CHẠY HIỆU ỨNG VFX NỔ (Scale tự động theo đĩa) ---
        PooledVFX explosionVFX = ObjectPoolManager.Instance.GetExplosionVFX();
        if (explosionVFX != null)
        {
            Vector3 vfxPos = plate.transform.position + new Vector3(0, 0.5f, 0);
            explosionVFX.PlayAt(vfxPos, plate.transform.localScale);
        }

        // --- PHÁT ÂM THANH NỔ (Có Pitch Shift) ---
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayExplosionSound();
        }
        
        // --- CHẠY CHỮ ĐIỂM SỐ BAY LÊN ---
        FloatingText scoreText = ObjectPoolManager.Instance.GetFloatingText();
        if (scoreText != null)
        {
            scoreText.Setup("+100", plate.transform.position + new Vector3(0, 1f, 0));
        }
     
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
                        plate.PlayShrinkAndReturn();
                        cell.ClearPlate();
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
