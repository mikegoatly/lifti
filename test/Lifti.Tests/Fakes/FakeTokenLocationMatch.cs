using Lifti.Querying;
using System.Collections.Generic;

namespace Lifti.Tests.Fakes
{
    public class FakeTokenLocationMatch : ITokenLocationMatch
    {
        private readonly TokenLocation[] locations;

        public FakeTokenLocationMatch(int minTokenIndex, int maxTokenIndex, params TokenLocation[] locations)
        {
            this.MaxTokenIndex = maxTokenIndex;
            this.MinTokenIndex = minTokenIndex;
            this.locations = locations;
        }

        public int MaxTokenIndex { get; private set; }

        public int MinTokenIndex { get; private set; }

        public IEnumerable<TokenLocation> GetLocations()
        {
            return this.locations;
        }
    }
}
