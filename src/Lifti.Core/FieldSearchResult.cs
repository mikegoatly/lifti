using System.Collections.Generic;

namespace Lifti
{
    public class FieldSearchResult
    {
        public FieldSearchResult(string foundIn, double score, IReadOnlyList<WordLocation> locations)
        {
            this.FoundIn = foundIn;
            this.Score = score;
            this.Locations = locations;
        }

        public string FoundIn { get; }
        public double Score { get; }
        public IReadOnlyList<WordLocation> Locations { get; }

        public override string ToString()
        {
            return $"{FoundIn}: {string.Join(",", this.Locations)}";
        }
    }
}
