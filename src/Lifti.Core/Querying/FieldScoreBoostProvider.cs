using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    /// <summary>
    /// The default implementation of the <see cref="IFieldScoreBoostProvider"/> interface.
    /// </summary>
    public class FieldScoreBoostProvider : IFieldScoreBoostProvider
    {
        private readonly Dictionary<byte, double> fieldBoosts;

        /// <summary>
        /// Constructs a new instance of the <see cref="FieldScoreBoostProvider"/> class.
        /// </summary>
        /// <param name="fieldLookup">
        /// The <see cref="IIndexedFieldLookup"/> to load the defined fields from.
        /// </param>
        public FieldScoreBoostProvider(IIndexedFieldLookup fieldLookup)
        {
            if (fieldLookup is null)
            {
                throw new ArgumentNullException(nameof(fieldLookup));
            }

            this.fieldBoosts = fieldLookup.AllFieldNames
                .Select(fieldName => fieldLookup.GetFieldInfo(fieldName))
                .ToDictionary(f => f.Id, f => f.ScoreBoost);

            this.fieldBoosts.Add(fieldLookup.DefaultField, 1D);
        }

        /// <inheritdoc />
        public double GetScoreBoost(byte fieldId)
        {
            if (!this.fieldBoosts.TryGetValue(fieldId, out var boost))
            {
                throw new LiftiException(ExceptionMessages.UnknownField, fieldId);
            }

            return boost;
        }
    }
}
