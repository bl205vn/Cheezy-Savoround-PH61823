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
            PizzaPlate plate = plateObj.GetComponent<PizzaPlate>();
            if (plate == null)
            {
                plate = plateObj.AddComponent<PizzaPlate>();
            }
            plate.Initialize(anchor.transform);

            _slotAnchors.Add(anchor);
        }
        
        Debug.Log($"[TrayManager] Đã sinh {slotCount} đĩa pizza trên khay.");
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
