using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct ShopCategory
{
    public Sprite BoardSprite;          // Nền bảng to (Asset 44, 54, 62)
    public Sprite PriceBoardSprite;     // Nền bảng giá (Asset 45/46 hoặc 56)
    public Sprite ItemBackgroundSprite; // Nền lót vật phẩm (Asset 47, 55)
    public bool Use3DModel;             // Tích vào True cho Tab Skin
    public Sprite[] ItemSprites;        // Các vật phẩm trong tab này (chỉ dùng cho 2D)
}

public class ShopManager : MonoBehaviour
{
    [Header("Tabs")]
    [SerializeField] private Image[] _tabImages; // Kéo 3 tab (Boot, Coin, Skin) vào đây
    [SerializeField] private Color _activeTabColor = Color.white; // Màu sáng (đang chọn)
    [SerializeField] private Color _inactiveTabColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Màu tối (không chọn)

    [Header("Categories Data (0: Boost, 1: Coin, 2: Skin)")]
    [SerializeField] private ShopCategory[] _categories; // Thiết lập 3 Tab ở đây

    [Header("UI Board References")]
    [SerializeField] private Image _shopBoardImage;       // Kéo object 'Shop' vào đây (Nền to nhất)
    [SerializeField] private Image _priceBoardImage;      // Kéo object 'BangGia' vào đây
    [SerializeField] private Image _itemBackgroundImage;  // Kéo object 'Nen' vào đây

    [Header("UI Item References")]
    [SerializeField] private Image _mainBoosterImage; // Kéo object 'Item' vào đây (Hình vật phẩm)
    [SerializeField] private TextMeshProUGUI _quantityText; // Kéo object 'SoLuong' vào đây

    [Header("Indicators (Dấu chấm)")]
    [SerializeField] private Image[] _indicatorImages; // Kéo 4 cái Hienthio vào đây
    [SerializeField] private Sprite _activeDotSprite;   // Asset 53 (Màu tím)
    [SerializeField] private Sprite _inactiveDotSprite; // Asset 52 (Màu gỗ)

    [Header("3D Skin Preview")]
    [SerializeField] private ShopConfig _shopConfig;          // Kéo ShopConfig trong Resources vào đây
    [SerializeField] private PizzaPlate _platePrefab;         // Kéo Prefab PizzaPlate vào đây
    [SerializeField] private RectTransform _modelSpawnPoint;  // Tạo 1 GameObject trống nằm trong Nền lót, kéo vào đây
    [SerializeField] private float _modelScale = 45f;         // Tùy chỉnh độ lớn của đĩa 3D trong Shop

    [Header("Action UI (Nút Mua/Trang bị)")]
    [SerializeField] private Sprite _equipBoardSprite;        // Kéo khung gỗ trống (Asset 56) vào đây
    [SerializeField] private TextMeshProUGUI _priceText;      // Text hiện giá tiền trên bảng giá (bật khi Mua)
    [SerializeField] private TextMeshProUGUI _actionText;     // Text "CHỌN", "ĐANG DÙNG" (bật khi đã có Skin)
    
    private int _currentTabIndex = 0;
    private int _currentIndex = 0;
    private PizzaPlate _previewPlate;

    private void Update()
    {
        // Xoay đĩa 3D nhẹ nhàng nếu đang mở tab Skin
        if (_previewPlate != null && _previewPlate.gameObject.activeInHierarchy)
        {
            _previewPlate.transform.Rotate(Vector3.up * 30f * Time.deltaTime, Space.World);
        }
    }

    private void OnEnable()
    {
        // Mặc định chọn Tab 0 (Boost) khi mở Shop
        SwitchTab(0);
    }

