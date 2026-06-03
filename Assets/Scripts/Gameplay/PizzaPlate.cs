using UnityEngine;
using DG.Tweening;

public class PizzaPlate : MonoBehaviour
{
    [SerializeField] private float _spawnHeight = 0f;
    [SerializeField] private float _pickUpOffset = 0.5f;
    [Tooltip("Nâng miếng bánh lên trên mặt đĩa (chỉnh thành 1 nếu cần)")]
    [SerializeField] private float _sliceYOffset = 1f;

    [Header("Game Feel")]
    [SerializeField] private float _snapSquashDuration = 0.08f;
    [SerializeField] private Vector3 _squashScaleMultiplier = new Vector3(1.3f, 0.7f, 1.3f);
    [SerializeField] private Vector3 _stretchScaleMultiplier = new Vector3(0.85f, 1.2f, 0.85f);
    [SerializeField] private float _shakeDuration = 0.3f;
    [SerializeField] private float _shakeStrength = 0.3f;
    [SerializeField] private float _returnDuration = 0.2f;
    [SerializeField] private float _shrinkDuration = 0.2f;
    private Vector3 _baseScale = Vector3.one;
    public Vector3 BaseScale => _baseScale;

    private Vector3 _originalPosition;
    private Transform _originalParent;
    private PizzaSliceVisual[] _slices; // Mảng chứa các miếng theo index/góc quay

    public PizzaSliceVisual[] Slices => _slices; // Cho phép các Manager đọc dữ liệu miếng bánh trên đĩa
    public float SliceYOffset => _sliceYOffset;
    public int Priority { get; set; } = 0; // Ưu tiên 9 -> 0 cho logic Trạm trung chuyển
    public bool IsPurging { get; set; } = false; // Cờ đánh dấu đĩa đang trong trạng thái xả rác (không được hút)
    public bool IsReturning { get; private set; } = false; // Cờ khóa tương tác khi đĩa đang bay về khay

    // --- ZERO GC BUFFERS ---
    private readonly System.Collections.Generic.Dictionary<int, int> _typeCountBuffer = new System.Collections.Generic.Dictionary<int, int>();
    private readonly System.Collections.Generic.List<int> _availableTypesBuffer = new System.Collections.Generic.List<int>();
    public void Initialize(Transform parentSlot)
    {
        _originalParent = parentSlot;
        transform.SetParent(parentSlot);
        transform.localPosition = new Vector3(0, _spawnHeight, 0); // Sinh cách khoảng y
        _originalPosition = transform.position;
        _baseScale = transform.localScale; // Cập nhật scale gốc để dùng cho các logic UI/Ghost
    }

    public void PickUp()
    {
        transform.DOKill(); // Dừng mọi hiệu ứng (như đang bay về khay) nếu người chơi chộp lại đĩa giữa chừng
        // Nâng đĩa lên một chút theo trục Y để tạo hiệu ứng nhấc lên
        transform.position = new Vector3(transform.position.x, _originalPosition.y + _pickUpOffset, transform.position.z);
    }

    public void DragTo(Vector3 worldPosition)
    {
        // Di chuyển đĩa theo chuột, giữ nguyên cao độ lúc đang nhấc
        transform.position = new Vector3(worldPosition.x, transform.position.y, worldPosition.z);
    }

    public void ReturnToOriginalSlot()
    {
        // Trả đĩa về vị trí khay ban đầu (instant)
        transform.position = _originalPosition;
        transform.SetParent(_originalParent);
    }

    public void PlayShakeAndReturn()
    {
        transform.DOKill();
        IsReturning = true; // Khóa tương tác
        
        Sequence seq = DOTween.Sequence();
        
        // Rung ngang tại vị trí hiện tại
        seq.Append(transform.DOShakePosition(_shakeDuration, strength: new Vector3(_shakeStrength, 0, 0), vibrato: 20, randomness: 0, snapping: false, fadeOut: true));
        
        // Bay mượt về vị trí gốc trên khay
        seq.Append(transform.DOMove(_originalPosition, _returnDuration).SetEase(Ease.OutQuad));
        
        seq.OnComplete(() => 
        {
            // Trả lại parent là khay sau khi bay xong
            transform.SetParent(_originalParent);
            IsReturning = false; // Mở khóa tương tác
        });
    }

