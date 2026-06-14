using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private float _cellSpacing = 1.0f; // Khoảng cách giữa các ô
    
    [Header("Visual")]
    [SerializeField] private Color _lightCellColor = new Color(0.9f, 0.85f, 0.7f); // Màu sáng (lẻ)
    [SerializeField] private Color _darkCellColor = new Color(0.55f, 0.5f, 0.35f);  // Màu tối hơn (chẵn)

    [Header("Score Settings")]
    [SerializeField] private int _scorePerExplosion = 100; // Có thể chỉnh điểm mỗi lần nổ đĩa tại đây


    // Dictionary lưu trạng thái các ô
    private Dictionary<Vector2Int, GridCell> _gridCells = new Dictionary<Vector2Int, GridCell>();
    private List<GridCell> _cellsToProcess = new List<GridCell>();
    private HashSet<GridCell> _cellsInQueue = new HashSet<GridCell>();
    private int _mergeSequenceCount = 0; // Đếm số lần merge liên tiếp để tăng tốc độ bay
    
    // Cache buffer để tránh GC alloc trong gameplay loop
    private readonly List<int> _gameOverTypeBuffer = new List<int>();
    
    // Cache buffer cho Transit Station (Epicenter relay logic)
    private readonly List<GridCell> _transitNeighbors = new List<GridCell>();
    private readonly Dictionary<int, int> _typeGlobalCount = new Dictionary<int, int>(); // type → tổng số miếng toàn vùng
    private readonly Dictionary<int, GridCell> _typeBestNeighbor = new Dictionary<int, GridCell>(); // type → đĩa hàng xóm có nhiều nhất

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
        // Kill tất cả BezierTween đang chạy để tránh tween treo trên transform đã bị Destroy
        if (BezierTween.Instance != null)
        {
            BezierTween.Instance.CancelAllTweens();
        }

        foreach (var cell in _gridCells.Values)
        {
            if (cell != null)
            {
                if (cell.CurrentPlate != null)
                {
                    cell.CurrentPlate.transform.DOKill(); // Kill shrink/scale tween trên plate
                    cell.CurrentPlate.ClearSlices();       // ClearSlices đã DOKill từng slice
                    if (ObjectPoolManager.Instance != null)
                    {
                        ObjectPoolManager.Instance.ReturnPizzaPlate(cell.CurrentPlate);
                    }
                }
                cell.transform.DOKill(); // Kill explosion-arc tween trên cell
                Destroy(cell.gameObject);
            }
        }
        _gridCells.Clear();
        _cellsToProcess.Clear();
        _cellsInQueue.Clear();
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

        // Đĩa tâm chấn (Priority 9) → Trạm Trung Chuyển thông minh
        if (centerPlate.Priority == 9)
        {
            return ProcessAsTransitStation(centerCell, centerPlate);
        }

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
                    else canPull = centerPlate.GetCountOf(targetPullType) > neighborPlate.GetCountOf(targetPullType);

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

    // ========================================================================
    // TRANSIT STATION: Đĩa tâm chấn (Priority 9) hoạt động như trạm trung chuyển
    // Phase 1: Tự nổ? Quét tổng type, nếu gom đủ 6 → hút về nổ
    // Phase 2: Relay - giúp hàng xóm gom tinh khiết
    // Phase 3: Push minority (cần >= 2 hàng xóm, tránh bounce loop)
    // ========================================================================

    private bool ProcessAsTransitStation(GridCell centerCell, PizzaPlate centerPlate)
    {
        // Phase 0: Đã tinh khiết 6/6 → Nổ
        if (centerPlate.IsFull() && centerPlate.IsFullAndPure())
        {
            ExplodePlate(centerCell);
            BezierTween.Instance.StartTween(centerCell.transform, centerCell.transform.position, arcHeight: 0, duration: 0.4f);
            return true;
        }

        // Thu thập hàng xóm có đĩa
        _transitNeighbors.Clear();
        foreach (var dir in _directions)
        {
            GridCell n = GetCell(centerCell.GridPosition + dir);
            if (n != null && n.IsOccupied) _transitNeighbors.Add(n);
        }
        if (_transitNeighbors.Count == 0) return ProcessNextMerge();

        // Xây bản đồ: type → tổng miếng (epicenter + hàng xóm) & hàng xóm giàu nhất
        _typeGlobalCount.Clear();
        _typeBestNeighbor.Clear();
        BuildTypeInventory(centerPlate, _transitNeighbors);

        // Phase 1: Tự nổ? Tìm type gom đủ 6 cho epicenter
        int selfExplodeType = FindSelfExplodeType(centerPlate);
        if (selfExplodeType != -1)
        {
            // Hút type mục tiêu từ hàng xóm
            bool pulled = TryTransitPull(centerCell, centerPlate, selfExplodeType);
            if (pulled) return true;

            // Đẩy type KHÔNG phải mục tiêu ra (cần >= 2 hàng xóm)
            if (_transitNeighbors.Count >= 2 && centerPlate.GetAvailableTypes().Count > 1)
            {
                bool pushed = TryTransitPushNonTarget(centerCell, centerPlate, selfExplodeType);
                if (pushed) return true;
            }

            // Đã xác định mục tiêu nổ nhưng kẹt (đĩa đầy hoặc thiếu hàng xóm)
            // KHÔNG cho Relay ghi đè mục tiêu → chờ lượt sau khi tình hình thay đổi
            CleanupEmptyNeighbors(centerCell);
            return ProcessNextMerge();
        }

        // Phase 2: Relay - gom type cho hàng xóm
        bool relayed = TryRelayForNeighbors(centerCell, centerPlate);
        if (relayed) return true;

        // Phase 3: Hút thường (như logic cũ cho non-epicenter)
        bool normalPull = TryStandardPull(centerCell, centerPlate);
        if (normalPull) return true;

        // Phase 4: Push minority nếu đầy mà bẩn (cần >= 2 hàng xóm)
        if (centerPlate.IsFull() && !centerPlate.IsFullAndPure() && _transitNeighbors.Count >= 2)
        {
            bool pushed = TryPushMinoritySlice(centerCell, centerPlate);
            if (pushed) return true;
        }

        // Dọn đĩa rỗng
        CleanupEmptyNeighbors(centerCell);
        return ProcessNextMerge();
    }

    private void BuildTypeInventory(PizzaPlate centerPlate, List<GridCell> neighbors)
    {
        // Đếm trên epicenter
        foreach (int t in centerPlate.GetAvailableTypes())
        {
            _typeGlobalCount[t] = centerPlate.GetCountOf(t);
        }
        // Đếm trên hàng xóm & tìm hàng xóm giàu nhất mỗi type
        foreach (var nc in neighbors)
        {
            PizzaPlate np = nc.CurrentPlate;
            foreach (int t in np.GetAvailableTypes())
            {
                int count = np.GetCountOf(t);
                if (_typeGlobalCount.ContainsKey(t))
                    _typeGlobalCount[t] += count;
                else
                    _typeGlobalCount[t] = count;

                if (!_typeBestNeighbor.ContainsKey(t) || count > _typeBestNeighbor[t].CurrentPlate.GetCountOf(t))
                {
                    _typeBestNeighbor[t] = nc;
                }
            }
        }
    }

    private int FindSelfExplodeType(PizzaPlate centerPlate)
    {
        int bestType = -1;
        int bestCenterCount = 0;
        foreach (var kvp in _typeGlobalCount)
        {
            if (kvp.Value >= 6 && centerPlate.HasType(kvp.Key))
            {
                int cc = centerPlate.GetCountOf(kvp.Key);
                if (cc > bestCenterCount) { bestType = kvp.Key; bestCenterCount = cc; }
            }
        }
        return bestType;
    }

    private bool TryTransitPull(GridCell centerCell, PizzaPlate centerPlate, int targetType)
    {
        if (centerPlate.IsFull()) return false;
        foreach (var nc in _transitNeighbors)
        {
            PizzaPlate np = nc.CurrentPlate;
            if (!np.HasType(targetType)) continue;

            PizzaSliceVisual slice = np.RemoveSliceOfType(targetType);
            if (slice != null && centerPlate.TryAddSlice(slice, out _))
            {
                AnimateSliceFly(slice, centerPlate);
                EnqueueCell(centerCell);
                CleanupEmptyNeighbors(centerCell);
                return true;
            }
        }
        return false;
    }

    private bool TryTransitPushNonTarget(GridCell centerCell, PizzaPlate centerPlate, int targetType)
    {
        // Tìm type KHÔNG phải mục tiêu để đẩy ra
        List<int> types = centerPlate.GetAvailableTypes();
        foreach (int t in types)
        {
            if (t == targetType) continue;
            // Tìm hàng xóm tốt nhất để nhận type này
            GridCell bestDest = null;
            int bestScore = int.MaxValue;
            foreach (var nc in _transitNeighbors)
            {
                PizzaPlate np = nc.CurrentPlate;
                if (np.IsFull()) continue;
                int countOnN = np.GetCountOf(t);
                int score = (countOnN > 0) ? (10 - countOnN) : (100 + np.GetTotalSlices());
                if (score < bestScore) { bestScore = score; bestDest = nc; }
            }
            if (bestDest != null)
            {
                PizzaSliceVisual slice = centerPlate.RemoveSliceOfType(t);
                if (slice != null)
                {
                    PizzaPlate destPlate = bestDest.CurrentPlate;
                    destPlate.TryAddSlice(slice, out _);
                    AnimateSliceFly(slice, destPlate);
                    EnqueueCell(bestDest);
                    EnqueueCell(centerCell);
                    return true;
                }
            }
        }
        return false;
    }

    private bool TryRelayForNeighbors(GridCell centerCell, PizzaPlate centerPlate)
    {
        if (_transitNeighbors.Count < 2) return false;
        if (centerPlate.IsFull()) return false;

        // Với mỗi type, tìm hàng xóm có ÍT nhất type đó (source) và hàng xóm có NHIỀU nhất (dest)
        foreach (var kvp in _typeBestNeighbor)
        {
            int type = kvp.Key;
            GridCell destCell = kvp.Value;
            PizzaPlate destPlate = destCell.CurrentPlate;
            if (destPlate.IsFull()) continue;

            // Tìm hàng xóm khác có type này nhưng ÍT hơn (source - "lạc chỗ")
            foreach (var nc in _transitNeighbors)
            {
                if (nc == destCell) continue;
                PizzaPlate srcPlate = nc.CurrentPlate;
                if (!srcPlate.HasType(type)) continue;

                // Chỉ relay nếu src có ÍT hơn dest (di chuyển từ ít → nhiều)
                if (srcPlate.GetCountOf(type) >= destPlate.GetCountOf(type)) continue;

                // ANTI-BOUNCE: Nếu epicenter đang 5/6 và type relay không phải đa số,
                // hút vào sẽ làm epicenter 6/6 không tinh khiết → IsPurging → tốn 1 vòng thừa
                if (centerPlate.GetTotalSlices() == 5 && centerPlate.GetCountOf(type) != 5) continue;

                // Hút từ src → epicenter (trung chuyển)
                PizzaSliceVisual slice = srcPlate.RemoveSliceOfType(type);
                if (slice != null && centerPlate.TryAddSlice(slice, out _))
                {
                    AnimateSliceFly(slice, centerPlate);
                    // Sau khi hút xong, lần xử lý tiếp epicenter sẽ push type này sang dest
                    centerPlate.IsPurging = false;
                    EnqueueCell(centerCell);
                    CleanupEmptyNeighbors(centerCell);
                    return true;
                }
            }
        }
        return false;
    }

    private bool TryStandardPull(GridCell centerCell, PizzaPlate centerPlate)
    {
        if (centerPlate.IsFull()) return false;
        List<int> centerTypes = centerPlate.GetAvailableTypes();
        if (centerTypes.Count == 0) return false;

        // Anti-bounce: đĩa 5/6 chỉ hút loại đa số
        if (centerPlate.GetTotalSlices() == 5)
        {
            int majorityType = centerTypes[0];
            centerTypes.Clear();
            centerTypes.Add(majorityType);
        }

        foreach (int pullType in centerTypes)
        {
            if (centerPlate.IsFull()) break;
            foreach (var dir in _directions)
            {
                GridCell neighbor = GetCell(centerCell.GridPosition + dir);
                if (neighbor == null || !neighbor.IsOccupied) continue;
                PizzaPlate np = neighbor.CurrentPlate;
                if (!np.HasType(pullType)) continue;

                bool canPull = centerPlate.Priority > np.Priority
                    || centerPlate.GetCountOf(pullType) > np.GetCountOf(pullType);
                if (canPull)
                {
                    PizzaSliceVisual slice = np.RemoveSliceOfType(pullType);
                    if (slice != null && centerPlate.TryAddSlice(slice, out _))
                    {
                        AnimateSliceFly(slice, centerPlate);
                        EnqueueCell(centerCell);
                        CleanupEmptyNeighbors(centerCell);
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private void AnimateSliceFly(PizzaSliceVisual slice, PizzaPlate targetPlate)
    {
        Vector3 targetPos = targetPlate.transform.position + new Vector3(0, targetPlate.SliceYOffset, 0);
        float flyDuration = Mathf.Max(0.08f, 0.25f - (_mergeSequenceCount * 0.02f));
        _mergeSequenceCount++;
        BezierTween.Instance.StartTween(slice.transform, targetPos, arcHeight: 1.5f, duration: flyDuration, onComplete: (t) => {
            slice.transform.localPosition = new Vector3(0, targetPlate.SliceYOffset, 0);
        });
    }

    private void CleanupEmptyNeighbors(GridCell centerCell)
    {
        foreach (var dir in _directions)
        {
            GridCell neighbor = GetCell(centerCell.GridPosition + dir);
            if (neighbor != null && neighbor.IsOccupied && neighbor.CurrentPlate.GetTotalSlices() == 0)
            {
                if (neighbor.CurrentPlate.Priority == 9) continue;
                neighbor.CurrentPlate.PlayShrinkAndReturn();
                neighbor.ClearPlate();
            }
        }
    }

    private bool TryPushMinoritySlice(GridCell centerCell, PizzaPlate centerPlate)
    {
        int pushType = centerPlate.GetMinorityType(-1);
        if (pushType == -1) return false;

        // ANTI-LOOP GUARD: Đếm hàng xóm có đĩa. 
        // Nếu chỉ có 1 đĩa cạnh → push xong nó dội ngược lại → lặp vô hạn.
        // Cần >= 2 hàng xóm để có chỗ tráo đổi mà không bị dội.
        int occupiedNeighborCount = 0;
        foreach (var dir in _directions)
        {
            GridCell n = GetCell(centerCell.GridPosition + dir);
            if (n != null && n.IsOccupied) occupiedNeighborCount++;
        }
        if (occupiedNeighborCount <= 1) return false;

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

    public static event System.Action<int> OnScoreAdded; // Sự kiện cộng điểm

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
        
        int scoreGained = _scorePerExplosion; // Dùng điểm cài đặt ở Inspector thay vì viết cứng
        
        // --- CHẠY CHỮ ĐIỂM SỐ BAY LÊN ---
        FloatingText scoreText = ObjectPoolManager.Instance.GetFloatingText();
        if (scoreText != null)
        {
            scoreText.Setup("+" + scoreGained, plate.transform.position + new Vector3(0, 1f, 0));
        }
        
        // Phát sự kiện cộng điểm cho LevelProgressUI nghe
        OnScoreAdded?.Invoke(scoreGained);
     
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

        // MỚI: Tìm hàng xóm priority cao nhất → tâm chấn mới
        GridCell newEpicenter = null;
        int highestPriority = -1;
        foreach (var dir in _directions)
        {
            GridCell neighbor = GetCell(cell.GridPosition + dir);
            if (neighbor != null && neighbor.IsOccupied)
            {
                if (neighbor.CurrentPlate.Priority > highestPriority)
                {
                    highestPriority = neighbor.CurrentPlate.Priority;
                    newEpicenter = neighbor;
                }
            }
        }

        if (newEpicenter != null)
        {
            CalculateLocalPriorities(newEpicenter); // BFS cục bộ, không reset toàn lưới
        }
    }

    /// <summary>
    /// BFS cục bộ: gán priority lan tỏa từ tâm chấn mới.
    /// Chỉ NÂNG priority (không hạ), nên không phá vỡ các đĩa đang chờ trong queue.
    /// </summary>
    private void CalculateLocalPriorities(GridCell epicenter)
    {
        if (epicenter == null || !epicenter.IsOccupied) return;

        // Gán tâm chấn mới = 9
        epicenter.CurrentPlate.Priority = 9;

        // BFS lan tỏa từ tâm chấn mới
        Queue<GridCell> bfsQueue = new Queue<GridCell>();
        HashSet<GridCell> visited = new HashSet<GridCell>();

        bfsQueue.Enqueue(epicenter);
        visited.Add(epicenter);

        while (bfsQueue.Count > 0)
        {
            GridCell current = bfsQueue.Dequeue();
            int currentPrio = current.CurrentPlate.Priority;

            foreach (var dir in _directions)
            {
                GridCell neighbor = GetCell(current.GridPosition + dir);
                if (neighbor == null || !neighbor.IsOccupied) continue;
                if (visited.Contains(neighbor)) continue;

                int newPrio = Mathf.Max(0, currentPrio - 1);

                // Chỉ cập nhật nếu priority mới CAO HƠN priority hiện tại
                // → không phá vỡ các đĩa đang chờ trong queue
                if (newPrio > neighbor.CurrentPlate.Priority)
                {
                    neighbor.CurrentPlate.Priority = newPrio;
                    visited.Add(neighbor);
                    if (newPrio > 0) bfsQueue.Enqueue(neighbor);
                }
            }
        }
    }

    /// <summary>
    /// Kiểm tra đĩa Priority 9 trống có được phép xóa chưa.
    /// Chỉ cho xóa khi tất cả hàng xóm đã ổn định (trống, hoặc tinh khiết đầy).
    /// </summary>
    private bool CanRemovePrivilegedPlate(GridCell cell)
    {
        foreach (var dir in _directions)
        {
            GridCell neighbor = GetCell(cell.GridPosition + dir);
            if (neighbor == null || !neighbor.IsOccupied) continue;

            PizzaPlate neighborPlate = neighbor.CurrentPlate;

            // Còn hàng xóm chưa tinh khiết VÀ còn miếng bánh → chưa được xóa
            if (neighborPlate.GetTotalSlices() > 0 && !neighborPlate.IsFullAndPure())
                return false;
        }
        return true;
    }

    public bool CleanupPrivilegedPlates()
    {
        bool anyRemoved = false;
        foreach (var kvp in _gridCells)
        {
            GridCell cell = kvp.Value;
            if (!cell.IsOccupied) continue;

            PizzaPlate plate = cell.CurrentPlate;
            if (plate.Priority == 9 && plate.GetTotalSlices() == 0)
            {
                // Chỉ xóa khi hàng xóm không còn di chuyển hợp lệ
                if (CanRemovePrivilegedPlate(cell))
                {
                    plate.PlayShrinkAndReturn();
                    cell.ClearPlate();
                    anyRemoved = true; // ← Gán true để cascade tiếp tục
                }
                else
                {
                    // Chưa được xóa → reset priority để không cản trở hàng xóm
                    plate.Priority = 0;
                }
            }
            else if (cell.IsOccupied)
            {
                cell.CurrentPlate.Priority = 0;
            }
        }
        return anyRemoved;
    }

    public bool CheckGameOver()
    {
        // Điều kiện 1: Lưới phải đầy hết
        foreach (var kvp in _gridCells)
        {
            if (!kvp.Value.IsOccupied) return false;
        }

        // Điều kiện 2: Kiểm tra xem có bất kỳ đĩa nào có thể Hút (Pull) hoặc Đẩy (Push) theo đúng logic game không
        foreach (var kvp in _gridCells)
        {
            GridCell cell = kvp.Value;
            if (!cell.IsOccupied) continue;

            PizzaPlate plateA = cell.CurrentPlate;

            // XÉT TRƯỜNG HỢP PUSH (Đẩy rác khi đĩa đầy nhưng không tinh khiết)
            if (plateA.IsFull() && !plateA.IsFullAndPure())
            {
                int pushType = plateA.GetMinorityType(-1);
                if (pushType != -1)
                {
                    foreach (var dir in _directions)
                    {
                        GridCell neighbor = GetCell(cell.GridPosition + dir);
                        if (neighbor != null && neighbor.IsOccupied)
                        {
                            PizzaPlate plateB = neighbor.CurrentPlate;
                            if (!plateB.IsFull())
                            {
                                // Luật chống dội rác (Bounce Loop)
                                if (!(plateB.GetTotalSlices() == 5 && plateB.GetCountOf(pushType) != 5))
                                {
                                    return false; // Có thể đẩy rác -> Chưa Game Over
                                }
                            }
                        }
                    }
                }
            }

            // XÉT TRƯỜNG HỢP PULL (Hút bánh cùng loại)
            if (!plateA.IsFull())
            {
                _gameOverTypeBuffer.Clear();
                _gameOverTypeBuffer.AddRange(plateA.GetAvailableTypes());

                foreach (var dir in _directions)
                {
                    GridCell neighbor = GetCell(cell.GridPosition + dir);
                    if (neighbor != null && neighbor.IsOccupied)
                    {
                        PizzaPlate plateB = neighbor.CurrentPlate;
                        foreach (int t in _gameOverTypeBuffer)
                        {
                            if (plateB.HasType(t))
                            {
                                // Nếu cả 2 đều có cùng loại bánh, và plateA không đầy -> Sẽ có giao dịch Hút
                                return false; // Còn nước đi -> Chưa Game Over
                            }
                        }
                    }
                }
            }
        }

        return true;
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
