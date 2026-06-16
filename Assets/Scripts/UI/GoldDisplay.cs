using UnityEngine;
using TMPro;

public class GoldDisplay : MonoBehaviour
{
    private static readonly System.Collections.Generic.List<GoldDisplay> _activeDisplays = new System.Collections.Generic.List<GoldDisplay>();

    [SerializeField] private TMP_Text _goldText;

    private void Start()
    {
        if (_goldText == null) _goldText = GetComponent<TMP_Text>();
        UpdateGold();
    }

    private void OnEnable()
    {
        _activeDisplays.Add(this);
        UpdateGold();
    }

    private void OnDisable()
    {
        _activeDisplays.Remove(this);
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
        // Duyệt qua danh sách đã đăng ký thay vì dùng FindObjectsByType (Gây giật lag khi gọi nhiều)
        for (int i = 0; i < _activeDisplays.Count; i++)
        {
            if (_activeDisplays[i] != null)
            {
                _activeDisplays[i].UpdateGold();
            }
        }
    }
}
