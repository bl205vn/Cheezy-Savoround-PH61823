using UnityEngine;

public class GridCell : MonoBehaviour
{
    public Vector2Int GridPosition { get; private set; }
    public bool IsOccupied { get; private set; }
    public PizzaPlate CurrentPlate { get; private set; }

    [SerializeField] private float _snapOffsetY = 0.2f;

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
        
        // Đặt vị trí của đĩa vào đúng tâm ô lưới (cùng toạ độ x, z và nhô lên trục y một chút)
        Vector3 snapPosition = new Vector3(transform.position.x, transform.position.y + _snapOffsetY, transform.position.z);
        plate.PlaceAt(snapPosition, transform);
    }
}
