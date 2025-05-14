namespace Zuva.Models;

public class StandardDeviation
{
    private double _range;

    /// <summary>
    /// Creates a new StandardDeviation instance using two price points
    /// </summary>
    /// <param name="index">Index of the swing point zero</param>
    /// <param name="zero">The first price point (usually high)</param>
    /// <param name="one">The second price point (usually low)</param>
    /// <param name="date">The time of swing point zero</param>
    /// <param name="direction">Direction of the standard deviation</param>
    public StandardDeviation(int index, double zero, double one, DateTime date, Direction direction)
    {
        Index = index;
        Zero = zero;
        One = one;
        OneTime = date;
        Direction = direction;
        AllSwept = false;
        _range = Math.Abs(zero - one);
        
        // Calculate the standard deviation values
        CalculateDeviations();
    }

    public double Zero { get; set; }
    public double One { get; set; }
    public double MinusTwo { get; set; }
    public double MinusFour { get; set; }
    public DateTime OneTime { get; set; }
    public int Index { get; set; }
    public Direction Direction { get; set; }
    public bool AllSwept { get; set; }

    private void CalculateDeviations()
    {
        // Determine direction (if Zero > One, we're going down, otherwise up)
        bool isDowntrend = Zero > One;
        
        if (isDowntrend)
        {
            // For downtrends, extensions continue downward
            MinusTwo = One - (2 * _range);
            MinusFour = One - (4 * _range);
        }
        else
        {
            // For uptrends, extensions continue upward
            MinusTwo = One + (2 * _range);
            MinusFour = One + (4 * _range);
        }
    }
    
    public void Update()
    {
        _range = Math.Abs(Zero - One);
        CalculateDeviations();
    }
    
    /// <summary>
    /// Mark a standard deviation level as swept
    /// </summary>
    /// <param name="isMinusTwo">True if MinusTwo level was swept, false if MinusFour</param>
    public void MarkLevelAsSwept(bool isMinusTwo)
    {
        if (isMinusTwo)
        {
            MinusTwo = 0;
        }
        else
        {
            MinusFour = 0;
        }

        // Check if all levels are swept
        if (MinusTwo == 0 && MinusFour == 0)
        {
            AllSwept = true;
        }
    }
}