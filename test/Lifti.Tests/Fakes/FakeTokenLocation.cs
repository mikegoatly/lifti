using System;
using System.Collections.Generic;

namespace Lifti.Tests.Fakes
{
    public class FakeTokenLocation : ITokenLocation
    {
        private readonly TokenLocation[] locations;

        public FakeTokenLocation(int minTokenIndex, int maxTokenIndex, params TokenLocation[] locations)
        {
            this.MaxTokenIndex = maxTokenIndex;
            this.MinTokenIndex = minTokenIndex;
            this.locations = locations;
        }

        public int MaxTokenIndex { get; private set; }

        public int MinTokenIndex { get; private set; }

        public void AddTo(HashSet<TokenLocation> locations)
        {
            foreach (var location in this.locations)
            {
                locations.Add(location);
            }
        }

        int IComparable<ITokenLocation>.CompareTo(ITokenLocation? other)
        {
            throw new NotImplementedException();
        }

        bool IEquatable<ITokenLocation>.Equals(ITokenLocation? other)
        {
            throw new NotImplementedException();
        }
    }
}
