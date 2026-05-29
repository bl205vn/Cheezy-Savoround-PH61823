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
            // Cấu trúc tạm: 1-10: 3x3 | 11-20: 3x4 | 21-30: 4x4
            int w = i <= 10 ? 3 : (i <= 20 ? 3 : 4);
            int h = i <= 10 ? 3 : (i <= 20 ? 4 : 4);
            
            LevelData data = new LevelData { 
                levelId = i, 
                gridWidth = w, 
                gridHeight = h,
                holdSlotCount = DEFAULT_HOLD_SLOT_COUNT 
            };
            string json = JsonUtility.ToJson(data, true);
            
            string filePath = Path.Combine(folderPath, $"level_{i}.json");
            File.WriteAllText(filePath, json);
        }
        
        AssetDatabase.Refresh();
        Debug.Log("✅ Đã sinh 30 file JSON trong Resources/Levels/");
    }
}
