namespace Zuva.Models;

public class Level
{
    public Level(LevelType levelType, double low, double high, DateTime lowTime, DateTime highTime, DateTime? midTime = null, Direction direction = Direction.Up, int index = 0, int indexHigh = 0, int indexLow = 0, Zone zone = Zone.Equilibrium, int score = 1, DateTime? stretchTo = null, bool isConfirmed = true, double? entry = 0)
    {
        LevelType = levelType;
        Low = low;
        High = high;
        LowTime = lowTime;
        HighTime = highTime;
        MidTime = midTime ?? highTime;
        Direction = direction;
        Index = index;
        IndexHigh = indexHigh;
        IndexLow = indexLow;
        Zone = zone;
        Score = score;
        StretchTo = stretchTo;
        IsConfirmed = isConfirmed;
        Entry = entry;
    }

    public Zone Zone { get; set; }
    public LevelType LevelType { get; set; }
    public Direction Direction { get; set; }
    public double Low { get; set; }
    public DateTime LowTime { get; set; }
    public double High { get; set; }
    public DateTime HighTime { get; set; }
    public double Mid => (High + Low) / 2;
    public DateTime MidTime { get; set; }
    public int Index { get; set; }
    public int IndexHigh { get; set; }
    public int IndexLow { get; set; }
    public int Score { get; set; }
    public bool Activated { get; set; }
    public bool IsInverted { get; set; }
    public int PassCount { get; set; }
    public bool IsConfirmed { get; set; }
    public DateTime? StretchTo { get; set; }
    public double? Entry { get; set; }
}