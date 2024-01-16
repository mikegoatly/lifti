using Lifti.Querying;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tests.Fakes
{
    internal class FakeFieldScoreBoostProvider : IFieldScoreBoostProvider
    {
        private Dictionary<byte, double> boostOverrides;

        public FakeFieldScoreBoostProvider(params (byte fieldId, double scoreBoost)[] boostOverrides)
        {
            this.boostOverrides = boostOverrides.ToDictionary(x => x.fieldId, x => x.scoreBoost);
        }

        public double GetScoreBoost(byte fieldId)
        {
            if (boostOverrides.TryGetValue(fieldId, out var boost))
            {
                return boost;
            }

            return 1D;
        }
    }
}
