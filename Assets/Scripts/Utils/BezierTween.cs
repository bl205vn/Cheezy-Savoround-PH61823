using System;
using UnityEngine;

/// <summary>
/// Quản lý hiệu ứng bay đường cong Bezier cho miếng pizza.
/// Sử dụng mảng struct cố định để đảm bảo Zero GC trong quá trình bay.
/// Đặt trên một GameObject trong Scene (Singleton pattern).
/// 
/// Cách dùng:
///   BezierTween.Instance.StartTween(sliceTransform, targetPos);
/// </summary>
public class BezierTween : MonoBehaviour
{
    // ==========================================
    // CẤU HÌNH (Inspector — không hardcode)
    // ==========================================

    [Header("Cấu hình đường bay")]
    [Tooltip("Chiều cao đỉnh cung so với điểm giữa 2 đĩa (đơn vị: Unity unit)")]
    [SerializeField] private float _defaultArcHeight = 2f;

    [Tooltip("Thời gian bay mặc định (giây)")]
    [SerializeField] private float _defaultDuration = 0.5f;

    [Tooltip("Số lượng tween tối đa chạy đồng thời (tránh resize mảng)")]
    [SerializeField] private int _maxConcurrentTweens = 20;

    // ==========================================
    // DỮ LIỆU TWEEN (Struct = Stack, không GC)
    // ==========================================

    /// <summary>
    /// Struct lưu trữ dữ liệu mỗi tween đang hoạt động.
    /// Nằm trong mảng cố định trên heap (nhưng bản thân struct không gây GC khi truy xuất).
    /// </summary>
    private struct TweenData
    {
        public bool IsActive;
        public Transform Target;
        public Vector3 StartPos;
        public Vector3 ControlPos; // Điểm điều khiển Bezier (đỉnh arc)
        public Vector3 EndPos;
        public float Duration;
        public float ElapsedTime;
        public Action<Transform> OnComplete; // Callback khi bay xong (nullable)
    }

    // Mảng cố định — cấp phát 1 lần duy nhất trong Awake(), không bao giờ resize
    private TweenData[] _activeTweens;
    private int _activeTweenCount;

    // ==========================================
    // SINGLETON
    // ==========================================

    public static BezierTween Instance { get; private set; }

    // ==========================================
    // EVENTS (Observer Pattern — cho FSM lắng nghe)
    // ==========================================

    /// <summary>
    /// Phát khi TẤT CẢ tween đang chạy hoàn thành.
    /// AnimatingState lắng nghe event này để biết khi nào chuyển về PlayingState.
    /// </summary>
    public static event Action OnAllTweensCompleted;

    // ==========================================
    // LIFECYCLE
    // ==========================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Cấp phát mảng 1 lần duy nhất — Zero GC về sau
        _activeTweens = new TweenData[_maxConcurrentTweens];
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ==========================================
    // TOÁN HỌC BEZIER (Pure Static — Zero Alloc)
    // ==========================================

