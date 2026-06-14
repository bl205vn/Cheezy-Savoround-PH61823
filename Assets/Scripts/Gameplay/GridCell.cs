using UnityEngine;

public class GridCell : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public bool IsOccupied { get; private set; }
    public PizzaPlate CurrentPlate { get; private set; }

    [SerializeField] private float _snapOffsetY = 0f;
    
    private Renderer[] _renderers;
    private MaterialPropertyBlock _propBlock;
    private Color _baseColor;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    public void Initialize(Vector2Int gridPos, Color baseColor)
    {
        GridPosition = gridPos;
        IsOccupied = false;
        CurrentPlate = null;
        _baseColor = baseColor;
        
        // Áp dụng màu base ban đầu (Zero-GC)
        if (_renderers != null && _propBlock != null)
        {
            foreach (var rend in _renderers)
            {
                rend.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_Color", _baseColor);
                _propBlock.SetColor("_BaseColor", _baseColor);
                rend.SetPropertyBlock(_propBlock);
            }
        }
    }

    public Vector3 GetDropPosition()
    {
        return transform.position + new Vector3(0, _snapOffsetY, 0);
    }

    public void PlacePlate(PizzaPlate plate, System.Action onSnapComplete = null)
    {
        IsOccupied = true;
        CurrentPlate = plate;
        
        Vector3 snapPosition = new Vector3(transform.position.x, transform.position.y + _snapOffsetY, transform.position.z);
        
        // Cập nhật data trước để ghi nhận _baseScale đúng với parent mới
        plate.PlaceAt(snapPosition, transform);
        plate.FitToSize(GridManager.Instance.CellSpacing);
        
        // Gọi Coroutine nhảy vào ô và chạy hiệu ứng Squash sau khi xong
        plate.StartCoroutine(plate.AnimateToCell(plate.transform.position, snapPosition, 0.25f, () => 
        {
            plate.PlaySnapEffect(onSnapComplete);
        }));
    }

    public void PlacePlateInstant(PizzaPlate plate)
    {
        IsOccupied = true;
        CurrentPlate = plate;
        Vector3 snapPosition = new Vector3(transform.position.x, transform.position.y + _snapOffsetY, transform.position.z);
        plate.PlaceAt(snapPosition, transform);
        plate.FitToSize(GridManager.Instance.CellSpacing);
        plate.transform.position = snapPosition; // Set position directly without animation
    }

    public void ClearPlate()
    {
        IsOccupied = false;
        CurrentPlate = null;
    }
}
