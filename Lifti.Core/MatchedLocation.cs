using System.Collections.Generic;

namespace Lifti
{
    public class MatchedLocation
    {
        public MatchedLocation(string foundIn, IReadOnlyList<Range> locations)
        {
            this.FoundIn = foundIn;
            this.Locations = locations;
        }

        public string FoundIn { get; set; }
        public IReadOnlyList<Range> Locations { get; set; }

        public override string ToString()
        {
            return $"{FoundIn}: {string.Join(",", this.Locations)}";
        }
    }
}