    public void PlayShrinkAndReturn()
    {
        transform.DOKill();
        
        // Thu nhỏ lại thành 0
        transform.DOScale(Vector3.zero, _shrinkDuration).SetEase(Ease.InBack).OnComplete(() => 
        {
            // Reset lại scale gốc trước khi tống vào Pool (để lần sau lấy ra không bị tàng hình)
            transform.localScale = _baseScale;
            ObjectPoolManager.Instance.ReturnPizzaPlate(this);
        });
    }

    public void PlaceAt(Vector3 targetPos, Transform newParent)
    {
        // Đặt parent nhưng KHÔNG di chuyển tức thì (để Coroutine lo)
        transform.SetParent(newParent, true);
        _originalParent = newParent;
        _originalPosition = targetPos;
        _baseScale = transform.localScale; // Lưu lại scale sau khi parent để tránh lỗi scale 2 lần hoặc sai lệch kích thước lưới
    }

    public System.Collections.IEnumerator AnimateToCell(Vector3 startPos, Vector3 endPos, float duration = 0.25f, System.Action onComplete = null)
    {
        Vector3 midPoint = (startPos + endPos) * 0.5f;
        Vector3 controlPoint = midPoint + Vector3.up * 0.8f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float u = 1f - t;
            Vector3 pos = u * u * startPos
                        + 2f * u * t * controlPoint
                        + t * t * endPos;
            
            transform.position = pos;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = endPos;
        onComplete?.Invoke();
    }

    public void PlaySnapEffect()
    {
        transform.DOKill();
        transform.localScale = _baseScale; // Reset lại scale gốc trong trường hợp tween cũ bị ngắt quãng
        
        Sequence seq = DOTween.Sequence();
        
        Vector3 squashScale = new Vector3(_baseScale.x * _squashScaleMultiplier.x, _baseScale.y * _squashScaleMultiplier.y, _baseScale.z * _squashScaleMultiplier.z);
        Vector3 stretchScale = new Vector3(_baseScale.x * _stretchScaleMultiplier.x, _baseScale.y * _stretchScaleMultiplier.y, _baseScale.z * _stretchScaleMultiplier.z);
        
        seq.Append(transform.DOScale(squashScale, _snapSquashDuration).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(stretchScale, _snapSquashDuration).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(_baseScale, _snapSquashDuration).SetEase(Ease.OutBounce));
    }

    private void OnDestroy()
    {
        // Thu hồi toàn bộ miếng bánh về Pool khi đĩa bị hủy (tránh thất thoát Pool khi đổi màn)
        ClearSlices();
    }

    public void ClearSlices()
    {
        if (_slices == null) return;
        for (int i = 0; i < _slices.Length; i++)
        {
            if (_slices[i] != null && ObjectPoolManager.Instance != null)
            {
                _slices[i].transform.DOKill(); // Đảm bảo dừng mọi animation xoay trước khi vứt vào Pool
                ObjectPoolManager.Instance.ReturnPizzaSlice(_slices[i]);
                _slices[i] = null;
            }
        }
    }

    public System.Collections.Generic.List<int> GetAvailableTypes()
    {
        _availableTypesBuffer.Clear();
        _typeCountBuffer.Clear();

        if (_slices == null) return _availableTypesBuffer;
        
        foreach (var slice in _slices)
        {
            if (slice != null)
            {
                if (!_typeCountBuffer.ContainsKey(slice.TypeIndex)) _typeCountBuffer[slice.TypeIndex] = 0;
                _typeCountBuffer[slice.TypeIndex]++;
                
                if (!_availableTypesBuffer.Contains(slice.TypeIndex))
                {
                    _availableTypesBuffer.Add(slice.TypeIndex);
                }
            }
        }
        
        // Sắp xếp các loại bánh theo số lượng giảm dần
        // Điều này rất quan trọng để tránh vòng lặp vô hạn (2 đĩa cứ hút bánh qua lại)
        _availableTypesBuffer.Sort((a, b) => _typeCountBuffer[b].CompareTo(_typeCountBuffer[a]));
        
        return _availableTypesBuffer;
    }

