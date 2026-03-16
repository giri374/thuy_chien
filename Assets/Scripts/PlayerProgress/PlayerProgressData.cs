using System;

[Serializable]
public class PlayerProgressData
{
    public int gold = 0;
    public int experience = 0;
    public int level = 1;
    public int totalWins = 0;
    public int totalLosses = 0;

    // Exp cần để lên level tiếp theo
    public int ExpToNextLevel = 100;
    // public int ExpToNextLevel => level * 100;
    public const int MaxLevel = 7; // Khai báo giới hạn

    public void AddExp (int amount)
    {
        if (level >= MaxLevel)
        {
            return; // Đã đạt tối đa thì không nhận thêm exp
        }

        experience += amount;

        // Kiểm tra: Exp đủ và chưa chạm ngưỡng MaxLevel
        while (experience >= ExpToNextLevel && level < MaxLevel)
        {
            experience -= ExpToNextLevel;
            level++;

            if (level == MaxLevel)
            {
                experience = 0; // Reset exp dư khi đạt đỉnh (tùy chọn)
                break;
            }
        }
    }

    public void AddGold (int amount)
    {
        gold = Math.Max(0, gold + amount);
    }
}