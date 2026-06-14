using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelProgressUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _fillImage;           // Kéo Image 'Lapday' vào đây
    [SerializeField] private TMP_Text _currentLevelText; // Kéo Text 'LevelBandau' vào đây
    [SerializeField] private TMP_Text _nextLevelText;    // Kéo Text 'LevelKe' vào đây

    [Header("Level Settings")]
    [SerializeField] private int _scoreToNextLevel = 1000; // Số điểm cần thiết để lên level
    
    private int _currentScore = 0;

    public int CurrentScore => _currentScore;
    
    private void Start()
    {
        // Kiểm tra và khởi tạo cấp độ mặc định (fix lỗi file JSON cũ)
        if (SaveLoadManager.Data != null && SaveLoadManager.Data.CurrentLevel <= 0)
        {
            SaveLoadManager.Data.CurrentLevel = 1;
            SaveLoadManager.Save();
        }
        
        // Khôi phục score nếu có save
        if (SaveLoadManager.Data != null && SaveLoadManager.Data.CurrentLevelProgress != null && SaveLoadManager.Data.CurrentLevelProgress.levelId == SaveLoadManager.Data.CurrentLevel)
        {
            _currentScore = SaveLoadManager.Data.CurrentLevelProgress.currentScore;
        }

        InitializeUI();
    }

    private bool _pendingLevelUp = false;

    private void OnEnable()
    {
        // Lắng nghe sự kiện cộng điểm từ GameEvents (Event-driven, Zero-GC)
        GameEvents.OnPlateExploded += HandlePlateExploded;
        GameStateManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnPlateExploded -= HandlePlateExploded;
        GameStateManager.OnStateChanged -= HandleStateChanged;
    }

    private void InitializeUI()
    {
        UpdateLevelTexts();
        UpdateFillImage();
    }

    private void HandlePlateExploded(int pizzaType, int score, int gold)
    {
        _currentScore += score;

        if (_currentScore >= _scoreToNextLevel)
        {
            _currentScore -= _scoreToNextLevel; // Giữ lại điểm thừa cho cấp sau
            
            // Chờ FSM về trạng thái Playing để không ngắt ngang chuỗi combo/nổ
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState is PlayingState)
            {
                LevelUp();
            }
            else
            {
                _pendingLevelUp = true;
            }
        }

        UpdateFillImage();
    }

    private void HandleStateChanged(IGameState state)
    {
        if (_pendingLevelUp && state is PlayingState)
        {
            _pendingLevelUp = false;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        if (SaveLoadManager.Data != null)
        {
            SaveLoadManager.Data.CurrentLevel++;
            SaveLoadManager.Data.CurrentLevelProgress = null; // Xoá tiến trình màn cũ để màn mới làm mới hoàn toàn
            SaveLoadManager.Save();
        }

        UpdateLevelTexts();
        
        // Load lại grid và thông số của màn mới từ JSON
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadNextLevel();
        }
        
        Debug.Log($"[LevelProgress] CHÚC MỪNG LÊN CẤP {SaveLoadManager.Data.CurrentLevel}!");
        
        // Optional: Có thể gọi âm thanh chúc mừng tại đây
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySuccessSound(); 
        }
    }

    public void ResetProgress()
    {
        _currentScore = 0;
        _pendingLevelUp = false;
        UpdateLevelTexts();
        UpdateFillImage();
    }

    private void UpdateLevelTexts()
    {
        if (SaveLoadManager.Data != null)
        {
            int currentLevel = SaveLoadManager.Data.CurrentLevel;
            
            // Dùng SetText để tuân thủ luật Zero-GC Alloc của dự án
            if (_currentLevelText != null) 
                _currentLevelText.SetText("{0}", currentLevel);
                
            if (_nextLevelText != null) 
                _nextLevelText.SetText("{0}", currentLevel + 1);
        }
    }

    private void UpdateFillImage()
    {
        if (_fillImage != null)
        {
            // Tránh chia cho 0
            float maxScore = Mathf.Max(1f, _scoreToNextLevel);
            
            // Tạm thời set trực tiếp. Nếu muốn mượt, có thể dùng Mathf.Lerp trong hàm Coroutine hoặc Tween
            _fillImage.fillAmount = Mathf.Clamp01((float)_currentScore / maxScore);
        }
    }
}
