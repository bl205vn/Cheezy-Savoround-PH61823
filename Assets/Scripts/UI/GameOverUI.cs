using UnityEngine;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _bestScoreText;
    [SerializeField] private GameObject _gameOverPanel;

    private void Awake()
    {
        // Đăng ký event ở Awake và OnDestroy để script vẫn 'nghe' được sự kiện 
        // ngay cả khi GameObject này đang bị tắt (SetActive = false)
        GameEvents.OnGameOver += ShowGameOver;
        GameStateManager.OnStateChanged += HandleStateChanged;

        if (_gameOverPanel == null)
            _gameOverPanel = gameObject; // Mặc định là chính GameObject này nếu không gán
            
        _gameOverPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        GameEvents.OnGameOver -= ShowGameOver;
        GameStateManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(IGameState state)
    {
        if (state is PlayingState)
        {
            _gameOverPanel.SetActive(false);
        }
    }

    private void ShowGameOver()
    {
        _gameOverPanel.SetActive(true);
        
        if (SaveLoadManager.Data != null)
        {
            // Tuân thủ Zero-GC Alloc bằng cách dùng SetText với định dạng
            if (_scoreText != null)
                _scoreText.SetText("{0}", SaveLoadManager.Data.TotalScore);
                
            if (_bestScoreText != null)
                _bestScoreText.SetText("{0}", SaveLoadManager.Data.BestScore);
        }
    }
}
