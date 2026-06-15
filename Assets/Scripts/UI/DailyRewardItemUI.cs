using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DailyRewardItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject _darkOverlay;
    [SerializeField] private GameObject _highlightBg;
    
    [Header("Normal Texts")]
    [SerializeField] private TMP_Text _textDown;
    [SerializeField] private TMP_Text _textUp;

    [Header("Highlight Texts")]
    [SerializeField] private TMP_Text _textDownHighlight;
    [SerializeField] private TMP_Text _textUpHighlight;

    [Header("Icon")]
    [SerializeField] private Image _rewardIcon;

    public void Setup(bool isClaimed, bool isCurrentDay, string dayName, string rewardText, Sprite iconSprite)
    {
        // Gán Icon nếu có
        if (_rewardIcon != null && iconSprite != null)
        {
            _rewardIcon.sprite = iconSprite;
        }

        // Hiển thị tên ngày (vd: Day 1) cho CẢ 2 TextUp
        if (_textUp != null) _textUp.SetText(dayName);
        if (_textUpHighlight != null) _textUpHighlight.SetText(dayName);

        if (isClaimed)
        {
            // Đã nhận: Bật màng đen, tắt viền cam, chữ là CLAIMED
            if (_darkOverlay != null) _darkOverlay.SetActive(true);
            if (_highlightBg != null) _highlightBg.SetActive(false);
            
            if (_textDown != null) _textDown.SetText("CLAIMED");
            if (_textDownHighlight != null) _textDownHighlight.SetText("CLAIMED");
        }
        else if (isCurrentDay)
        {
            // Hôm nay: Tắt màng đen, bật viền cam rực rỡ, hiển thị số lượng quà
            if (_darkOverlay != null) _darkOverlay.SetActive(false);
            if (_highlightBg != null) _highlightBg.SetActive(true);
            
            if (_textDown != null) _textDown.SetText(rewardText);
            if (_textDownHighlight != null) _textDownHighlight.SetText(rewardText);
            
            transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        }
        else
        {
            // Các ngày sau: Tắt màng đen, tắt viền cam, hiển thị số lượng quà
            if (_darkOverlay != null) _darkOverlay.SetActive(false);
            if (_highlightBg != null) _highlightBg.SetActive(false);
            
            if (_textDown != null) _textDown.SetText(rewardText);
            if (_textDownHighlight != null) _textDownHighlight.SetText(rewardText);
            
            transform.localScale = Vector3.one;
        }
    }
}
