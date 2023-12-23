using System;

namespace Lifti.Tokenization.Objects
{
    /// <summary>
    /// Allows for the properties of an indexed object to influence how it is scored relative to other objects.
    /// </summary>
    /// <typeparam name="TObject">The type of the object</typeparam>
    public class ObjectScoreBoostBuilder<TObject>
    {
        internal Func<TObject, double?>? MagnitudeProvider { get; private set; }
        internal double MagnitudeMultiplier { get; private set; }
        internal Func<TObject, DateTime?>? FreshnessProvider { get; private set; }
        internal double FreshnessMultiplier { get; private set; }

        /// <summary>
        /// Boosts results based on the freshness of the document extracted from the object. 
        /// For example, if a multiplier of 3 is specified, then the score of the newest document will be 3 times higher than 
        /// the oldest.
        /// </summary>
        /// <param name="freshnessProvider">
        /// The delegate capable of reading the freshness value from the object.
        /// </param>
        /// <param name="multiplier">
        /// The multiplier to apply to the score of the object's document based on its freshness. Must be greater than 1.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if the multiplier is less than or equal to 1.
        /// </exception>
        public ObjectScoreBoostBuilder<TObject> Freshness(Func<TObject, DateTime?> freshnessProvider, double multiplier)
        {
            if (multiplier <= 1)
            {
                throw new ArgumentException(ExceptionMessages.MultiplierValueMustBeGreaterThanOne);
            }

            this.FreshnessProvider = freshnessProvider;
            this.FreshnessMultiplier = multiplier;
            return this;
        }

        /// <summary>
        /// Boosts results based on the magnitude of the document extracted from the object. 
        /// For example, if a multiplier of 3 is specified, then the score of the document with the highest magnitude will be 3 
        /// times higher than the one with the lowest magnitude.
        /// </summary>
        /// <param name="magnitudeProvider">
        /// The delegate capable of reading the magnitude value from the object.
        /// </param>
        /// <param name="multiplier">
        /// The multiplier to apply to the score of the object's document based on its magnitude. Must be greater than 1.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if the multiplier is less than or equal to 1.
        /// </exception>
        public ObjectScoreBoostBuilder<TObject> Magnitude(Func<TObject, double?> magnitudeProvider, double multiplier)
        {
            if (multiplier <= 1)
            {
                throw new ArgumentException(ExceptionMessages.MultiplierValueMustBeGreaterThanOne);
            }

            this.MagnitudeProvider = magnitudeProvider;
            this.MagnitudeMultiplier = multiplier;
            return this;
        }

        internal ObjectScoreBoostOptions<TObject> Build()
        {
            return new ObjectScoreBoostOptions<TObject>(
                this.MagnitudeMultiplier,
                this.MagnitudeProvider,
                this.FreshnessMultiplier,
                this.FreshnessProvider);
        }
    }
}
