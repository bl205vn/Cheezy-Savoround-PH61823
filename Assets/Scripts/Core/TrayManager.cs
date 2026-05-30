using System.Collections.Generic;
using UnityEngine;

public class TrayManager : MonoBehaviour
{
    [SerializeField] private float _slotSpacing; // Khoảng cách giữa các slot
    [SerializeField] private GameObject _pizzaPlatePrefab; // Prefab đĩa pizza

    // Lưu trữ các slot anchor (empty GO) để quản lý vòng đời
    private List<GameObject> _slotAnchors = new List<GameObject>();

    public void GenerateTray(int slotCount)
    {
        ClearTray();

        if (_pizzaPlatePrefab == null)
        {
            Debug.LogError("[TrayManager] _pizzaPlatePrefab chưa được gán!");
            return;
        }

        // Tính toán offset để căn giữa
        float offsetX = (slotCount - 1) * _slotSpacing * 0.5f;

        for (int i = 0; i < slotCount; i++)
        {
            Vector3 localPos = new Vector3(i * _slotSpacing - offsetX, 0, 0);
            Vector3 worldPos = transform.position + localPos;

            // Tạo empty GO làm điểm neo (thay thế slot prefab cũ)
            GameObject anchor = new GameObject($"TraySlot_{i}");
            anchor.transform.SetParent(transform);
            anchor.transform.position = worldPos;

            // Sinh đĩa pizza vào tâm anchor
            GameObject plateObj = Instantiate(_pizzaPlatePrefab, worldPos, Quaternion.identity, anchor.transform);
            
            // Ép scale đĩa pizza theo kích thước slot
            FitPlateToSlot(plateObj);

            PizzaPlate plate = plateObj.GetComponent<PizzaPlate>();
            if (plate == null)
            {
                plate = plateObj.AddComponent<PizzaPlate>();
            }
            plate.Initialize(anchor.transform);
            plate.GenerateRandomSlices(); // Sinh bánh trực tiếp lên đĩa vừa tạo

            _slotAnchors.Add(anchor);
        }
        
        Debug.Log($"[TrayManager] Đã sinh {slotCount} đĩa pizza trên khay.");
    }

    /// <summary>
    /// Ép scale prefab vào đúng kích thước 1 slot dựa trên Renderer bounds.
    /// </summary>
    private void FitPlateToSlot(GameObject plateObj)
    {
        Renderer rend = plateObj.GetComponentInChildren<Renderer>();
        if (rend == null) return;

        Vector3 currentSize = rend.bounds.size;
        
        // Chỉ scale theo trục X và Z (mặt phẳng ngang), giữ nguyên tỷ lệ Y
        float scaleX = (currentSize.x > 0.001f) ? (_slotSpacing / currentSize.x) : 1f;
        float scaleZ = (currentSize.z > 0.001f) ? (_slotSpacing / currentSize.z) : 1f;
        
        // Dùng scale nhỏ nhất để giữ tỷ lệ, khít hoàn toàn
        float uniformScale = Mathf.Min(scaleX, scaleZ);
        
        plateObj.transform.localScale = plateObj.transform.localScale * uniformScale;
    }

    private void ClearTray()
    {
        foreach (var anchor in _slotAnchors)
        {
            if (anchor != null)
            {
                Destroy(anchor);
            }
        }
        _slotAnchors.Clear();
    }

#if UNITY_EDITOR
    public void DrawGizmos(int slotCount)
    {
        Gizmos.color = Color.cyan;
        float offsetX = (slotCount - 1) * _slotSpacing * 0.5f;

        for (int i = 0; i < slotCount; i++)
        {
            Vector3 localPos = new Vector3(i * _slotSpacing - offsetX, 0, 0);
            Vector3 worldPos = transform.position + localPos;
            
            // Vẽ khung vuông phẳng (2D trên mặt phẳng XZ)
            Vector3 size = new Vector3(_slotSpacing, 0f, _slotSpacing); 
            Gizmos.DrawWireCube(worldPos, size);
        }
    }
#endif
}
