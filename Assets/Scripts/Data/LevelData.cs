using System;

[Serializable]
public class LevelData
{
    public int levelId;
    public int gridWidth;
    public int gridHeight;
    public int holdSlotCount;
    public int maxSlices = 6;
    public int[] availablePizzaTypes; // Chứa danh sách ID các loại pizza xuất hiện ở level này (ví dụ: [0, 1] cho màn 1)
}
