using System;

namespace Lifti.Tokenization.Objects
{
    /// <summary>
    /// Allows for the properties of an indexed object to influence how it is scored relative to other objects.
    /// </summary>
    /// <typeparam name="TItem">The type of the item</typeparam>
    public class ObjectScoreBoostOptions<TItem>
    {
        internal Func<TItem, double>? MagnitudeProvider { get; private set; }
        internal double MagnitudeMultiplier { get; private set; }
        internal Func<TItem, DateTime>? FreshnessProvider { get; private set; }
        internal double FreshnessMultiplier { get; private set; }

        /// <summary>
        /// Boosts results based on the freshness of the item. For example, if a multiplier of 3 is specified, then the score of 
        /// the newest item will be 3 times higher than the oldest item.
        /// </summary>
        /// <param name="freshnessProvider">
        /// The delegate capable of reading the freshness value from the item.
        /// </param>
        /// <param name="multiplier">
        /// The multiplier to apply to the score of the item based on its freshness.
        /// </param>
        public ObjectScoreBoostOptions<TItem> Freshness(Func<TItem, DateTime> freshnessProvider, double multiplier)
        {
            this.FreshnessProvider = freshnessProvider;
            this.FreshnessMultiplier = multiplier;
            return this;
        }

        /// <summary>
        /// Boosts results based on the magnitude of the item. For example, if a multiplier of 3 is specified, then the score 
        /// of the item with the highest magnitude will be 3 
        /// times higher than the item with the lowest magnitude.
        /// </summary>
        /// <param name="magnitudeProvider">
        /// The delegate capable of reading the magnitude value from the item.
        /// </param>
        /// <param name="multiplier">
        /// The multiplier to apply to the score of the item based on its magnitude.
        /// </param>
        public ObjectScoreBoostOptions<TItem> Magnitude(Func<TItem, double> magnitudeProvider, double multiplier)
        {
            this.MagnitudeProvider = magnitudeProvider;
            this.MagnitudeMultiplier = multiplier;
            return this;
        }
    }
}
