using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("Tabs")]
    [SerializeField] private Image[] _tabImages; // Kéo 3 tab (Boot, Coin, Skin) vào đây
    [SerializeField] private Color _activeTabColor = Color.white; // Màu sáng (đang chọn)
    [SerializeField] private Color _inactiveTabColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Màu tối (không chọn)

    [Header("Boosters Data")]
    [SerializeField] private Sprite[] _boosterSprites; // Kéo thả Asset 48, 49, 50, 51 vào đây

    [Header("UI References")]
    [SerializeField] private Image _mainBoosterImage; // Kéo object 'Item' vào đây (KHÔNG phải Nen)
    [SerializeField] private TextMeshProUGUI _quantityText; // Kéo object 'SoLuong' vào đây

    [Header("Indicators (Dấu chấm)")]
    [SerializeField] private Image[] _indicatorImages; // Kéo 4 cái Hienthio vào đây
    [SerializeField] private Sprite _activeDotSprite;   // Asset 53 (Màu tím)
    [SerializeField] private Sprite _inactiveDotSprite; // Asset 52 (Màu gỗ)

    private int _currentIndex = 0;

    private void OnEnable()
    {
        // Mỗi lần mở Shop lên thì reset về item đầu tiên
        _currentIndex = 0;
        UpdateUI();
        
        // Mặc định chọn Tab 0 (Boost)
        SwitchTab(0);
    }

    /// <summary>
    /// Chuyển đổi màu sắc của các Tab. Gắn hàm này vào nút bấm của từng Tab (truyền tham số 0, 1, 2)
    /// </summary>
    public void SwitchTab(int tabIndex)
    {
        if (_tabImages == null) return;

        for (int i = 0; i < _tabImages.Length; i++)
        {
            if (_tabImages[i] != null)
            {
                // Thay đổi màu: Tab đang chọn thì trắng (sáng), Tab không chọn thì xám (tối)
                _tabImages[i].color = (i == tabIndex) ? _activeTabColor : _inactiveTabColor;
            }
        }
        
        // TODO: Mở/tắt các Panel tương ứng với Tab (PanelBoost, PanelCoin, PanelSkin) ở đây
    }

    /// <summary>
    /// Gắn hàm này vào OnClick của nút Mũi Tên Phải
    /// </summary>
    public void NextItem()
    {
        if (_boosterSprites == null || _boosterSprites.Length == 0) return;

        _currentIndex++;
        if (_currentIndex >= _boosterSprites.Length)
        {
            _currentIndex = 0; // Quay vòng lại đầu
        }
        UpdateUI();
    }

    /// <summary>
    /// Gắn hàm này vào OnClick của nút Mũi Tên Trái
    /// </summary>
    public void PrevItem()
    {
        if (_boosterSprites == null || _boosterSprites.Length == 0) return;

        _currentIndex--;
        if (_currentIndex < 0)
        {
            _currentIndex = _boosterSprites.Length - 1; // Quay vòng về cuối
        }
        UpdateUI();
    }

    /// <summary>
    /// Cập nhật hiển thị (Thay đổi Sprite hoàn toàn Zero-GC)
    /// </summary>
    private void UpdateUI()
    {
        // 1. Thay đổi hình ảnh Booster ở giữa
        if (_boosterSprites.Length > 0 && _mainBoosterImage != null)
        {
            _mainBoosterImage.sprite = _boosterSprites[_currentIndex];
        }

        // 2. Thay đổi trạng thái các dấu chấm (Indicator)
        for (int i = 0; i < _indicatorImages.Length; i++)
        {
            if (_indicatorImages[i] != null)
            {
                // Nếu đúng index hiện tại thì gán hình dấu chấm Tím (Asset 53), ngược lại gán màu gỗ (Asset 52)
                _indicatorImages[i].sprite = (i == _currentIndex) ? _activeDotSprite : _inactiveDotSprite;
            }
        }

        // 3. Cập nhật Text hiển thị số lượng (Zero-GC SetText)
        if (_quantityText != null && SaveLoadManager.Data != null)
        {
            if (SaveLoadManager.Data.BoostersOwned != null && _currentIndex < SaveLoadManager.Data.BoostersOwned.Count)
            {
                _quantityText.SetText("x{0}", SaveLoadManager.Data.BoostersOwned[_currentIndex]);
            }
        }
    }
}
