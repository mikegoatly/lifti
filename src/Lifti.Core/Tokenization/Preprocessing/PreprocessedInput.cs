using System;

namespace Lifti
{
    public struct PreprocessedInput : IEquatable<PreprocessedInput>
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

        public string? Replacement { get; }

        public override bool Equals(object obj)
        {
            return obj is PreprocessedInput input &&
                this.Equals(input);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Value, this.Replacement);
        }

        public static implicit operator PreprocessedInput(char value)
        {
            return new PreprocessedInput(value);
        }

        public static implicit operator PreprocessedInput(string replacement)
        {
            return new PreprocessedInput(replacement);
        }

        public bool Equals(PreprocessedInput other)
        {
            return this.Value == other.Value &&
                   this.Replacement == other.Replacement;
        }

        public static bool operator ==(PreprocessedInput left, PreprocessedInput right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PreprocessedInput left, PreprocessedInput right)
        {
            return !(left == right);
        }

        public static PreprocessedInput ToPreprocessedInput(char value)
        {
            return value;
        }

        public static PreprocessedInput ToPreprocessedInput(string value)
        {
            return value;
        }
    }
}
