using UnityEngine;

public class GridCell : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public bool IsOccupied { get; private set; }
    public PizzaPlate CurrentPlate { get; private set; }

    [SerializeField] private float _snapOffsetY = 0f;

    public void Initialize(Vector2Int gridPos)
    {
        GridPosition = gridPos;
        IsOccupied = false;
        CurrentPlate = null;
    }

    public void PlacePlate(PizzaPlate plate)
    {
        IsOccupied = true;
        CurrentPlate = plate;
        
        Vector3 snapPosition = new Vector3(transform.position.x, transform.position.y + _snapOffsetY, transform.position.z);
        
        // Cập nhật data trước để ghi nhận _baseScale đúng với parent mới
        plate.PlaceAt(snapPosition, transform);
        
        // Gọi Coroutine nhảy vào ô và chạy hiệu ứng Squash sau khi xong
        plate.StartCoroutine(plate.AnimateToCell(plate.transform.position, snapPosition, 0.25f, () => 
        {
            plate.PlaySnapEffect();
        }));
    }

    public void ClearPlate()
    {
        IsOccupied = false;
        CurrentPlate = null;
    }
}
