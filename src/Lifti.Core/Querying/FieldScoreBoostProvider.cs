using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    internal class FieldScoreBoostProvider : IFieldScoreBoostProvider
    {
        private Dictionary<byte, double> fieldBoosts;

        public FieldScoreBoostProvider(IIndexedFieldLookup fieldLookup)
        {
            this.fieldBoosts = fieldLookup.AllFieldNames
                .Select(fieldName => fieldLookup.GetFieldInfo(fieldName))
                .ToDictionary(f => f.Id, f => f.ScoreBoost);

            this.fieldBoosts.Add(fieldLookup.DefaultField, 1D);
        }

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
