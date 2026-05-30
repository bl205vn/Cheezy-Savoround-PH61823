using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [Header("Pizza Slice Pool")]
    [SerializeField] private PizzaSliceVisual _pizzaSlicePrefab;
    [Tooltip("Size: 144 (Lưới 4x6 đầy) + 18 (Khay) + 38 (Animation dự phòng) = 200")]
    [SerializeField] private int _initialSlicePoolSize = 200;

    private Queue<PizzaSliceVisual> _slicePool = new Queue<PizzaSliceVisual>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePool()
    {
        for (int i = 0; i < _initialSlicePoolSize; i++)
        {
            PizzaSliceVisual slice = Instantiate(_pizzaSlicePrefab, transform);
            slice.gameObject.SetActive(false);
            _slicePool.Enqueue(slice);
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
}
