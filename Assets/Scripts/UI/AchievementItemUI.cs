using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý giao diện của từng dòng Thành tựu (Gắn vào các object "Thanhtuu")
/// </summary>
public class AchievementItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private Image _progressBarFill;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private Image _rewardIcon;
    
    [Header("Optional: Hiển thị khi đã hoàn thành")]
    [SerializeField] private GameObject _completedOverlay; 

    public void Setup(AchievementItem config, AchievementSaveData data)
    {
        // 1. Cập nhật Text mô tả (Zero-GC SetText)
        _descriptionText.SetText(config.Description);
        
        // 2. Cập nhật Icon phần thưởng
        if (config.RewardIcon != null && _rewardIcon != null)
        {
            _rewardIcon.sprite = config.RewardIcon;
        }

        // 3. Tính toán tiến trình
        int currentProgress = data.Progress;
        int target = config.TargetGoal;
        
        if (currentProgress > target) currentProgress = target;
        
        // Cập nhật thanh trượt (Image Filled)
        if (_progressBarFill != null)
        {
            _progressBarFill.fillAmount = (float)currentProgress / target;
        }
        
        // Cập nhật chữ tiến trình
        if (_progressText != null)
        {
            if (currentProgress >= target && !data.IsClaimed)
            {
                _progressText.SetText("Nhận"); // Hiển thị chữ Nhận thay vì 50/50
            }
            else if (data.IsClaimed)
            {
                _progressText.SetText("Đã Nhận");
            }
            else
            {
                _progressText.SetText($"{currentProgress}/{target}");
            }
        }
        
        // Cập nhật sự kiện Click cho nút (Tự động thêm Button nếu chưa có)
        Button btn = GetComponent<Button>();
        if (btn == null) btn = gameObject.AddComponent<Button>();
        
        btn.onClick.RemoveAllListeners();
        
        // Cho phép bấm nhận nếu đầy và chưa nhận
        if (currentProgress >= target && !data.IsClaimed)
        {
            btn.interactable = true;
            btn.onClick.AddListener(() => 
            {
                AchievementManager.Instance.ClaimReward(config.Id);
            });
        }
        else
        {
            btn.interactable = false; // Tắt bấm nếu chưa đầy hoặc đã nhận rồi
        }
        
        // 4. Nếu đã nhận quà (Đạt 100%) thì bật Overlay làm mờ
        if (_completedOverlay != null)
        {
            _completedOverlay.SetActive(data.IsClaimed);
        }
    }
}
