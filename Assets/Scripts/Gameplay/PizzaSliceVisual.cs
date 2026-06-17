using UnityEngine;

public class PizzaSliceVisual : MonoBehaviour
{
    [Tooltip("Kéo thả 6 model Pizza con vào mảng này theo thứ tự 1-6")]
    [SerializeField] private GameObject[] _pizzaModels;

    public int TypeIndex { get; private set; } // Thuộc tính để nhận diện loại pizza hiện tại

    public void SetVisual(int pizzaTypeIndex)
    {
        TypeIndex = pizzaTypeIndex;
        // Tuân thủ luật Zero GC: Không dùng Find() hay GetComponent()
        // Duyệt qua mảng và chỉ bật model được chọn
        for (int i = 0; i < _pizzaModels.Length; i++)
        {
            // Bật model nếu index trùng khớp, ngược lại thì tắt
            _pizzaModels[i].SetActive(i == pizzaTypeIndex);
        }
    }
}
