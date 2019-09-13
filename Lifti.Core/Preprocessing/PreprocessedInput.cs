namespace Lifti
{
    public struct PreprocessedInput
    {
        public PreprocessedInput(char value)
        {
            this.Replacement = null;
            this.Value = value;
        }

        public PreprocessedInput(string replacement)
        {
            this.Value = '\0';
            this.Replacement = replacement;
        }

        public char Value { get; }

        public string Replacement { get; }

        public static implicit operator PreprocessedInput(char value)
        {
            return new PreprocessedInput(value);
        }

        public static implicit operator PreprocessedInput(string replacement)
        {
            return new PreprocessedInput(replacement);
        }
    }
}
