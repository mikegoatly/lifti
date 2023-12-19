using System;

namespace Lifti.Tokenization.Objects
{
    /// <summary>
    /// Provides the configured options for boosting the score of an object based on its magnitude and freshness.
    /// </summary>
    /// <param name="magnitudeMultiplier">
    /// The multiplier to apply to the score of the item based on its magnitude.
    /// </param>
    /// <param name="freshnessMultiplier">
    /// The multiplier to apply to the score of the item based on its freshness.
    /// </param>
    public abstract class ObjectScoreBoostOptions(double magnitudeMultiplier, double freshnessMultiplier)
    {
        /// <summary>
        /// Gets the multiplier to apply to the score of the item based on its magnitude.
        /// </summary>
        public double MagnitudeMultiplier { get; } = magnitudeMultiplier;

        /// <summary>
        /// Gets the multiplier to apply to the score of the item based on its freshness.
        /// </summary>
        public double FreshnessMultiplier { get; } = freshnessMultiplier;
    }

    /// <summary>
    /// Provides the configured options for boosting the score of an object based on its magnitude and freshness.
    /// </summary>
    /// <typeparam name="TItem">The type of the object.</typeparam>
    /// <param name="magnitudeMultiplier">
    /// The multiplier to apply to the score of the item based on its magnitude.
    /// </param>
    /// <param name="magnitudeProvider">
    /// The delegate capable of reading the magnitude value from the item.
    /// </param>
    /// <param name="freshnessMultiplier">
    /// The multiplier to apply to the score of the item based on its freshness.
    /// </param>
    /// <param name="freshnessProvider">
    /// The delegate capable of reading the freshness value from the item.
    /// </param>
    public class ObjectScoreBoostOptions<TItem>(
        double magnitudeMultiplier,
        Func<TItem, double?>? magnitudeProvider,
        double freshnessMultiplier,
        Func<TItem, DateTime?>? freshnessProvider)
        : ObjectScoreBoostOptions(magnitudeMultiplier, freshnessMultiplier)
    {
        /// <summary>
        /// Gets the delegate capable of reading the magnitude value from the item.
        /// </summary>
        public Func<TItem, double?>? MagnitudeProvider { get; } = magnitudeProvider;

        /// <summary>
        /// Gets the delegate capable of reading the freshness value from the item.
        /// </summary>
        public Func<TItem, DateTime?>? FreshnessProvider { get; } = freshnessProvider;

        internal static ObjectScoreBoostOptions<TItem> Empty()
        {
            return new ObjectScoreBoostOptions<TItem>(0D, null, 0D, null);
        }
    }
}
