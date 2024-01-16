using System;

namespace Lifti.Tokenization.Objects
{
    /// <summary>
    /// Provides the configured options for boosting the score of an object based on its magnitude and freshness.
    /// </summary>
    /// <param name="magnitudeMultiplier">
    /// The multiplier to apply to the score of the object's document, based on its magnitude.
    /// </param>
    /// <param name="freshnessMultiplier">
    /// The multiplier to apply to the score of the object's document, based on its freshness.
    /// </param>
    public abstract class ObjectScoreBoostOptions(double magnitudeMultiplier, double freshnessMultiplier)
    {
        /// <summary>
        /// Gets the multiplier to apply to the score of the object's document, based on its magnitude.
        /// </summary>
        public double MagnitudeMultiplier { get; } = magnitudeMultiplier;

        /// <summary>
        /// Gets the multiplier to apply to the score of the object's document, based on its freshness.
        /// </summary>
        public double FreshnessMultiplier { get; } = freshnessMultiplier;
    }

    /// <summary>
    /// Provides the configured options for boosting the score of an object based on its magnitude and freshness.
    /// </summary>
    /// <typeparam name="TObject">The type of the object.</typeparam>
    /// <param name="magnitudeMultiplier">
    /// The multiplier to apply to the score of the object's document, based on its magnitude.
    /// </param>
    /// <param name="magnitudeProvider">
    /// The delegate capable of reading the magnitude value from the object.
    /// </param>
    /// <param name="freshnessMultiplier">
    /// The multiplier to apply to the score of the object's document, based on its freshness.
    /// </param>
    /// <param name="freshnessProvider">
    /// The delegate capable of reading the freshness value from the object.
    /// </param>
    public class ObjectScoreBoostOptions<TObject>(
        double magnitudeMultiplier,
        Func<TObject, double?>? magnitudeProvider,
        double freshnessMultiplier,
        Func<TObject, DateTime?>? freshnessProvider)
        : ObjectScoreBoostOptions(magnitudeMultiplier, freshnessMultiplier)
    {
        /// <summary>
        /// Gets the delegate capable of reading the magnitude value from the object.
        /// </summary>
        public Func<TObject, double?>? MagnitudeProvider { get; } = magnitudeProvider;

        /// <summary>
        /// Gets the delegate capable of reading the freshness value from the object.
        /// </summary>
        public Func<TObject, DateTime?>? FreshnessProvider { get; } = freshnessProvider;

        internal static ObjectScoreBoostOptions<TObject> Empty()
        {
            return new ObjectScoreBoostOptions<TObject>(0D, null, 0D, null);
        }
    }
}
