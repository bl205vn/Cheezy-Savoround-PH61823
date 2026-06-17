using UnityEditor;
using UnityEngine;
using System.IO;

public class LevelGenerator
{
    #region Constants
    private const int TOTAL_LEVELS = 30;
    private const int DEFAULT_HOLD_SLOT_COUNT = 3;
    private const int MAX_SLICES_PER_PLATE = 6;

    // Xác suất sinh đĩa có N miếng ở đầu game (dễ: nhiều đĩa 3, 4 miếng)
    private static readonly float[] EASY_PROBABILITIES = { 2f, 8f, 30f, 35f, 18f, 7f };
    
    // Xác suất sinh đĩa có N miếng ở cuối game (khó: nhiều đĩa 1, 2 miếng)
    private static readonly float[] HARD_PROBABILITIES = { 25f, 28f, 20f, 14f, 8f, 5f };
    #endregion

    [MenuItem("Tools/Generate 30 Levels")]
    public static void Generate()
    {
        string folderPath = Path.Combine(Application.dataPath, "Resources", "Levels");
        
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        
        for (int levelIndex = 1; levelIndex <= TOTAL_LEVELS; levelIndex++)
        {
            // --- 1. TÍNH TOÁN KÍCH THƯỚC LƯỚI ---
            // Tăng dần kích thước lưới từ 2x2 lên 4x6 theo tiến trình game
            int currentGridWidth = 2;
            int currentGridHeight = 2;

            if (levelIndex <= 5) { currentGridWidth = 2; currentGridHeight = 3; }
            else if (levelIndex <= 10) { currentGridWidth = 3; currentGridHeight = 3; }
            else if (levelIndex <= 15) { currentGridWidth = 3; currentGridHeight = 4; }
            else if (levelIndex <= 20) { currentGridWidth = 4; currentGridHeight = 4; }
            else if (levelIndex <= 25) { currentGridWidth = 4; currentGridHeight = 5; }
            else { currentGridWidth = 4; currentGridHeight = 6; }
            
            // --- 2. TÍNH TOÁN SỐ LOẠI BÁNH ---
            // Level càng cao càng nhiều loại bánh để tăng độ khó (tối đa 6 loại)
            int currentTypeCount = Mathf.Clamp(2 + (levelIndex / 5), 2, MAX_SLICES_PER_PLATE);
            int[] availablePizzaTypes = new int[currentTypeCount];
            
            for (int typeIndex = 0; typeIndex < currentTypeCount; typeIndex++)
            {
                availablePizzaTypes[typeIndex] = typeIndex;
            }
            
            // --- 3. TÍNH TOÁN TỈ LỆ SINH MẢNH BÁNH ---
            // Nội suy tuyến tính (Lerp) từ bộ tỉ lệ dễ (Easy) sang khó (Hard) theo mốc level hiện tại
            float difficultyLerpFactor = (levelIndex - 1) / (float)(TOTAL_LEVELS - 1); 

            float[] sliceCountProbabilities = new float[MAX_SLICES_PER_PLATE];
            for (int sliceIndex = 0; sliceIndex < MAX_SLICES_PER_PLATE; sliceIndex++)
            {
                sliceCountProbabilities[sliceIndex] = Mathf.Lerp(
                    EASY_PROBABILITIES[sliceIndex], 
                    HARD_PROBABILITIES[sliceIndex], 
                    difficultyLerpFactor
                );
            }

            // --- 4. KHỞI TẠO VÀ LƯU LEVEL DATA ---
            LevelData newLevelData = new LevelData 
            { 
                levelId = levelIndex, 
                gridWidth = currentGridWidth, 
                gridHeight = currentGridHeight,
                holdSlotCount = DEFAULT_HOLD_SLOT_COUNT,
                maxSlices = MAX_SLICES_PER_PLATE,
                availablePizzaTypes = availablePizzaTypes,
                sliceCountProbabilities = sliceCountProbabilities
            };

            string jsonContent = JsonUtility.ToJson(newLevelData, true);
            string filePath = Path.Combine(folderPath, $"level_{levelIndex}.json");
            
            File.WriteAllText(filePath, jsonContent);
        }
        
        AssetDatabase.Refresh();
        Debug.Log($"✅ Đã sinh {TOTAL_LEVELS} file JSON trong Resources/Levels/ với tỷ lệ phân bổ độ khó hoàn chỉnh!");
    }
}
