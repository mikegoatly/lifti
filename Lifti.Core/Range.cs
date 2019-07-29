namespace Lifti
{
    public struct Range
    {
        public Range(int start, int length)
        {
            this.Start = start;
            this.Length = length;
        }

        public int Start { get; }
        public int Length { get; }

        public override string ToString()
        {
            return $"{this.Start},{this.Length}";
        }
    }
}
