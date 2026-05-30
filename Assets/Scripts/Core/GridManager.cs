using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GameObject _cellPrefab;
    [SerializeField] private float _cellSpacing = 1.0f; // Khoảng cách giữa các ô
    
    [Header("Visual")]
    [SerializeField] private Color _lightCellColor = new Color(0.9f, 0.85f, 0.7f); // Màu sáng (lẻ)
    [SerializeField] private Color _darkCellColor = new Color(0.55f, 0.5f, 0.35f);  // Màu tối hơn (chẵn)

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
                // Kiểm tra xem 2 đĩa có bất kỳ loại bánh nào chung không (nền tảng cho logic Merge)
                bool hasCommonType = false;
                if (centerPlate.Slices != null && neighbor.CurrentPlate.Slices != null)
                {
                    foreach (var cSlice in centerPlate.Slices)
                    {
                        if (cSlice == null) continue;
                        foreach (var nSlice in neighbor.CurrentPlate.Slices)
                        {
                            if (nSlice != null && cSlice.TypeIndex == nSlice.TypeIndex)
                            {
                                hasCommonType = true;
                                break;
                            }
                        }
                        if (hasCommonType) break;
                    }
                }

                if (hasCommonType)
                {
                    _matchingCells.Add(neighbor);
                }
            }
        }

        // Log kết quả
        if (_matchingCells.Count > 0)
        {
            string log = $"[Thuật toán quét] Đĩa tại ô {centerPos} có miếng bánh CÙNG LOẠI với các ô:";
            foreach (var match in _matchingCells)
            {
                log += $" {match.GridPosition}";
            }
            Debug.Log(log);
        }
        else
        {
            Debug.Log($"[Thuật toán quét] Đĩa tại {centerPos} KHÔNG có ô lân cận nào chứa miếng bánh cùng loại.");
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