    public bool HasType(int typeIndex)
    {
        if (_slices == null) return false;
        foreach (var slice in _slices)
        {
            if (slice != null && slice.TypeIndex == typeIndex) return true;
        }
        return false;
    }

    public int GetCountOf(int typeIndex)
    {
        if (_slices == null) return 0;
        int count = 0;
        foreach (var slice in _slices)
        {
            if (slice != null && slice.TypeIndex == typeIndex) count++;
        }
        return count;
    }

    public int GetTotalSlices()
    {
        if (_slices == null) return 0;
        int count = 0;
        foreach (var slice in _slices)
        {
            if (slice != null) count++;
        }
        return count;
    }

    public bool IsFull()
    {
        return GetTotalSlices() >= (_slices != null ? _slices.Length : 6);
    }

    public int GetMajorityType()
    {
        if (_slices == null) return -1;
        _typeCountBuffer.Clear();
        foreach (var slice in _slices)
        {
            if (slice != null)
            {
                if (!_typeCountBuffer.ContainsKey(slice.TypeIndex)) _typeCountBuffer[slice.TypeIndex] = 0;
                _typeCountBuffer[slice.TypeIndex]++;
            }
        }
        int maxCount = 0;
        int majorityType = -1;
        foreach (var kvp in _typeCountBuffer)
        {
            if (kvp.Value > maxCount)
            {
                maxCount = kvp.Value;
                majorityType = kvp.Key;
            }
        }
        return majorityType;
    }

    public int GetMinorityType(int excludeType = -1)
    {
        if (_slices == null) return -1;
        _typeCountBuffer.Clear();
        foreach (var slice in _slices)
        {
            if (slice != null && slice.TypeIndex != excludeType)
            {
                if (!_typeCountBuffer.ContainsKey(slice.TypeIndex)) _typeCountBuffer[slice.TypeIndex] = 0;
                _typeCountBuffer[slice.TypeIndex]++;
            }
        }
        int minCount = int.MaxValue;
        int minorityType = -1;
        foreach (var kvp in _typeCountBuffer)
        {
            if (kvp.Value < minCount)
            {
                minCount = kvp.Value;
                minorityType = kvp.Key;
            }
        }
        return minorityType;
    }

    public bool IsFullAndPure()
    {
        if (!IsFull()) return false;
        int firstType = -1;
        foreach (var slice in _slices)
        {
            if (slice == null) continue;
            if (firstType == -1) firstType = slice.TypeIndex;
            else if (slice.TypeIndex != firstType) return false;
        }
        return true;
    }

    public bool TryAddSlice(PizzaSliceVisual slice, out int addedIndex)
    {
        addedIndex = -1;
        if (IsFull()) return false;
        
        for (int i = 0; i < _slices.Length; i++)
        {
            if (_slices[i] == null)
            {
                _slices[i] = slice;
                addedIndex = i;
                
                // Chuẩn bị cho Animation bay
                // Bắt buộc dùng true để giữ nguyên World Position, tránh lỗi teleport!
                slice.transform.SetParent(this.transform, true);
                
                // Tự động dồn mảng và tạo hiệu ứng xoay về chỗ mới
                CompactAndRearrangeSlices();
                
                return true;
            }
        }
        return false;
    }

