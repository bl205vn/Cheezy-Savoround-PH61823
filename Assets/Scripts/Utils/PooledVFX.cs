using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PooledVFX : MonoBehaviour
{
    [Tooltip("Hệ số Scale tự động so với vật thể bị nổ")]
    [SerializeField] private float _autoScaleMultiplier;

    private ParticleSystem _particleSystem;

    private void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        // Khi đối tượng được bật lên (lấy từ Pool), bắt đầu Coroutine để tự động cất đi
        StartCoroutine(ReturnToPoolCoroutine());
    }

    // Hàm public này giúp tự động scale VFX theo kích thước của vật thể gốc (Zero Hardcoding)
    public void PlayAt(Vector3 position, Vector3 referenceScale)
    {
        transform.position = position;
        
        // Nhân kích thước của cái đĩa với hệ số thu nhỏ có thể tinh chỉnh trên Inspector
        transform.localScale = referenceScale * _autoScaleMultiplier;
        
        if (_particleSystem != null)
        {
            _particleSystem.Play(true);
        }
    }

    private IEnumerator ReturnToPoolCoroutine()
    {
        // Đợi cho đến khi Particle System phát xong tất cả các hạt
        yield return new WaitUntil(() => _particleSystem != null && !_particleSystem.IsAlive(true));
        
        // Trả về Pool thay vì Destroy (Zero GC)
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnVFX(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
