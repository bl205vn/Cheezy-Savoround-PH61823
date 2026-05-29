using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GameObject _cellPrefabLight; // Prefab ô màu sáng (chẵn)
    [SerializeField] private GameObject _cellPrefabDark;  // Prefab ô màu tối (lẻ)
    [SerializeField] private float _cellSpacing = 1.0f; // Khoảng cách giữa các ô

    // Dictionary lưu trạng thái các ô
    private Dictionary<Vector2Int, GridCell> _gridCells = new Dictionary<Vector2Int, GridCell>();

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
                
                // Thuật toán Checkerboard (Bàn cờ caro)
                bool isEven = (x + y) % 2 == 0;
                GameObject prefabToUse = isEven ? _cellPrefabLight : _cellPrefabDark;
                
                GameObject cellObj = Instantiate(prefabToUse, worldPos, Quaternion.identity, transform);
                cellObj.name = $"Cell_{x}_{y}";
                
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
        CheckAdjacentCells(cell.GridPosition);
    }

    private void CheckAdjacentCells(Vector2Int centerPos)
    {
        GridCell centerCell = GetCell(centerPos);
        if (centerCell == null || !centerCell.IsOccupied) return;

        PizzaPlate centerPlate = centerCell.CurrentPlate;
        _matchingCells.Clear();

        foreach (var dir in _directions)
        {
            GridCell neighbor = GetCell(centerPos + dir);
            if (neighbor != null && neighbor.IsOccupied)
            {
                // Kiểm tra cùng loại đĩa (Mock cho Tuần 1 vì chưa có miếng pizza)
                if (neighbor.CurrentPlate.Type == centerPlate.Type)
                {
                    _matchingCells.Add(neighbor);
                }
            }
        }

        // Log kết quả
        if (_matchingCells.Count > 0)
        {
            string log = $"[Thuật toán quét] Đĩa {centerPlate.Type} tại ô {centerPos} trùng khớp với các ô:";
            foreach (var match in _matchingCells)
            {
                log += $" {match.GridPosition}";
            }
            Debug.Log(log);
        }
        else
        {
            Debug.Log($"[Thuật toán quét] Đĩa tại {centerPos} không có ô lân cận nào cùng loại.");
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
                Vector3 size = new Vector3(_cellSpacing, 0f, _cellSpacing) * 0.95f; 
                Gizmos.DrawWireCube(worldPos, size);
            }
        }
    }
#endif
}
