using UnityEngine;

public enum PizzaType
{
    Pho_mai,
    Xuc_xich,
    Mi_tom,
    Bun_dau_mam_tom
}

public class PizzaPlate : MonoBehaviour
{
    [Tooltip("Dùng tạm để test thuật toán tuần 1")]
    [SerializeField] private PizzaType _type = PizzaType.Pho_mai;
    public PizzaType Type => _type;
    [SerializeField] private float _spawnHeight = 0f;
    [SerializeField] private float _pickUpOffset = 0.5f;

    private Vector3 _originalPosition;
    private Transform _originalParent;
    private PizzaSliceVisual[] _slices; // Mảng chứa các miếng theo index/góc quay

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
        // Đặt đĩa vào ô lưới
        transform.position = targetPos;
        transform.SetParent(newParent);
        _originalParent = newParent;
        _originalPosition = targetPos;
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

        // Sinh ngẫu nhiên số lượng miếng trên đĩa này
        int sliceCount = Random.Range(1, maxSlices + 1); 
        float angleStep = 360f / maxSlices;

        for (int i = 0; i < sliceCount; i++)
        {
            // Data-driven: Random dựa trên list cho phép của Level hiện tại
            int randomTypeIndex = Random.Range(0, availableTypes.Length);
            int selectedType = availableTypes[randomTypeIndex];

            // Kéo trực tiếp Component visual từ Pool (Zero GC)
            PizzaSliceVisual slice = ObjectPoolManager.Instance.GetPizzaSlice();
            slice.transform.SetParent(this.transform);
            slice.transform.localPosition = Vector3.zero;
            
            // Toán học thay thế GameObject neo: Quay góc dứt khoát
            slice.transform.localRotation = Quaternion.Euler(0, i * angleStep, 0);
            
            slice.SetVisual(selectedType);

            _slices[i] = slice;
        }
    }
}
