using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct ShopCategory
{
    public Sprite BoardSprite;          // Nền bảng to (Asset 44, 54, 62)
    public Sprite PriceBoardSprite;     // Nền bảng giá (Asset 45/46 hoặc 56)
    public Sprite ItemBackgroundSprite; // Nền lót vật phẩm (Asset 47, 55)
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
    [SerializeField] private TextMeshProUGUI _coinRewardText; // Kéo object 'GIaTIenTuoi' vào đây

    [Header("Indicators (Dấu chấm)")]
    [SerializeField] private Image[] _indicatorImages; // Kéo 4 cái Hienthio vào đây
    [SerializeField] private Sprite _activeDotSprite;   // Asset 53 (Màu tím)
    [SerializeField] private Sprite _inactiveDotSprite; // Asset 52 (Màu gỗ)

    [Header("3D Skin Preview")]
    [SerializeField] private ShopConfig _shopConfig;          // Kéo ShopConfig trong Resources vào đây
    [SerializeField] private PizzaPlate _platePrefab;         // Kéo Prefab PizzaPlate vào đây
    [SerializeField] private Transform _modelSpawnPoint;      // Tạo 1 GameObject trống đặt TRƯỚC mặt Camera Preview
    [SerializeField] private float _modelScale;               // Độ lớn của đĩa 3D trong Shop
    [SerializeField] private float _modelRotationSpeed;       // Tốc độ xoay của đĩa 3D
    [SerializeField] private float _modelTiltAngle;           // Độ nghiêng của đĩa 3D (trục X) để dễ nhìn mặt đĩa
    [Header("Action UI (Nút Mua/Trang bị)")]
    [SerializeField] private GameObject _buyButtonObj;        // Kéo cái Nút Xanh lá (chữ BUY) vào đây
    [SerializeField] private Sprite _equipBoardSprite;        // Kéo khung gỗ trống (Asset 56) vào đây
    [SerializeField] private TextMeshProUGUI _priceText;      // Text hiện giá tiền nằm trên khung gỗ
    [SerializeField] private TextMeshProUGUI _actionText;     // Text "CHỌN" / "ĐANG DÙNG" nằm trên khung gỗ
    
    [Header("Top Bar UI")]
    [SerializeField] private TextMeshProUGUI _goldText;       // Text hiện số dư Xu (Money)
    
    private int _currentTabIndex = 0;
    private int _currentIndex = 0;
    private PizzaPlate _previewPlate;

    private void Update()
    {
        // Xoay đĩa 3D nhẹ nhàng nếu đang mở tab Skin
        if (_previewPlate != null && _previewPlate.gameObject.activeInHierarchy)
        {
            _previewPlate.transform.Rotate(Vector3.up * _modelRotationSpeed * Time.deltaTime, Space.World);
        }
    }

    private void OnEnable()
    {
        UpdateGoldText(); // Cập nhật số xu hiển thị khi mở Shop
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
            if (_currentTabIndex == 2) // Tab Skin dùng 3D Model
            {
                if (_mainBoosterImage != null) _mainBoosterImage.gameObject.SetActive(false);
                if (_modelSpawnPoint != null) _modelSpawnPoint.gameObject.SetActive(true);

                // Nếu chưa spawn đĩa 3D thì spawn 1 lần duy nhất (Zero-GC)
                if (_previewPlate == null && _platePrefab != null && _modelSpawnPoint != null)
                {
                    _previewPlate = Instantiate(_platePrefab, _modelSpawnPoint);
                    _previewPlate.transform.localPosition = Vector3.zero; // Sinh ra đúng ngay vị trí của Spawn Point
                    _previewPlate.transform.localRotation = Quaternion.Euler(_modelTiltAngle, 0, 0); 
                    _previewPlate.transform.localScale = Vector3.one * _modelScale;
                    
                    // Xóa các component vật lý không cần thiết trong UI (tránh va chạm ngoài ý muốn)
                    var colliders = _previewPlate.GetComponentsInChildren<Collider>();
                    foreach (var col in colliders) Destroy(col);
                    
                    var rb = _previewPlate.GetComponent<Rigidbody>();
                    if (rb != null) Destroy(rb);
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

    private int GetCurrentMaxItems()
    {
        if (_shopConfig == null) return 0;
        if (_currentTabIndex == 0 && _shopConfig.Boosters != null) return _shopConfig.Boosters.Length;
        if (_currentTabIndex == 1 && _shopConfig.CoinPacks != null) return _shopConfig.CoinPacks.Length;
        if (_currentTabIndex == 2 && _shopConfig.Skins != null) return _shopConfig.Skins.Length;
        return 0;
    }

    /// <summary>
    /// Gắn hàm này vào OnClick của nút Mũi Tên Phải
    /// </summary>
    public void NextItem()
    {
        int maxCount = GetCurrentMaxItems();
        if (maxCount == 0) return;

        _currentIndex++;
        if (_currentIndex >= maxCount) _currentIndex = 0; // Quay vòng lại đầu
        UpdateUI();
    }

    /// <summary>
    /// Gắn hàm này vào OnClick của nút Mũi Tên Trái
    /// </summary>
    public void PrevItem()
    {
        int maxCount = GetCurrentMaxItems();
        if (maxCount == 0) return;

        _currentIndex--;
        if (_currentIndex < 0) _currentIndex = maxCount - 1; // Quay vòng về cuối
        UpdateUI();
    }

    /// <summary>
    /// Cập nhật hiển thị số dư Xu hiện tại
    /// </summary>
    public void UpdateGoldText()
    {
        if (_goldText != null && SaveLoadManager.Data != null)
        {
            _goldText.text = SaveLoadManager.Data.Gold.ToString();
        }
    }

    /// <summary>
    /// Cập nhật hiển thị (Thay đổi Sprite hoàn toàn Zero-GC)
    /// </summary>
    private void UpdateUI()
    {
        if (_categories == null || _categories.Length <= _currentTabIndex) return;
        var category = _categories[_currentTabIndex];

        // 1. Thay đổi hình ảnh vật phẩm ở giữa (2D hoặc 3D)
        if (_currentTabIndex == 2) // Skin
        {
            if (_shopConfig != null && _shopConfig.Skins != null && _shopConfig.Skins.Length > 0 && _previewPlate != null)
            {
                var skinData = _shopConfig.Skins[_currentIndex];
                if (skinData != null) _previewPlate.ApplySkinDirectly(skinData.Texture);
            }
        }
        else if (_currentTabIndex == 0) // Boost
        {
            if (_shopConfig != null && _shopConfig.Boosters != null && _shopConfig.Boosters.Length > 0 && _mainBoosterImage != null)
            {
                _mainBoosterImage.sprite = _shopConfig.Boosters[_currentIndex].Icon;
            }
        }
        else if (_currentTabIndex == 1) // Coin
        {
            if (_shopConfig != null && _shopConfig.CoinPacks != null && _shopConfig.CoinPacks.Length > 0 && _mainBoosterImage != null)
            {
                _mainBoosterImage.sprite = _shopConfig.CoinPacks[_currentIndex].Icon;
            }
        }

        // 2. Thay đổi trạng thái các dấu chấm (Indicator)
        int currentMaxItems = GetCurrentMaxItems();

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
            // Tạm thời chỉ có dữ liệu số lượng cho Tab Boost (Index 0).
            if (_currentTabIndex == 0 && SaveLoadManager.Data.BoostersOwned != null && _currentIndex < SaveLoadManager.Data.BoostersOwned.Count)
            {
                _quantityText.gameObject.SetActive(true);
                _quantityText.SetText("x{0}", SaveLoadManager.Data.BoostersOwned[_currentIndex]);
            }
            else
            {
                _quantityText.gameObject.SetActive(false);
            }
        }

        // Cập nhật Text hiển thị số Xu nhận được hoặc giá trị gói Coin (Text GIaTIenTuoi)
        if (_coinRewardText != null)
        {
            if (_currentTabIndex == 1 && _shopConfig != null && _shopConfig.CoinPacks != null && _shopConfig.CoinPacks.Length > 0)
            {
                _coinRewardText.gameObject.SetActive(true);
                _coinRewardText.text = _shopConfig.CoinPacks[_currentIndex].RewardAmount.ToString() + " Coin";
            }
            else
            {
                _coinRewardText.gameObject.SetActive(false);
            }
        }

        // 4. Cập nhật nút Mua/Trang bị
        UpdateActionUI();
    }

    private void UpdateActionUI()
    {
        if (SaveLoadManager.Data == null) return;
        var category = _categories[_currentTabIndex];
        
        // Lấy component Button của Bảng Giá để bật/tắt (Ngăn click nhầm)
        Button boardButton = _priceBoardImage.GetComponent<Button>();

        // LOGIC CHO TAB SKIN (Tab 2)
        if (_currentTabIndex == 2 && _shopConfig != null && _shopConfig.Skins.Length > 0)
        {
            var skinData = _shopConfig.Skins[_currentIndex];
            bool isOwned = SaveLoadManager.Data.UnlockedSkins.Contains(skinData.Id);
            bool isEquipped = SaveLoadManager.Data.CurrentSkinId == skinData.Id;

            if (isOwned)
            {
                // TẮT ẨN cái nút xanh lá chữ BUY đi
                if (_buyButtonObj != null) _buyButtonObj.SetActive(false);
                
                // Cho phép bấm vào Bảng gỗ để Chọn
                if (boardButton != null) boardButton.enabled = true;

                // Chuyển bảng giá thành khung gỗ trống, ẩn text giá, hiện text CHỌN / ĐANG DÙNG đè lên khung gỗ
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
                // BẬT LÊN cái nút xanh lá chữ BUY
                if (_buyButtonObj != null) _buyButtonObj.SetActive(true);
                
                // CẤM bấm vào Bảng gỗ (Bắt buộc phải bấm nút xanh lá để mua)
                if (boardButton != null) boardButton.enabled = false;

                // Chưa sở hữu: Chuyển bảng giá về hình Asset 46 (có đồng xu), hiện text giá, ẩn text Action
                _priceBoardImage.sprite = category.PriceBoardSprite;
                
                if (_actionText != null) _actionText.gameObject.SetActive(false); 
                if (_priceText != null)
                {
                    _priceText.gameObject.SetActive(true);
                    _priceText.text = skinData.Price.ToString();
                }
            }
        }
        else if (_currentTabIndex == 0) // Tab Boost
        {
            // Tab Boost: Luôn bật nút BUY xanh lá
            if (_buyButtonObj != null) _buyButtonObj.SetActive(true);
            
            // Tab Boost: Cấm bấm vào Bảng giá, chỉ cho bấm nút BUY
            if (boardButton != null) boardButton.enabled = false;

            _priceBoardImage.sprite = category.PriceBoardSprite;
            
            // Dùng _priceText vì bảng gỗ Boost có đồng xu (cần lệch sang phải)
            if (_actionText != null) _actionText.gameObject.SetActive(false);
            if (_priceText != null)
            {
                _priceText.gameObject.SetActive(true);
                if (_shopConfig != null && _shopConfig.Boosters != null && _shopConfig.Boosters.Length > 0)
                {
                    _priceText.text = _shopConfig.Boosters[_currentIndex].Price.ToString(); 
                }
            }
        }
        else if (_currentTabIndex == 1) // Tab Coin
        {
            // Tab Coin: Luôn bật nút BUY xanh lá
            if (_buyButtonObj != null) _buyButtonObj.SetActive(true);
            
            // Tab Coin: Cấm bấm vào Bảng giá, chỉ cho bấm nút BUY
            if (boardButton != null) boardButton.enabled = false;

            _priceBoardImage.sprite = category.PriceBoardSprite;

            // Dùng _actionText vì bảng gỗ Coin trống (cần canh giữa), tận dụng text của Skin
            if (_priceText != null) _priceText.gameObject.SetActive(false);
            if (_actionText != null)
            {
                _actionText.gameObject.SetActive(true);
                if (_shopConfig != null && _shopConfig.CoinPacks != null && _shopConfig.CoinPacks.Length > 0)
                {
                    _actionText.text = _shopConfig.CoinPacks[_currentIndex].PriceString; 
                }
            }
        }
    }



    /// <summary>
    /// Gắn hàm này vào sự kiện OnClick của Button trên Bảng Giá HOẶC Nút BUY màu xanh lá
    /// </summary>
    public void OnActionClicked()
    {
        if (SaveLoadManager.Data == null) return;

        // LOGIC CHO TAB SKIN
        if (_currentTabIndex == 2 && _shopConfig != null && _shopConfig.Skins.Length > 0)
        {
            var skinData = _shopConfig.Skins[_currentIndex];
            bool isOwned = SaveLoadManager.Data.UnlockedSkins.Contains(skinData.Id);

            if (isOwned)
            {
                // Trang bị
                SaveLoadManager.Data.CurrentSkinId = skinData.Id;
                SaveLoadManager.Save();
                UpdateActionUI();
                Debug.Log($"[Shop] Đã trang bị skin: {skinData.Id}");
            }
            else
            {
                // Tiến hành Mua bằng Xu
                if (SaveLoadManager.Data.Gold >= skinData.Price)
                {
                    SaveLoadManager.Data.Gold -= skinData.Price;
                    SaveLoadManager.Data.UnlockedSkins.Add(skinData.Id);
                    SaveLoadManager.Data.CurrentSkinId = skinData.Id;
                    
                    SaveLoadManager.Save();
                    UpdateActionUI();
                    UpdateGoldText();
                    Debug.Log($"[Shop] Mua thành công skin: {skinData.Id}");
                }
                else
                {
                    Debug.LogWarning("[Shop] Không đủ vàng để mua skin này!");
                }
            }
        }
        // LOGIC CHO TAB COIN (Dùng để test)
        else if (_currentTabIndex == 1 && _shopConfig != null && _shopConfig.CoinPacks.Length > 0)
        {
            var packData = _shopConfig.CoinPacks[_currentIndex];
            
            // Tạm thời cộng thẳng xu để test, không bắt thanh toán tiền thật
            SaveLoadManager.Data.Gold += packData.RewardAmount;
            SaveLoadManager.Save();
            
            UpdateGoldText();
            Debug.Log($"[Shop] Đã test mua gói {packData.Id}, nhận được {packData.RewardAmount} Xu!");
        }
        // LOGIC CHO TAB BOOST
        else if (_currentTabIndex == 0 && _shopConfig != null && _shopConfig.Boosters.Length > 0)
        {
            var boostData = _shopConfig.Boosters[_currentIndex];
            if (SaveLoadManager.Data.Gold >= boostData.Price)
            {
                SaveLoadManager.Data.Gold -= boostData.Price;
                
                // Tăng số lượng Booster sở hữu (nếu mảng đã được khởi tạo)
                if (SaveLoadManager.Data.BoostersOwned != null && _currentIndex < SaveLoadManager.Data.BoostersOwned.Count)
                {
                    SaveLoadManager.Data.BoostersOwned[_currentIndex]++;
                }
                
                SaveLoadManager.Save();
                UpdateActionUI();
                UpdateGoldText();
                UpdateUI(); // Cập nhật lại text x1, x2
                Debug.Log($"[Shop] Mua thành công boost: {boostData.Id}");
            }
            else
            {
                Debug.LogWarning("[Shop] Không đủ vàng để mua boost này!");
            }
        }
    }
}