    /// <summary>
    /// Chuyển đổi màu sắc của các Tab. Gắn hàm này vào nút bấm của từng Tab (truyền tham số 0, 1, 2)
    /// </summary>
    public void SwitchTab(int tabIndex)
    {
        if (_tabImages == null) return;

        _currentTabIndex = tabIndex;
        _currentIndex = 0; // Reset về item đầu tiên của Tab mới

        // 1. Cập nhật màu các nút Tab
        for (int i = 0; i < _tabImages.Length; i++)
        {
            if (_tabImages[i] != null)
            {
                _tabImages[i].color = (i == tabIndex) ? _activeTabColor : _inactiveTabColor;
            }
        }
        
        // 2. Thay đổi toàn bộ Asset của bảng Shop dựa theo Category
        if (_categories != null && tabIndex >= 0 && tabIndex < _categories.Length)
        {
            var category = _categories[tabIndex];

            // Đổi nền bảng Shop to nhất
            if (_shopBoardImage != null && category.BoardSprite != null)
                _shopBoardImage.sprite = category.BoardSprite;

            // Đổi bảng giá
            if (_priceBoardImage != null && category.PriceBoardSprite != null)
                _priceBoardImage.sprite = category.PriceBoardSprite;

            // Đổi nền lót vật phẩm
            if (_itemBackgroundImage != null && category.ItemBackgroundSprite != null)
                _itemBackgroundImage.sprite = category.ItemBackgroundSprite;

            // Xử lý bật/tắt 3D Model hoặc 2D Image
            if (category.Use3DModel)
            {
                if (_mainBoosterImage != null) _mainBoosterImage.gameObject.SetActive(false);
                if (_modelSpawnPoint != null) _modelSpawnPoint.gameObject.SetActive(true);

                // Nếu chưa spawn đĩa 3D thì spawn 1 lần duy nhất (Zero-GC)
                if (_previewPlate == null && _platePrefab != null && _modelSpawnPoint != null)
                {
                    _previewPlate = Instantiate(_platePrefab, _modelSpawnPoint);
                    _previewPlate.transform.localPosition = new Vector3(0, 0, -100f); // Kéo lên trước UI
                    _previewPlate.transform.localScale = Vector3.one * _modelScale;
                    
                    // Xóa các component vật lý không cần thiết trong UI (tránh va chạm ngoài ý muốn)
                    var colliders = _previewPlate.GetComponentsInChildren<Collider>();
                    foreach (var col in colliders) Destroy(col);
                    
                    var rb = _previewPlate.GetComponent<Rigidbody>();
                    if (rb != null) Destroy(rb);
                    
                    // Đổi Layer của toàn bộ đĩa sang UI để hiển thị cùng Canvas
                    SetLayerRecursively(_previewPlate.gameObject, LayerMask.NameToLayer("UI"));
                }

                if (_previewPlate != null) _previewPlate.gameObject.SetActive(true);
            }
            else
            {
                if (_mainBoosterImage != null) _mainBoosterImage.gameObject.SetActive(true);
                if (_modelSpawnPoint != null) _modelSpawnPoint.gameObject.SetActive(false);
                if (_previewPlate != null) _previewPlate.gameObject.SetActive(false);
            }
        }

        // 3. Cập nhật hiển thị vật phẩm hiện tại
        UpdateUI();
    }

    /// <summary>
    /// Gắn hàm này vào OnClick của nút Mũi Tên Phải
    /// </summary>
    public void NextItem()
    {
        if (_categories == null || _categories.Length <= _currentTabIndex) return;
        var category = _categories[_currentTabIndex];

        int maxCount = 0;
        if (category.Use3DModel)
        {
            if (_shopConfig == null || _shopConfig.Skins == null) return;
            maxCount = _shopConfig.Skins.Length;
        }
        else
        {
            if (category.ItemSprites == null) return;
            maxCount = category.ItemSprites.Length;
        }

        if (maxCount == 0) return;

        _currentIndex++;
        if (_currentIndex >= maxCount)
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
        if (_categories == null || _categories.Length <= _currentTabIndex) return;
        var category = _categories[_currentTabIndex];

        int maxCount = 0;
        if (category.Use3DModel)
        {
            if (_shopConfig == null || _shopConfig.Skins == null) return;
            maxCount = _shopConfig.Skins.Length;
        }
        else
        {
            if (category.ItemSprites == null) return;
            maxCount = category.ItemSprites.Length;
        }

        if (maxCount == 0) return;

        _currentIndex--;
        if (_currentIndex < 0)
        {
            _currentIndex = maxCount - 1; // Quay vòng về cuối
        }
        UpdateUI();
    }

    /// <summary>
    /// Cập nhật hiển thị (Thay đổi Sprite hoàn toàn Zero-GC)
    /// </summary>
    private void UpdateUI()
    {
        if (_categories == null || _categories.Length <= _currentTabIndex) return;
        var category = _categories[_currentTabIndex];

        // 1. Thay đổi hình ảnh vật phẩm ở giữa (2D hoặc 3D)
        if (category.Use3DModel)
        {
            if (_shopConfig != null && _shopConfig.Skins != null && _shopConfig.Skins.Length > 0 && _previewPlate != null)
            {
                var skinData = _shopConfig.Skins[_currentIndex];
                if (skinData != null)
                {
                    _previewPlate.ApplySkinDirectly(skinData.Texture);
                }
            }
        }
        else
        {
            if (category.ItemSprites != null && category.ItemSprites.Length > 0 && _mainBoosterImage != null)
            {
                _mainBoosterImage.sprite = category.ItemSprites[_currentIndex];
            }
        }

        // 2. Thay đổi trạng thái các dấu chấm (Indicator)
        int currentMaxItems = category.Use3DModel ? 
            (_shopConfig != null && _shopConfig.Skins != null ? _shopConfig.Skins.Length : 0) : 
            (category.ItemSprites != null ? category.ItemSprites.Length : 0);

        for (int i = 0; i < _indicatorImages.Length; i++)
        {
            if (_indicatorImages[i] != null)
            {
                // Ẩn indicator nếu vượt quá số lượng item hiện tại
                if (i >= currentMaxItems)
                {
                    _indicatorImages[i].gameObject.SetActive(false);
                }
                else
                {
                    _indicatorImages[i].gameObject.SetActive(true);
                    _indicatorImages[i].sprite = (i == _currentIndex) ? _activeDotSprite : _inactiveDotSprite;
                }
            }
        }

        // 3. Cập nhật Text hiển thị số lượng (Tạm thời chỉ hiển thị cho Boosters)
        if (_quantityText != null && SaveLoadManager.Data != null)
        {
            // Tạm thời chỉ có dữ liệu số lượng cho Tab Boost (Index 0). Các tab khác nếu cần lưu thì bạn bổ sung vào PlayerData sau.
            if (_currentTabIndex == 0 && SaveLoadManager.Data.BoostersOwned != null && _currentIndex < SaveLoadManager.Data.BoostersOwned.Count)
            {
                _quantityText.gameObject.SetActive(true);
                _quantityText.SetText("x{0}", SaveLoadManager.Data.BoostersOwned[_currentIndex]);
            }
            else
            {
                // Tab Coin và Skin hiện tại chưa làm logic sở hữu số lượng, tạm ẩn đi
                _quantityText.gameObject.SetActive(false);
            }
        }

        // 4. Cập nhật nút Mua/Trang bị
        UpdateActionUI();
    }

