using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    [Header("Pizza Slice Pool")]
    [SerializeField] private PizzaSliceVisual _pizzaSlicePrefab;

    [Header("Pizza Plate Pool")]
    [SerializeField] private PizzaPlate _pizzaPlatePrefab;

    [Header("VFX Pool")]
    [SerializeField] private PooledVFX _explosionVfxPrefab;
    [SerializeField] private int _initialVfxPoolSize = 15;

    [Header("UI Pool")]
    [SerializeField] private FloatingText _floatingTextPrefab;
    [SerializeField] private int _initialTextPoolSize = 15;

    private Queue<PizzaSliceVisual> _slicePool = new Queue<PizzaSliceVisual>();
    private Queue<PizzaPlate> _platePool = new Queue<PizzaPlate>();
    private Queue<PooledVFX> _vfxPool = new Queue<PooledVFX>();
    private Queue<FloatingText> _floatingTextPool = new Queue<FloatingText>();

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
        
        if (_explosionVfxPrefab != null)
        {
            for (int i = 0; i < _initialVfxPoolSize; i++)
            {
                PooledVFX vfx = Instantiate(_explosionVfxPrefab, transform);
                vfx.gameObject.SetActive(false);
                _vfxPool.Enqueue(vfx);
            }
        }

        for (int i = 0; i < _initialTextPoolSize; i++)
        {
            if (_floatingTextPrefab != null)
            {
                FloatingText txt = Instantiate(_floatingTextPrefab, transform);
                txt.gameObject.SetActive(false);
                _floatingTextPool.Enqueue(txt);
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

    public PooledVFX GetExplosionVFX()
    {
        if (_explosionVfxPrefab == null) return null;

        if (_vfxPool.Count > 0)
        {
            PooledVFX vfx = _vfxPool.Dequeue();
            vfx.gameObject.SetActive(true);
            return vfx;
        }

        Debug.LogWarning("[ObjectPool] VFX Pool bị cạn! Đang tự động tăng thêm...");
        PooledVFX newVfx = Instantiate(_explosionVfxPrefab, transform);
        return newVfx;
    }

    public void ReturnVFX(PooledVFX vfx)
    {
        vfx.gameObject.SetActive(false);
        vfx.transform.SetParent(transform);
        _vfxPool.Enqueue(vfx);
    }

    public FloatingText GetFloatingText()
    {
        if (_floatingTextPrefab == null) return null;

        if (_floatingTextPool.Count > 0)
        {
            FloatingText txt = _floatingTextPool.Dequeue();
            txt.gameObject.SetActive(true);
            return txt;
        }

        return Instantiate(_floatingTextPrefab, transform);
    }

    public void ReturnFloatingText(FloatingText txt)
    {
        txt.gameObject.SetActive(false);
        txt.transform.SetParent(transform);
        _floatingTextPool.Enqueue(txt);
    }
}
