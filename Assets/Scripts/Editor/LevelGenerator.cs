using UnityEditor;
using UnityEngine;
using System.IO;

public class LevelGenerator
{
    [MenuItem("Tools/Generate 30 Levels")]
    public static void Generate()
    {
        const int DEFAULT_HOLD_SLOT_COUNT = 3;
        string folderPath = Path.Combine(Application.dataPath, "Resources", "Levels");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        
        for (int i = 1; i <= 30; i++)
        {
            // Tăng dần kích thước lưới từ 2x2 lên 4x6
            int w = 2, h = 2;
            if (i <= 5) { w = 2; h = 3; }
            else if (i <= 10) { w = 3; h = 3; }
            else if (i <= 15) { w = 3; h = 4; }
            else if (i <= 20) { w = 4; h = 4; }
            else if (i <= 25) { w = 4; h = 5; }
            else { w = 4; h = 6; }
            
            // Số lượng loại bánh: level càng cao càng nhiều loại (tối đa 6 loại)
            // Ví dụ: Level 1-5 có 2 loại, 6-10 có 3 loại...
            int typeCount = Mathf.Clamp(2 + (i / 5), 2, 6);
            int[] availableTypes = new int[typeCount];
            for (int t = 0; t < typeCount; t++)
            {
                availableTypes[t] = t;
            }
            
            // Xác suất số lượng miếng bánh xuất hiện trên 1 đĩa (từ 1 đến 6 miếng)
            // Level thấp dễ có đĩa đầy hơn (nhiều miếng), level cao hay bị lẻ miếng (ít miếng)
            float[] probs = new float[6];
            if (i <= 5) 
                probs = new float[] { 5, 10, 40, 30, 10, 5 }; // Tập trung sinh đĩa có 3, 4 miếng
            else if (i <= 15) 
                probs = new float[] { 10, 20, 30, 25, 10, 5 }; // Phân tán hơn, khó dần
            else 
                probs = new float[] { 20, 25, 20, 15, 10, 10 }; // Hay ra đĩa 1, 2 miếng, rất khó

            LevelData data = new LevelData { 
                levelId = i, 
                gridWidth = w, 
                gridHeight = h,
                holdSlotCount = DEFAULT_HOLD_SLOT_COUNT,
                maxSlices = 6,
                availablePizzaTypes = availableTypes,
                sliceCountProbabilities = probs
            };
            string json = JsonUtility.ToJson(data, true);
            
            string filePath = Path.Combine(folderPath, $"level_{i}.json");
            File.WriteAllText(filePath, json);
        }
        
        AssetDatabase.Refresh();
        Debug.Log("✅ Đã sinh 30 file JSON trong Resources/Levels/ với tỷ lệ phân bổ độ khó hoàn chỉnh!");
    }
}
