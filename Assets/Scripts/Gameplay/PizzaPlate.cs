using UnityEngine;

public class PizzaPlate : MonoBehaviour
{
    [SerializeField] private float _spawnHeight = 0f;
    [SerializeField] private float _pickUpOffset = 0.5f;
    [Tooltip("Nâng miếng bánh lên trên mặt đĩa (chỉnh thành 1 nếu cần)")]
    [SerializeField] private float _sliceYOffset = 1f;

    private Vector3 _originalPosition;
    private Transform _originalParent;
    private PizzaSliceVisual[] _slices; // Mảng chứa các miếng theo index/góc quay

    public PizzaSliceVisual[] Slices => _slices; // Cho phép các Manager đọc dữ liệu miếng bánh trên đĩa
    public float SliceYOffset => _sliceYOffset;

    public void Initialize(Transform parentSlot)
    {
        _originalParent = parentSlot;
        transform.SetParent(parentSlot);
        transform.localPosition = new Vector3(0, _spawnHeight, 0); // Sinh cách khoảng y
        _originalPosition = transform.position;
    }

    public void PickUp()
    {
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
        // Trả đĩa về vị trí khay ban đầu
        transform.position = _originalPosition;
        transform.SetParent(_originalParent);
    }

    public void PlaceAt(Vector3 targetPos, Transform newParent)
    {
        // Đặt parent nhưng KHÔNG di chuyển tức thì (để Coroutine lo)
        transform.SetParent(newParent, true);
        _originalParent = newParent;
        _originalPosition = targetPos;
    }

    public System.Collections.IEnumerator AnimateToCell(Vector3 startPos, Vector3 endPos, float duration = 0.25f)
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
                ObjectPoolManager.Instance.ReturnPizzaSlice(_slices[i]);
                _slices[i] = null;
            }
        }
    }

    public System.Collections.Generic.List<int> GetAvailableTypes()
    {
        System.Collections.Generic.List<int> types = new System.Collections.Generic.List<int>();
        if (_slices == null) return types;
        foreach (var slice in _slices)
        {
            if (slice != null && !types.Contains(slice.TypeIndex))
            {
                types.Add(slice.TypeIndex);
            }
        }
        return types;
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
        System.Collections.Generic.Dictionary<int, int> counts = new System.Collections.Generic.Dictionary<int, int>();
        foreach (var slice in _slices)
        {
            if (slice != null)
            {
                if (!counts.ContainsKey(slice.TypeIndex)) counts[slice.TypeIndex] = 0;
                counts[slice.TypeIndex]++;
            }
        }
        int maxCount = 0;
        int majorityType = -1;
        foreach (var kvp in counts)
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
        System.Collections.Generic.Dictionary<int, int> counts = new System.Collections.Generic.Dictionary<int, int>();
        foreach (var slice in _slices)
        {
            if (slice != null && slice.TypeIndex != excludeType)
            {
                if (!counts.ContainsKey(slice.TypeIndex)) counts[slice.TypeIndex] = 0;
                counts[slice.TypeIndex]++;
            }
        }
        int minCount = int.MaxValue;
        int minorityType = -1;
        foreach (var kvp in counts)
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
                
                float angleStep = 360f / _slices.Length;
                slice.transform.localRotation = Quaternion.Euler(0, i * angleStep, 0);
                
                return true;
            }
        }
        return false;
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
