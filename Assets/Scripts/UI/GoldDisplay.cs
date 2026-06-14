using UnityEngine;
using TMPro;

public class GoldDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text _goldText;

    private void Start()
    {
        if (_goldText == null) _goldText = GetComponent<TMP_Text>();
        UpdateGold();
    }

    private void OnEnable()
    {
        UpdateGold();
    }

    public void UpdateGold()
    {
        if (_goldText != null && SaveLoadManager.Data != null)
        {
            _goldText.SetText("{0}", SaveLoadManager.Data.Gold);
        }
    }

    // Hàm tiện lợi để update tất cả GoldDisplay trên Scene (ví dụ khi mua Boost/Skin)
    public static void UpdateAll()
    {
        var displays = FindObjectsByType<GoldDisplay>(FindObjectsSortMode.None);
        foreach (var d in displays)
        {
            if (d != null) d.UpdateGold();
        }
    }
}