    /// <summary>
    /// Tính điểm trên đường cong Bezier bậc 2 (Quadratic).
    /// B(t) = (1-t)²·P0 + 2·(1-t)·t·P1 + t²·P2
    /// 
    /// P0 = điểm bắt đầu (vị trí miếng bánh trên đĩa nguồn)
    /// P1 = điểm điều khiển (đỉnh arc — nâng cao tạo vòng cung)
    /// P2 = điểm kết thúc (vị trí đích trên đĩa nhận)
    /// t  ∈ [0, 1] — tiến trình bay
    /// </summary>
    public static Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    /// <summary>
    /// Tính điểm trên đường cong Bezier bậc 3 (Cubic) — dự phòng cho hiệu ứng phức tạp hơn.
    /// B(t) = (1-t)³·P0 + 3·(1-t)²·t·P1 + 3·(1-t)·t²·P2 + t³·P3
    /// </summary>
    public static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        float uu = u * u;
        float tt = t * t;
        return uu * u * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + tt * t * p3;
    }

    // ==========================================
    // EASING (Tăng tốc/giảm tốc mượt mà)
    // ==========================================

    /// <summary>
    /// Ease In-Out Quadratic: Bắt đầu chậm → tăng tốc → giảm tốc → dừng mượt.
    /// Tạo cảm giác "nặng" khi cất cánh và "nhẹ" khi hạ cánh — rất tự nhiên cho pizza bay.
    /// </summary>
    public static float EaseInOutQuad(float t)
    {
        return t < 0.5f
            ? 2f * t * t
            : 1f - (-2f * t + 2f) * (-2f * t + 2f) / 2f;
    }

    // ==========================================
    // PUBLIC API
    // ==========================================

    /// <summary>
    /// Bắt đầu tween: miếng pizza bay từ vị trí hiện tại đến đích theo đường cong Bezier.
    /// 
    /// Luồng gọi điển hình (Task 3.1 sẽ dùng):
    ///   1. GridManager phát hiện miếng bánh cùng loại ở đĩa lân cận
    ///   2. Gỡ miếng bánh khỏi mảng _slices của đĩa nguồn
    ///   3. Gọi BezierTween.Instance.StartTween(slice.transform, targetPos, onComplete: ...)
    ///   4. Trong callback onComplete: gắn miếng bánh vào mảng _slices của đĩa đích
    /// </summary>
    /// <param name="target">Transform cần di chuyển (miếng pizza)</param>
    /// <param name="endPos">Vị trí đích world-space (tâm đĩa nhận)</param>
    /// <param name="arcHeight">Chiều cao đỉnh cung (< 0 = dùng mặc định)</param>
    /// <param name="duration">Thời gian bay giây (< 0 = dùng mặc định)</param>
    /// <param name="onComplete">Callback khi bay xong (nullable, GC chỉ phát sinh 1 lần khi tạo delegate)</param>
    /// <returns>true nếu tween được khởi tạo thành công, false nếu hết slot</returns>
    public bool StartTween(Transform target, Vector3 endPos,
        float arcHeight = -1f, float duration = -1f, Action<Transform> onComplete = null)
    {
        if (arcHeight < 0f) arcHeight = _defaultArcHeight;
        if (duration < 0f) duration = _defaultDuration;

        // Tìm slot trống trong mảng cố định
        for (int i = 0; i < _activeTweens.Length; i++)
        {
            if (!_activeTweens[i].IsActive)
            {
                Vector3 startPos = target.position;

                // Tính điểm điều khiển: trung điểm giữa Start và End, nâng lên theo arcHeight
                Vector3 controlPos;
                controlPos.x = (startPos.x + endPos.x) * 0.5f;
                controlPos.y = Mathf.Max(startPos.y, endPos.y) + arcHeight; // Nâng từ điểm CAO hơn
                controlPos.z = (startPos.z + endPos.z) * 0.5f;

                _activeTweens[i].IsActive = true;
                _activeTweens[i].Target = target;
                _activeTweens[i].StartPos = startPos;
                _activeTweens[i].ControlPos = controlPos;
                _activeTweens[i].EndPos = endPos;
                _activeTweens[i].Duration = duration;
                _activeTweens[i].ElapsedTime = 0f;
                _activeTweens[i].OnComplete = onComplete;

                _activeTweenCount++;
                return true;
            }
        }

        Debug.LogWarning($"[BezierTween] Hết slot tween! Tối đa {_maxConcurrentTweens} tween đồng thời.");
        return false;
    }

    /// <summary>
    /// Kiểm tra xem còn tween nào đang chạy hay không.
    /// Hữu ích cho FSM: AnimatingState kiểm tra property này mỗi frame
    /// để biết khi nào chuyển về PlayingState.
    /// </summary>
    public bool HasActiveTweens => _activeTweenCount > 0;

    /// <summary>
    /// Hủy tất cả tween đang chạy (dùng khi reset màn hoặc Game Over).
    /// Snap mọi target về vị trí đích ngay lập tức, KHÔNG gọi callback.
    /// </summary>
    public void CancelAllTweens()
    {
        for (int i = 0; i < _activeTweens.Length; i++)
        {
            if (_activeTweens[i].IsActive)
            {
                // Snap về đích để tránh trạng thái lơ lửng
                if (_activeTweens[i].Target != null)
                {
                    _activeTweens[i].Target.position = _activeTweens[i].EndPos;
                }

                _activeTweens[i].IsActive = false;
                _activeTweens[i].Target = null;
                _activeTweens[i].OnComplete = null;
            }
        }
        _activeTweenCount = 0;
    }

    // ==========================================
    // UPDATE LOOP — ZERO GC TRONG QUÁ TRÌNH BAY
    // ==========================================
    // Không có: new, string concat, lambda, boxing, List resize
    // Chỉ có: phép toán số học + gán struct field qua array indexer

    private void Update()
    {
        // Early return: không chạy vòng lặp nếu không có tween nào
        if (_activeTweenCount == 0) return;

        float deltaTime = Time.deltaTime;

        for (int i = 0; i < _activeTweens.Length; i++)
        {
            if (!_activeTweens[i].IsActive) continue;

            // Safety: Target có thể bị hủy bất ngờ (ví dụ reset scene)
            if (_activeTweens[i].Target == null)
            {
                _activeTweens[i].IsActive = false;
                _activeTweens[i].OnComplete = null;
                _activeTweenCount--;
                continue;
            }

            _activeTweens[i].ElapsedTime += deltaTime;

            // Tính tiến trình thô (0 → 1)
            float rawT = _activeTweens[i].ElapsedTime / _activeTweens[i].Duration;

            if (rawT >= 1f)
            {
                // ===== HOÀN THÀNH: Snap chính xác vào đích =====
                _activeTweens[i].Target.position = _activeTweens[i].EndPos;

                // Cache callback trước khi xóa slot (tránh mất ref)
                Action<Transform> callback = _activeTweens[i].OnComplete;
                Transform target = _activeTweens[i].Target;

                // Giải phóng slot
                _activeTweens[i].IsActive = false;
                _activeTweens[i].Target = null;
                _activeTweens[i].OnComplete = null;
                _activeTweenCount--;

                // Gọi callback SAU KHI giải phóng slot
                // (callback có thể gọi StartTween mới → cần slot trống)
                callback?.Invoke(target);
            }
            else
            {
                // ===== ĐANG BAY: Nội suy vị trí trên đường cong Bezier =====
                float easedT = EaseInOutQuad(rawT);
                _activeTweens[i].Target.position = QuadraticBezier(
                    _activeTweens[i].StartPos,
                    _activeTweens[i].ControlPos,
                    _activeTweens[i].EndPos,
                    easedT
                );
            }
        }

        // Phát event khi tất cả tween vừa hoàn thành
        // (AnimatingState lắng nghe để chuyển về PlayingState hoặc CheckingComboState)
        if (_activeTweenCount == 0)
        {
            OnAllTweensCompleted?.Invoke();
        }
    }
}