    private void UpdateActionUI()
    {
        if (SaveLoadManager.Data == null) return;
        var category = _categories[_currentTabIndex];

        // LOGIC CHO TAB SKIN (Tab 2)
        if (_currentTabIndex == 2 && category.Use3DModel && _shopConfig != null && _shopConfig.Skins.Length > 0)
        {
            var skinData = _shopConfig.Skins[_currentIndex];
            bool isOwned = SaveLoadManager.Data.UnlockedSkins.Contains(skinData.Id);
            bool isEquipped = SaveLoadManager.Data.CurrentSkinId == skinData.Id;

            if (isOwned)
            {
                // Đã sở hữu: Chuyển bảng giá thành khung gỗ trống, ẩn text giá, hiện text CHỌN / ĐANG DÙNG
                if (_equipBoardSprite != null) _priceBoardImage.sprite = _equipBoardSprite;
                
                if (_priceText != null) _priceText.gameObject.SetActive(false);
                if (_actionText != null)
                {
                    _actionText.gameObject.SetActive(true);
                    _actionText.text = isEquipped ? "ĐANG DÙNG" : "CHỌN";
                }
            }
            else
            {
                // Chưa sở hữu: Chuyển bảng giá về hình Asset 46 (có chữ BUY sẵn), hiện text giá, ẩn text Action
                _priceBoardImage.sprite = category.PriceBoardSprite;
                
                if (_actionText != null) _actionText.gameObject.SetActive(false); // Ẩn text Mua vì hình đã có chữ BUY
                if (_priceText != null)
                {
                    _priceText.gameObject.SetActive(true);
                    _priceText.text = skinData.Price.ToString();
                }
            }
        }
        else
        {
            // Reset hiển thị cho các Tab khác (Mặc định là dùng hình gốc của category)
            _priceBoardImage.sprite = category.PriceBoardSprite;
            if (_priceText != null)
            {
                _priceText.gameObject.SetActive(true);
                // TODO: Điền logic lấy giá cho Boost hoặc Coin
                _priceText.text = "???"; 
            }
            // Tab Boost/Coin nếu hình đã có chữ BUY thì ẩn action text đi
            if (_actionText != null) _actionText.gameObject.SetActive(false);
        }
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child != null) SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    /// <summary>
    /// Gắn hàm này vào sự kiện OnClick của Button trên Bảng Giá
    /// </summary>
    public void OnActionClicked()
    {
        if (SaveLoadManager.Data == null) return;

        // Xử lý logic Mua / Trang Bị cho Tab Skin
        if (_currentTabIndex == 2 && _shopConfig != null && _shopConfig.Skins.Length > 0)
        {
            var skinData = _shopConfig.Skins[_currentIndex];
            bool isOwned = SaveLoadManager.Data.UnlockedSkins.Contains(skinData.Id);

            if (isOwned)
            {
                // Người chơi đã sở hữu -> Trang bị
                SaveLoadManager.Data.CurrentSkinId = skinData.Id;
                SaveLoadManager.Save();
                UpdateActionUI();
                Debug.Log($"[Shop] Đã trang bị skin: {skinData.Id}");
            }
            else
            {
                // Người chơi chưa sở hữu -> Tiến hành Mua
                if (SaveLoadManager.Data.Gold >= skinData.Price)
                {
                    // Trừ tiền vàng
                    SaveLoadManager.Data.Gold -= skinData.Price;
                    // Mở khóa skin
                    SaveLoadManager.Data.UnlockedSkins.Add(skinData.Id);
                    // Tự động trang bị sau khi mua
                    SaveLoadManager.Data.CurrentSkinId = skinData.Id;
                    
                    SaveLoadManager.Save();
                    UpdateActionUI();
                    Debug.Log($"[Shop] Mua thành công skin: {skinData.Id}");
                }
                else
                {
                    Debug.LogWarning("[Shop] Không đủ vàng để mua skin này!");
                    // TODO: Gọi hiệu ứng rung lắc nút bảng giá hoặc popup báo hết tiền (nếu có)
                }
            }
        }
    }
}
