using UnityEngine;

[CreateAssetMenu(fileName = "ShopConfig", menuName = "Cheezy Savoround/Shop Config")]
public class ShopConfig : ScriptableObject
{
    [Header("Danh sách Skin Đĩa (Plate)")]
    public SkinData[] Skins;

    /// <summary>
    /// Tìm dữ liệu Skin theo ID. Trả về null nếu không thấy.
    /// </summary>
    public SkinData GetSkin(string id)
    {
        if (Skins == null) return null;
        for (int i = 0; i < Skins.Length; i++)
        {
            if (Skins[i].Id == id) return Skins[i];
        }
        return null;
    }
}

[System.Serializable]
public class SkinData
{
    public string Id;
    public string DisplayName;
    public int Price;
    
    [Tooltip("Kéo thả Texture (plate01.png, plate02.png...) vào đây")]
    public Texture2D Texture;
}
