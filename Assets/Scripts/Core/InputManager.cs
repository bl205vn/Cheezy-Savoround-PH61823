using UnityEngine;
using System;

public class InputManager : MonoBehaviour
{
    private Camera _mainCamera;
    private PizzaPlate _draggedPlate;
    private GridCell _lastHighlightedCell;
    private Plane _dragPlane;
    [SerializeField] private float _dragHeight = 1.0f; // Độ cao của đĩa khi kéo
    [SerializeField] private LayerMask _gridLayerMask = Physics.DefaultRaycastLayers;
    [Header("Ghost Preview")]
    [SerializeField] private GhostPreview _ghostPreview;

    private const float DEBUG_RAY_LENGTH = 5f;
    private const float DEBUG_RAY_DURATION = 2f;

    private readonly RaycastHit[] _hitBuffer = new RaycastHit[10];

    private class HitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
    {
        public int Compare(RaycastHit x, RaycastHit y)
        {
            return x.distance.CompareTo(y.distance);
        }
    }
    private readonly HitDistanceComparer _hitComparer = new HitDistanceComparer();

    // Event giờ đã được quản lý tập trung ở GameEvents.cs

    private void Awake()
    {
        _mainCamera = Camera.main;
        // Mặt phẳng dùng để kéo thả (nằm ngang y = dragHeight)
        _dragPlane = new Plane(Vector3.up, new Vector3(0, _dragHeight, 0));

        // Tự động Instantiate Ghost Preview nếu người dùng kéo Prefab từ Project vào
        if (_ghostPreview != null)
        {
            if (_ghostPreview.gameObject.scene != this.gameObject.scene)
            {
                _ghostPreview = Instantiate(_ghostPreview);
            }
            _ghostPreview.Hide();
        }
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

        // Ưu tiên BoosterManager nếu đang chờ target (di chuyển/xóa đĩa)
        if (BoosterManager.Instance != null && BoosterManager.Instance.IsWaitingForTarget)
        {
            if (BoosterManager.Instance.IsMoveBoosterActive)
            {
                // Cho phép kéo thả bình thường (bỏ qua return để chạy TryPickUpPlate ở dưới)
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (UnityEngine.EventSystems.EventSystem.current != null)
                    {
                        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject() ||
                            (Input.touchCount > 0 && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)))
                        {
                            return; // Bỏ qua nếu chạm vào UI
                        }
                    }

                    Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        BoosterManager.Instance.HandleTap(hit);
                    }
                }
                return; // Khóa input thường đối với các Booster khác (như Trash)
            }
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
        // Chặn tương tác nếu người chơi đang bấm vào UI (cả chuột và touch trên mobile)
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject() ||
                (Input.touchCount > 0 && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)))
            {
                return;
            }
        }

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.TryGetComponent(out PizzaPlate plate))
            {
                // Nếu đang dùng Move Booster, cho phép bốc đĩa từ Grid
                bool isMoveBooster = BoosterManager.Instance != null && BoosterManager.Instance.IsMoveBoosterActive;
                GridCell plateCell = GridManager.Instance != null ? GridManager.Instance.GetCellOfPlate(plate) : null;
                
                bool fromTray = TrayManager.Instance != null && TrayManager.Instance.IsPlateInTray(plate);
                bool fromGridAndBoosted = isMoveBooster && plateCell != null;

                if ((fromTray || fromGridAndBoosted) && !plate.IsReturning)
                {
                    _draggedPlate = plate;
                    _draggedPlate.PickUp();
                    
                    if (fromGridAndBoosted)
                    {
                        plateCell.ClearPlate(); // Nhấc khỏi lưới
                    }
                }
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
            
            // Xử lý Highlight ô lưới bên dưới
            Ray downwardRay = new Ray(_draggedPlate.transform.position, Vector3.down);
            int hitCount = Physics.RaycastNonAlloc(downwardRay, _hitBuffer, 100f, _gridLayerMask);
            Array.Sort(_hitBuffer, 0, hitCount, _hitComparer);

            GridCell currentTargetCell = null;
            for (int i = 0; i < hitCount; i++)
            {
                var hit = _hitBuffer[i];
                if (hit.collider.TryGetComponent(out GridCell cell) && !cell.IsOccupied)
                {
                    currentTargetCell = cell;
                    break;
                }
            }

            // Xử lý hiển thị bóng mờ (Ghost Plate) snap vào tâm ô lưới
            if (currentTargetCell != null)
            {
                if (_ghostPreview != null)
                {
                    // Scale bóng mờ theo world scale của đĩa (kể cả khi đĩa nằm trên GridCell bị scale)
                    Vector3 ghostScale = _draggedPlate.BaseScale;
                    if (_draggedPlate.OriginalParent != null)
                    {
                        Vector3 parentScale = _draggedPlate.OriginalParent.lossyScale;
                        ghostScale = new Vector3(
                            _draggedPlate.BaseScale.x * parentScale.x,
                            _draggedPlate.BaseScale.y * parentScale.y,
                            _draggedPlate.BaseScale.z * parentScale.z
                        );
                    }
                    _ghostPreview.transform.localScale = ghostScale;
                    _ghostPreview.ShowAt(currentTargetCell.GetDropPosition());
                }
            }
            else
            {
                if (_ghostPreview != null)
                {
                    _ghostPreview.Hide();
                }
            }

            _lastHighlightedCell = currentTargetCell;
        }
    }

    private void TryDropPlate()
    {
        // Ẩn bóng mờ khi thả đĩa
        if (_ghostPreview != null)
        {
            _ghostPreview.Hide();
        }

        _lastHighlightedCell = null;

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
                // Thay đổi trạng thái FSM ngay lập tức sang Animating để block input người chơi
                if (GameStateManager.Instance != null)
                {
                    GameStateManager.Instance.ChangeState(GameStateManager.Instance.Animating);
                }

                PizzaPlate plateToPlace = _draggedPlate;
                _draggedPlate = null;

                if (BoosterManager.Instance != null && BoosterManager.Instance.IsMoveBoosterActive)
                {
                    BoosterManager.Instance.CompleteMoveBooster();
                }

                // Snap vào ô lưới và chờ animation xong mới bắn event
                cell.PlacePlate(plateToPlace, () => {
                    GameEvents.TriggerPlatePlaced(plateToPlace, cell);
                });
                
                return; // Thành công
            }
        }

        // Không tìm thấy ô hoặc ô đã có đĩa -> trả về chỗ cũ
        GameEvents.TriggerPlatePlaceFailed(_draggedPlate);
        
        if (BoosterManager.Instance != null && BoosterManager.Instance.IsMoveBoosterActive)
        {
            if (_draggedPlate.OriginalParent != null)
            {
                GridCell originalCell = _draggedPlate.OriginalParent.GetComponent<GridCell>();
                if (originalCell != null)
                {
                    originalCell.RestorePlateLogical(_draggedPlate);
                }
            }
        }
        
        _draggedPlate.PlayShakeAndReturn();
        _draggedPlate = null;
    }
}
