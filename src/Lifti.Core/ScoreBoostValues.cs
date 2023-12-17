using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    internal class DoubleScoreBoostValues : ScoreBoostValues<double>
    {
        protected override double ValueAsDouble(double value)
        {
            return value;
        }
    }

    internal class DateTimeScoreBoostValues : ScoreBoostValues<DateTime>
    {
        protected override double ValueAsDouble(DateTime value)
        {
            return value.Ticks;
        }
    }

    /// <summary>
    /// Maintains the set of values that have been encountered for a given object type, allowing for
    /// a normalized score to be calculated for any given value.
    /// </summary>
    internal abstract class ScoreBoostValues<T>
        where T : struct, IComparable<T>
    {
        private readonly Dictionary<T, int> valueRefCount = [];

        /// <summary>
        /// Calculating a normalized score is calculated as (value - min) / (max - min). We can pre-calculate two parts of this:
        /// * A value that can be applied to the score to adjust it to the baseline. This is derived from the minimum value in the set of values.
        /// * The denominator of the normalization calculation.
        /// </summary>
        private (double baselineAdjustment, double normalizationDenominator) normalizationPrecalculations;
        private bool normalizationPrecalculationsValid;

        protected ScoreBoostValues()
        {
        }

        public T Minimum { get; private set; }
        public T Maximum { get; private set; }

        public void Add(T value)
        {
            if (this.valueRefCount.Count == 0)
            {
                // This is the first value, so set the min/max to this value
                this.Minimum = value;
                this.Maximum = value;

                // Add the ref count for the value
                this.valueRefCount.Add(value, 1);
            }
            else
            {
                // Adjust the ref count for the value
                if (this.valueRefCount.TryGetValue(value, out var count))
                {
                    this.valueRefCount[value] = count + 1;
                }
                else
                {
                    this.valueRefCount.Add(value, 1);

                    // Adjust the min/max values if necessary
                    if (this.Minimum.CompareTo(value) > 0)
                    {
                        this.Minimum = value;
                        var minValue = this.ValueAsDouble(value);
                        this.ResetNormalizationPrecalculations();
                    }
                    else if (this.Maximum.CompareTo(value) < 0)
                    {
                        this.Maximum = value;
                        this.ResetNormalizationPrecalculations();
                    }
                }
            }
        }

        private void ResetNormalizationPrecalculations()
        {
            this.normalizationPrecalculationsValid = false;
        }

        public void Remove(T value)
        {
            // Adjust the ref count for the value
            if (!this.valueRefCount.TryGetValue(value, out var count))
            {
                throw new LiftiException(ExceptionMessages.UnexpectedScoreBoostValueRemoval);
            }

            if (count == 1)
            {
                this.valueRefCount.Remove(value);

                if (this.valueRefCount.Count > 0)
                {
                    // Because the value has been removed entirely, if it was the current minimum or maximum, we need to recalculate
                    if (this.Minimum.CompareTo(value) >= 0)
                    {
                        this.Minimum = this.valueRefCount.Keys.Min();
                        this.ResetNormalizationPrecalculations();
                    }

                    if (this.Maximum.CompareTo(value) <= 0)
                    {
                        this.Maximum = this.valueRefCount.Keys.Max();
                        this.ResetNormalizationPrecalculations();
                    }
                }
            }
            else
            {
                this.valueRefCount[value] = count - 1;
            }
        }

        protected abstract double ValueAsDouble(T value);

        internal double CalculateBoost(double multiplier, T? value)
        {
            if (value is null)
            {
                // No value, so no boost
                return 0D;
            }

            var baseline = this.ValueAsDouble((T)value);

            var (baselineAdjustment, normalizationDenominator) = this.GetNormalizationPrecalculations();

            if (normalizationDenominator == 0D)
            {
                // In this case the max and min are the same, so just return the multiplier
                return multiplier;
            }

            // Standard normalization between 0 and 1 would be
            // (value - min) / (max - min)
            // But we want the value to range between 1..multiplier, so the formula we're using here is:
            // 1 + ((value - min) / (max - min)) * (multiplier - 1)
            // The multiplier values are guarded at index creation time to ensure they are greater than 1
            return 1D + ((baseline - baselineAdjustment) / normalizationDenominator * (multiplier - 1D));
        }

        private (double baselineAdjustment, double normalizationDenominator) GetNormalizationPrecalculations()
        {
            if (this.normalizationPrecalculationsValid == false)
            {
                var minValue = this.ValueAsDouble(this.Minimum);
                this.normalizationPrecalculations = (minValue, this.ValueAsDouble(this.Maximum) - minValue);
            }

            return this.normalizationPrecalculations;
        }
    }
}
