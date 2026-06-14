using UnityEngine;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(TextMeshPro))]
public class FloatingText : MonoBehaviour
{
    private TextMeshPro _textMesh;

    private void Awake()
    {
        _textMesh = GetComponent<TextMeshPro>();
    }

    public void Setup(string text, Vector3 startPos)
    {
        if (_textMesh == null) _textMesh = GetComponent<TextMeshPro>();

        // Dọn dẹp các Tween cũ đang dở dang (cực kỳ quan trọng khi xài Object Pool)
        transform.DOKill();
        _textMesh.DOKill();

        transform.position = startPos;
        
        // Ép chữ luôn xoay mặt về Camera
        Vector3 moveDirection = Vector3.forward; // Mặc định bay theo trục Z nếu có lỗi
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
            // Vector "hướng lên trên" của màn hình (chính là chiều dọc của Camera)
            moveDirection = Camera.main.transform.up; 
        }

        _textMesh.SetText(text);
        // KHÔNG set color ở đây để Designer tự chỉnh màu/Gradient trong Inspector.
        // Chỉ reset Alpha về 1 vì DOTween sẽ làm nó mờ đi về 0 ở cuối chu kỳ.
        _textMesh.alpha = 1f;

        // --- DOTWEEN ANIMATION ---
        // 1. Trượt lên trên màn hình (theo trục Z ở TopDown) một khoảng 1.5 unit
        transform.DOMove(startPos + moveDirection * 1.5f, 0.8f).SetEase(Ease.OutQuad);
        
        // 2. Mờ dần về 0 trong 0.8 giây, sau khi xong thì trả về Pool
        _textMesh.DOFade(0f, 0.8f).SetEase(Ease.InQuad).OnComplete(() =>
        {
            if (ObjectPoolManager.Instance != null)
                ObjectPoolManager.Instance.ReturnFloatingText(this);
            else
                gameObject.SetActive(false);
        });
    }

    // Zero-GC Overload cho điểm số (VD: "+100")
    public void Setup(string format, int value, Vector3 startPos)
    {
        if (_textMesh == null) _textMesh = GetComponent<TextMeshPro>();
        transform.DOKill();
        _textMesh.DOKill();
        transform.position = startPos;
        
        Vector3 moveDirection = Vector3.forward;
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
            moveDirection = Camera.main.transform.up; 
        }

        _textMesh.SetText(format, value);
        _textMesh.alpha = 1f;

        transform.DOMove(startPos + moveDirection * 1.5f, 0.8f).SetEase(Ease.OutQuad);
        _textMesh.DOFade(0f, 0.8f).SetEase(Ease.InQuad).OnComplete(() =>
        {
            if (ObjectPoolManager.Instance != null)
                ObjectPoolManager.Instance.ReturnFloatingText(this);
            else
                gameObject.SetActive(false);
        });
    }
}
