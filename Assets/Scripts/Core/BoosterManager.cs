using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class BoosterManager : MonoBehaviour
{
    public static BoosterManager Instance { get; private set; }

    public enum Phase { Idle, MoveSource, MoveDest, TrashTarget }
    private Phase _currentPhase = Phase.Idle;

    public bool IsWaitingForTarget => _currentPhase != Phase.Idle;

    private GridCell _moveSourceCell;
    private System.Action _onConsumeCallback;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ActivateBooster(BoostButton.BoosterType type, System.Action onConsume)
    {
        if (_currentPhase != Phase.Idle) return; // Đang chạy boost khác
        _onConsumeCallback = onConsume;

        switch (type)
        {
            case BoostButton.BoosterType.Cutter:
                if (ApplyCutter()) FinishBooster(); else CancelBooster();
                break;
            case BoostButton.BoosterType.Sauce:
                if (ApplySauce()) FinishBooster(); else CancelBooster();
                break;
            case BoostButton.BoosterType.Move:
                _currentPhase = Phase.MoveSource;
                break;
            case BoostButton.BoosterType.Trash:
                _currentPhase = Phase.TrashTarget;
                break;
        }
    }

    private void CancelBooster()
    {
        _currentPhase = Phase.Idle;
        _moveSourceCell = null;
        _onConsumeCallback = null;
        Debug.Log("[BoosterManager] Booster cancelled (no valid target or user aborted).");
    }

    private void FinishBooster()
    {
        _currentPhase = Phase.Idle;
        _moveSourceCell = null;
        
        // Trừ số lượng booster, update UI, Trigger Event...
        _onConsumeCallback?.Invoke();
        _onConsumeCallback = null;
    }

    // InputManager sẽ gọi hàm này nếu IsWaitingForTarget = true
    public void HandleTap(RaycastHit hit)
    {
        if (_currentPhase == Phase.Idle) return;

        GridCell targetCell = null;

        if (hit.collider.TryGetComponent(out GridCell cell))
        {
            targetCell = cell;
        }
        else if (hit.collider.TryGetComponent(out PizzaPlate plate))
        {
            if (GridManager.Instance != null)
            {
                targetCell = GridManager.Instance.GetCellOfPlate(plate);
            }
        }

        if (targetCell == null) return;

        if (_currentPhase == Phase.MoveSource)
        {
            if (targetCell.IsOccupied)
            {
                _moveSourceCell = targetCell;
                _currentPhase = Phase.MoveDest;
                // Có thể thêm hiệu ứng nhấp nháy cho đĩa nguồn ở đây
                targetCell.CurrentPlate.transform.DOScale(1.1f, 0.2f).SetLoops(-1, LoopType.Yoyo).SetId("MoveHighlight");
            }
        }
        else if (_currentPhase == Phase.MoveDest)
        {
            if (targetCell == _moveSourceCell)
            {
                // Tap lại nguồn -> Hủy
                DOTween.Kill("MoveHighlight");
                _moveSourceCell.CurrentPlate.transform.localScale = _moveSourceCell.CurrentPlate.BaseScale;
                CancelBooster();
                return;
            }

            DOTween.Kill("MoveHighlight");
            _moveSourceCell.CurrentPlate.transform.localScale = _moveSourceCell.CurrentPlate.BaseScale;

            if (ApplyMove(_moveSourceCell, targetCell))
            {
                FinishBooster();
            }
            else
            {
                CancelBooster();
            }
        }
        else if (_currentPhase == Phase.TrashTarget)
        {
            if (targetCell.IsOccupied)
            {
                if (ApplyTrash(targetCell))
                {
                    FinishBooster();
                }
                else
                {
                    CancelBooster();
                }
            }
        }
    }

    // ----------------------------------------------------
    // LOGIC CỤ THỂ CỦA 4 BOOSTERS
    // ----------------------------------------------------

    private bool ApplyCutter()
    {
        GridManager grid = GridManager.Instance;
        if (grid == null) return false;

        GridCell targetCell = null;
        GridCell emptyAdjacentCell = null;
        float bestScore = -1;

        // Định nghĩa 4 hướng
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        // Tìm đĩa đang thiếu miếng (không đầy, không rỗng) và có ít nhất 1 ô trống kề bên
        foreach (var cell in grid.GetAllCells())
        {
            if (!cell.IsOccupied) continue;
            PizzaPlate p = cell.CurrentPlate;
            int totalSlices = p.GetTotalSlices();
            if (totalSlices == 0 || totalSlices == 6) continue;

            // Tìm ô trống kề bên
            GridCell emptyNeighbor = null;
            foreach (var dir in dirs)
            {
                GridCell neighbor = grid.GetCell(cell.GridPosition + dir);
                if (neighbor != null && !neighbor.IsOccupied)
                {
                    emptyNeighbor = neighbor;
                    break;
                }
            }

            if (emptyNeighbor == null) continue;

            // Tính điểm ưu tiên (đĩa càng gần tinh khiết và càng đầy càng tốt)
            int majorityType = p.GetMajorityType();
            int majorityCount = p.GetCountOf(majorityType);
            float purity = (float)majorityCount / totalSlices;
            float score = purity * 10 + majorityCount;

            if (score > bestScore)
            {
                bestScore = score;
                targetCell = cell;
                emptyAdjacentCell = emptyNeighbor;
            }
        }

        if (targetCell == null || emptyAdjacentCell == null) return false;

        PizzaPlate targetPlate = targetCell.CurrentPlate;
        int targetMajority = targetPlate.GetMajorityType();
        int slicesNeeded = 6 - targetPlate.GetCountOf(targetMajority);
        
        // Giới hạn max 6
        slicesNeeded = Mathf.Clamp(slicesNeeded, 1, 6);

        // Spawn đĩa mới
        if (ObjectPoolManager.Instance != null)
        {
            PizzaPlate newPlate = ObjectPoolManager.Instance.GetPizzaPlate();
            newPlate.ClearSlices();
            
            // Đặt đĩa xuống lưới ngay lập tức để lấy chuẩn Scale và Parent
            emptyAdjacentCell.PlacePlateInstant(newPlate);
            
            Sequence seq = DOTween.Sequence();

            for (int i = 0; i < slicesNeeded; i++)
            {
                PizzaSliceVisual newSlice = ObjectPoolManager.Instance.GetPizzaSlice();
                newSlice.SetVisual(targetMajority);
                
                // Set vị trí ban đầu trên cao
                newSlice.transform.position = newPlate.transform.position + Vector3.up * (3f + i * 0.5f);
                
                newPlate.TryAddSlice(newSlice, out _);
                newSlice.transform.localScale = Vector3.one; // Khắc phục lỗi sai scale
                
                // Rớt xuống đĩa
                Tween dropTween = newSlice.transform.DOLocalMove(new Vector3(0, newPlate.SliceYOffset, 0), 0.3f).SetEase(Ease.OutBack);
                seq.Insert(i * 0.1f, dropTween);
            }

            // Đợi rớt xong hết mới gộp nổ
            seq.OnComplete(() => 
            {
                if (grid != null) grid.TriggerCascade(emptyAdjacentCell);
            });
            
            return true;
        }

        return false;
    }

    private bool ApplySauce()
    {
        GridManager grid = GridManager.Instance;
        if (grid == null || ObjectPoolManager.Instance == null) return false;

        GridCell bestCell = null;
        int maxSameTypeSlices = -1;

        // Tìm đĩa có nhiều miếng cùng type nhất (chưa nổ)
        foreach (var cell in grid.GetAllCells())
        {
            if (!cell.IsOccupied) continue;
            PizzaPlate p = cell.CurrentPlate;
            if (p.GetTotalSlices() == 0 || p.IsFullAndPure()) continue; // Bỏ qua rỗng hoặc đã nổ

            int majorityType = p.GetMajorityType();
            int count = p.GetCountOf(majorityType);
            
            if (count > maxSameTypeSlices)
            {
                maxSameTypeSlices = count;
                bestCell = cell;
            }
        }

        if (bestCell == null) return false;

        PizzaPlate targetPlate = bestCell.CurrentPlate;
        int targetMajority = targetPlate.GetMajorityType();

        // 1. Xóa các miếng thiểu số trước (nếu có)
        var types = targetPlate.GetAvailableTypes();
        foreach (int t in types)
        {
            if (t == targetMajority) continue;
            int count = targetPlate.GetCountOf(t);
            for (int i = 0; i < count; i++)
            {
                PizzaSliceVisual slice = targetPlate.RemoveSliceOfType(t);
                if (slice != null)
                {
                    slice.transform.DOKill();
                    if (ObjectPoolManager.Instance != null)
                    {
                        ObjectPoolManager.Instance.ReturnPizzaSlice(slice);
                    }
                    else
                    {
                        Destroy(slice.gameObject);
                    }
                }
            }
        }

        // 2. Điền đủ 6/6 tinh khiết
        int slicesToFill = 6 - targetPlate.GetTotalSlices();
        Sequence seq = DOTween.Sequence();

        for (int i = 0; i < slicesToFill; i++)
        {
            PizzaSliceVisual newSlice = ObjectPoolManager.Instance.GetPizzaSlice();
            newSlice.SetVisual(targetMajority);
            
            // Spawn từ trên cao rơi xuống đĩa
            newSlice.transform.position = targetPlate.transform.position + Vector3.up * (3f + i * 0.5f);
            targetPlate.TryAddSlice(newSlice, out _);
            newSlice.transform.localScale = Vector3.one; // Khắc phục lỗi sai scale
            
            Tween dropTween = newSlice.transform.DOLocalMove(new Vector3(0, targetPlate.SliceYOffset, 0), 0.3f).SetEase(Ease.OutBack);
            seq.Insert(i * 0.1f, dropTween);
        }

        // 3. Kick cascade để nổ sau khi rớt xong
        seq.OnComplete(() => 
        {
            if (grid != null) grid.TriggerCascade(bestCell);
        });
        
        return true;
    }

    private bool ApplyMove(GridCell source, GridCell dest)
    {
        if (source == null || dest == null) return false;
        
        PizzaPlate sourcePlate = source.CurrentPlate;
        PizzaPlate destPlate = dest.CurrentPlate;

        // Xóa liên kết cũ
        source.ClearPlate();
        dest.ClearPlate();

        // Hoán đổi
        if (destPlate != null)
        {
            source.PlacePlate(destPlate, null);
            // Có thể dùng tween để bay chéo sang nhau
            destPlate.transform.DOMove(source.GetDropPosition(), 0.3f).SetEase(Ease.InOutQuad);
        }

        if (sourcePlate != null)
        {
            dest.PlacePlate(sourcePlate, null);
            sourcePlate.transform.DOMove(dest.GetDropPosition(), 0.3f).SetEase(Ease.InOutQuad);
        }

        // Trigger cascade cho cả 2 ô
        if (GridManager.Instance != null)
        {
            // GridManager.TriggerCascade chỉ xử lý 1 cell và tự đổi State. 
            // Gọi cho 2 ô có thể gây đụng độ priority, nên chọn ô đích để kick.
            GridManager.Instance.TriggerCascade(dest);
        }
        return true;
    }

    private bool ApplyTrash(GridCell target)
    {
        if (target == null || !target.IsOccupied) return false;

        // Hủy đĩa
        PizzaPlate plate = target.CurrentPlate;
        plate.PlayShrinkAndReturn();
        target.ClearPlate();

        // Kích hoạt cascade ở ô vừa xoá (để hàng xóm kiểm tra lại lẫn nhau)
        if (GridManager.Instance != null)
        {
            GridManager.Instance.TriggerCascade(target);
        }
        return true;
    }
}
