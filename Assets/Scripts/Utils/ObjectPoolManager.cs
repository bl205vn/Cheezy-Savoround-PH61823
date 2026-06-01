using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [Header("Pizza Slice Pool")]
    [SerializeField] private PizzaSliceVisual _pizzaSlicePrefab;

    [Header("Pizza Plate Pool")]
    [SerializeField] private PizzaPlate _pizzaPlatePrefab;

    private Queue<PizzaSliceVisual> _slicePool = new Queue<PizzaSliceVisual>();
    private Queue<PizzaPlate> _platePool = new Queue<PizzaPlate>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Không InitializePool ở đây nữa, đợi LevelManager truyền data vào
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void InitializePool(int gridWidth, int gridHeight, int holdSlotCount, int maxSlices)
    {
        // Tính toán linh hoạt theo Data-Driven
        int requiredPlates = (gridWidth * gridHeight) + holdSlotCount + 3; // +3 dự phòng
        int requiredSlices = requiredPlates * maxSlices + 20; // +20 dự phòng cho animation

        Debug.Log($"[ObjectPool] Khởi tạo động: {requiredPlates} đĩa, {requiredSlices} miếng.");

        for (int i = 0; i < requiredSlices; i++)
        {
            PizzaSliceVisual slice = Instantiate(_pizzaSlicePrefab, transform);
            slice.gameObject.SetActive(false);
            _slicePool.Enqueue(slice);
        }

        if (_pizzaPlatePrefab != null)
        {
            for (int i = 0; i < requiredPlates; i++)
            {
                PizzaPlate plate = Instantiate(_pizzaPlatePrefab, transform);
                plate.gameObject.SetActive(false);
                _platePool.Enqueue(plate);
            }
        }
    }

    // Zero GC: Trả về trực tiếp component PizzaSliceVisual thay vì GameObject để tránh GetComponent ở script gọi
    public PizzaSliceVisual GetPizzaSlice()
    {
        if (_slicePool.Count > 0)
        {
            PizzaSliceVisual slice = _slicePool.Dequeue();
            slice.gameObject.SetActive(true);
            return slice;
        }

        // Sinh thêm nếu Pool cạn (Có phát sinh alloc nhưng tránh lỗi dừng game)
        Debug.LogWarning("[ObjectPool] Slice Pool bị cạn! Đang tự động tăng thêm...");
        PizzaSliceVisual newSlice = Instantiate(_pizzaSlicePrefab, transform);
        return newSlice;
    }

    public void ReturnPizzaSlice(PizzaSliceVisual slice)
    {
        slice.gameObject.SetActive(false);
        slice.transform.SetParent(transform); // Đưa về làm con của Pool để hierarchy gọn gàng
        _slicePool.Enqueue(slice);
    }

    public PizzaPlate GetPizzaPlate()
    {
        if (_platePool.Count > 0)
        {
            PizzaPlate plate = _platePool.Dequeue();
            plate.gameObject.SetActive(true);
            return plate;
        }

        if (_pizzaPlatePrefab == null)
        {
            Debug.LogError("[ObjectPoolManager] _pizzaPlatePrefab CHƯA ĐƯỢC GÁN! Vui lòng kéo Prefab đĩa Pizza vào script ObjectPoolManager trên Scene.");
            return null;
        }

        Debug.LogWarning("[ObjectPool] Plate Pool bị cạn! Đang tự động tăng thêm...");
        PizzaPlate newPlate = Instantiate(_pizzaPlatePrefab, transform);
        return newPlate;
    }

    public void ReturnPizzaPlate(PizzaPlate plate)
    {
        plate.gameObject.SetActive(false);
        plate.transform.SetParent(transform);
        _platePool.Enqueue(plate);
    }
}
