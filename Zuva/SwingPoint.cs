namespace cAlgo;

/// <summary>
/// Represents a detected swing point with relevant data
/// </summary>
public class SwingPoint
{
    public bool HasSwingHigh { get; set; }
    public bool HasSwingLow { get; set; }
    public double SwingHighValue { get; set; }
    public double SwingLowValue { get; set; }
    public bool ClearPreviousHigh { get; set; }
    public bool ClearPreviousLow { get; set; }
    public int PreviousSwingHighIndex { get; set; }
    public int PreviousSwingLowIndex { get; set; }
}