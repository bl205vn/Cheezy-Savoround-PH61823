using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Settings")]
    [Tooltip("Tiếng nổ đĩa (VD: cinematic-boom.mp3)")]
    [SerializeField] private AudioClip _explosionClip;
    [Tooltip("Tiếng đặt đĩa thành công (VD: placing-small-ceramic)")]
    [SerializeField] private AudioClip _placeClip;
    [Tooltip("Tiếng báo lỗi đặt sai ô (VD: wrong)")]
    [SerializeField] private AudioClip _errorClip;
    [SerializeField] private float _baseVolume = 1f;

    [Header("Pitch Shift (Hiệu ứng Combo)")]
    [SerializeField] private float _basePitch = 1f;
    [SerializeField] private float _pitchIncrement = 0.15f; // Tăng cao độ mỗi lần nổ liên tiếp
    [SerializeField] private float _maxPitch = 2f;
    [SerializeField] private float _comboResetDelay = 1.5f; // Nếu 1.5s không có tiếng nổ thì reset cao độ

    private AudioSource _sfxSource;
    private AudioSource _pitchSource; // Source riêng để đổi Pitch không ảnh hưởng SFX khác
    private float _lastExplosionTime;
    private int _comboCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        _sfxSource = GetComponent<AudioSource>();
        _pitchSource = gameObject.AddComponent<AudioSource>();
        _pitchSource.playOnAwake = false;
    }

    public void PlayExplosionSound()
    {
        if (_explosionClip == null || _pitchSource == null) return;

        // Nếu khoảng thời gian giữa 2 lần nổ quá lâu -> Reset combo
        if (Time.time - _lastExplosionTime > _comboResetDelay)
        {
            _comboCount = 0;
        }

        _comboCount++;
        _lastExplosionTime = Time.time;

        // Tính toán Pitch: Càng combo nhiều âm thanh càng "tít" lên cao
        float currentPitch = Mathf.Min(_basePitch + (_comboCount - 1) * _pitchIncrement, _maxPitch);
        _pitchSource.pitch = currentPitch;

        // Phát âm thanh trên Source riêng
        _pitchSource.PlayOneShot(_explosionClip, _baseVolume);
    }

    public void PlayPlaceSound()
    {
        // Yêu cầu: Khi người chơi đặt đĩa mới, âm thanh combo phải được đặt về ban đầu
        _comboCount = 0;

        if (_placeClip != null && _sfxSource != null)
        {
            _sfxSource.PlayOneShot(_placeClip, _baseVolume);
        }
    }

    public void PlayErrorSound()
    {
        if (_errorClip != null && _sfxSource != null)
        {
            _sfxSource.PlayOneShot(_errorClip, _baseVolume);
        }
    }
}
