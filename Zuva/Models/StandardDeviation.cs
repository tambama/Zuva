namespace Zuva.Models;

  /// <summary>
    /// Calculates standard deviation levels based on Fibonacci retracement points
    /// </summary>
    public class StandardDeviation
    {
        private double _range;

        /// <summary>
        /// Creates a new StandardDeviation instance using two price points
        /// </summary>
        /// <param name="index">Index if the swing point zero</param>
        /// <param name="zero">The first price point (usually high)</param>
        /// <param name="one">The second price point (usually low)</param>
        /// <param name="date">The time of swing point zero</param>
        public StandardDeviation(int index, double zero, double one, DateTime date)
        {
            Index = index;
            Zero = zero;
            One = one;
            OneTime = date;
            _range = Math.Abs(zero - one);
            
            // Calculate the standard deviation values
            CalculateDeviations();
        }

        /// <summary>
        /// The first price point (usually high)
        /// </summary>
        public double Zero { get; set; }
        
        /// <summary>
        /// The second price point (usually low)
        /// </summary>
        public double One { get; set; }
        
        /// <summary>
        /// -2 standard deviations from the Fibonacci retracement
        /// </summary>
        public double MinusTwo { get; set; }
        
        /// <summary>
        /// -4 standard deviations from the Fibonacci retracement
        /// </summary>
        public double MinusFour { get; set; }

        public DateTime OneTime { get; set; }
        
        public int Index { get; set; }

        /// <summary>
        /// Calculates the deviation levels based on the range
        /// </summary>
        private void CalculateDeviations()
        {
            // For Fibonacci-based standard deviations, traditionally:
            // -2 SD is calculated as extending the range by 1.618 (the golden ratio) from point One
            // -4 SD is calculated as extending the range by 2.618 (square of the golden ratio) from point One
            
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
        
        /// <summary>
        /// Recalculates the deviation levels if Zero or One points change
        /// </summary>
        public void Update()
        {
            _range = Math.Abs(Zero - One);
            CalculateDeviations();
        }
    }