    public void CompactAndRearrangeSlices()
    {
        if (_slices == null) return;
        
        int nonNullIndex = 0;
        
        // Compact array (dồn null về cuối) in-place
        for (int i = 0; i < _slices.Length; i++)
        {
            if (_slices[i] != null)
            {
                if (i != nonNullIndex)
                {
                    _slices[nonNullIndex] = _slices[i];
                    _slices[i] = null;
                }
                nonNullIndex++;
            }
        }
        
        // Tính lại góc xoay mượt mà cho các miếng bánh còn lại
        float angleStep = 360f / _slices.Length;
        for (int i = 0; i < nonNullIndex; i++)
        {
            PizzaSliceVisual slice = _slices[i];
            if (slice != null)
            {
                float targetAngle = i * angleStep;
                
                slice.transform.DOKill(); // Tránh bị đè tween
                slice.transform.DOLocalRotate(new Vector3(0, targetAngle, 0), 0.2f).SetEase(Ease.OutQuad);
            }
        }
    }

    public PizzaSliceVisual RemoveSliceOfType(int typeIndex)
    {
        if (_slices == null) return null;
        for (int i = _slices.Length - 1; i >= 0; i--)
        {
            if (_slices[i] != null && _slices[i].TypeIndex == typeIndex)
            {
                PizzaSliceVisual slice = _slices[i];
                _slices[i] = null;
                
                // Dồn mảng và xếp lại bánh sau khi rút 1 miếng
                CompactAndRearrangeSlices();
                
                return slice;
            }
        }
        return null;
    }

    public void GenerateRandomSlices()
    {
        if (LevelManager.CurrentLevelData == null) return;

        ClearSlices(); // Dọn dẹp an toàn trước khi sinh mới

        int maxSlices = LevelManager.CurrentLevelData.maxSlices;
        int[] availableTypes = LevelManager.CurrentLevelData.availablePizzaTypes;

        if (availableTypes == null || availableTypes.Length == 0)
        {
            availableTypes = new int[] { 0 }; // Dự phòng an toàn nếu JSON lỗi
        }

        if (_slices == null || _slices.Length != maxSlices)
        {
            _slices = new PizzaSliceVisual[maxSlices];
        }

        // Sinh ngẫu nhiên số lượng miếng dựa theo tỉ lệ phần trăm cấu hình trong JSON (Roulette Wheel)
        int sliceCount = 1;
        float[] probabilities = LevelManager.CurrentLevelData.sliceCountProbabilities;

        if (probabilities != null && probabilities.Length >= maxSlices)
        {
            float totalProb = 0f;
            for (int i = 0; i < maxSlices; i++) totalProb += probabilities[i];

            if (totalProb > 0f)
            {
                float randomPoint = Random.value * totalProb;
                float currentSum = 0f;
                
                for (int i = 0; i < maxSlices; i++)
                {
                    currentSum += probabilities[i];
                    if (randomPoint <= currentSum)
                    {
                        sliceCount = i + 1; // i = 0 tương đương 1 miếng
                        break;
                    }
                }
            }
            else
            {
                sliceCount = Random.Range(1, maxSlices + 1); // Fallback nếu mảng toàn 0
            }
        }
        else
        {
            sliceCount = Random.Range(1, maxSlices + 1); // Fallback nếu JSON bị thiếu
        }

        float angleStep = 360f / maxSlices;

        for (int i = 0; i < sliceCount; i++)
        {
            // Data-driven: Random dựa trên list cho phép của Level hiện tại
            int randomTypeIndex = Random.Range(0, availableTypes.Length);
            int selectedType = availableTypes[randomTypeIndex];

            // Kéo trực tiếp Component visual từ Pool (Zero GC)
            PizzaSliceVisual slice = ObjectPoolManager.Instance.GetPizzaSlice();
            
            // Fix Scale: Dùng false để slice thừa kế chính xác scale của đĩa, không bị Unity phóng to bù trừ
            slice.transform.SetParent(this.transform, false);
            slice.transform.localPosition = new Vector3(0, _sliceYOffset, 0); // Nâng cao miếng bánh
            slice.transform.localRotation = Quaternion.Euler(0, i * angleStep, 0);
            slice.transform.localScale = Vector3.one; // Ép lại scale chuẩn
            
            slice.SetVisual(selectedType);

            _slices[i] = slice;
        }
    }
}
