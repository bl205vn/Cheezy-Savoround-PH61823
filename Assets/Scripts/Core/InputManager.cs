using UnityEngine;
using System;

public class InputManager : MonoBehaviour
{
    private Camera _mainCamera;
    private PizzaPlate _draggedPlate;
    private Plane _dragPlane;
    [SerializeField] private float _dragHeight = 1.0f; // Độ cao của đĩa khi kéo
    [SerializeField] private LayerMask _gridLayerMask = Physics.DefaultRaycastLayers;

    private const float DEBUG_RAY_LENGTH = 5f;
    private const float DEBUG_RAY_DURATION = 2f;

    private readonly RaycastHit[] _hitBuffer = new RaycastHit[10];

    private struct HitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
    {
        public int Compare(RaycastHit x, RaycastHit y)
        {
            return x.distance.CompareTo(y.distance);
        }
    }
    private readonly HitDistanceComparer _hitComparer = new HitDistanceComparer();

    public static event Action<PizzaPlate, GridCell> OnPlatePlaced;

    private void Awake()
    {
        _mainCamera = Camera.main;
        // Mặt phẳng dùng để kéo thả (nằm ngang y = dragHeight)
        _dragPlane = new Plane(Vector3.up, new Vector3(0, _dragHeight, 0));
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // FSM Lock: Chỉ cho phép tương tác khi đang ở trạng thái PlayingState
        if (GameStateManager.Instance != null && 
            GameStateManager.Instance.CurrentState != GameStateManager.Instance.Playing)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryPickUpPlate();
        }
        else if (Input.GetMouseButton(0) && _draggedPlate != null)
        {
            DragPlate();
        }
        else if (Input.GetMouseButtonUp(0) && _draggedPlate != null)
        {
            TryDropPlate();
        }
    }

    private void TryPickUpPlate()
    {
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.TryGetComponent(out PizzaPlate plate))
            {
                _draggedPlate = plate;
                _draggedPlate.PickUp();
            }
        }
    }

    private void DragPlate()
    {
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        if (_dragPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPos = ray.GetPoint(distance);
            _draggedPlate.DragTo(worldPos);

            // Vẽ gizmo (Debug Ray) màu cam đậm hướng xuống dưới khi đang kéo
            Debug.DrawRay(_draggedPlate.transform.position, Vector3.down * DEBUG_RAY_LENGTH, new Color(1.0f, 0.5f, 0.0f));
        }
    }

    private void TryDropPlate()
    {
        // Bắn tia từ đĩa xuống dưới để tìm lưới
        Ray ray = new Ray(_draggedPlate.transform.position, Vector3.down);
        
        // Vẽ gizmo (Debug Ray) màu cam đậm lưu lại 2 giây để dễ quan sát khi nhả chuột
        Debug.DrawRay(ray.origin, ray.direction * DEBUG_RAY_LENGTH, new Color(1.0f, 0.5f, 0.0f), DEBUG_RAY_DURATION);

        // Dùng RaycastNonAlloc để tránh GC Alloc mỗi lần thả đĩa
        int hitCount = Physics.RaycastNonAlloc(ray, _hitBuffer, 100f, _gridLayerMask);
        Array.Sort(_hitBuffer, 0, hitCount, _hitComparer);

        for (int i = 0; i < hitCount; i++)
        {
            var hit = _hitBuffer[i];
            if (hit.collider.TryGetComponent(out GridCell cell) && !cell.IsOccupied)
            {
                // Snap vào ô lưới
                cell.PlacePlate(_draggedPlate);
                OnPlatePlaced?.Invoke(_draggedPlate, cell);
                _draggedPlate = null;
                return; // Thành công
            }
        }

        // Không tìm thấy ô hoặc ô đã có đĩa -> trả về chỗ cũ
        _draggedPlate.ReturnToOriginalSlot();
        _draggedPlate = null;
    }
}
