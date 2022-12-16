using Lifti.Tokenization.Preprocessing;
using System;

namespace Lifti
{
    /// <summary>
    /// Describes the output from a <see cref="IInputPreprocessor.Preprocess(char)"/> invocation.
    /// The output of 
    /// </summary>
    public readonly struct PreprocessedInput : IEquatable<PreprocessedInput>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PreprocessedInput"/> struct. This overload should
        /// only be used if the replacement is a single character, or the input character has not been modified
        /// by the pre-processing action.
        /// </summary>
        /// <param name="value">
        /// The single character replacement value.
        /// </param>
        public PreprocessedInput(char value)
        {
            this.Replacement = null;
            this.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreprocessedInput"/> struct. This overload should only be used if the 
        /// replacement is expanding a single character to a multi-character replacement.
        /// </summary>
        /// <param name="replacement">
        /// The multi-character replacement text.
        /// </param>
        public PreprocessedInput(string replacement)
        {
            this.Value = '\0';
            this.Replacement = replacement;
        }

        /// <summary>
        /// Gets an instance of <see cref="PreprocessedInput"/> that yields no characters.
        /// </summary>
        public static PreprocessedInput Empty { get; } = new PreprocessedInput(string.Empty);

        /// <summary>
        /// Gets the single character value that this instance represents. This is <c>\0</c> if
        /// the instance is a multi-character replacement.
        /// </summary>
        /// <value>
        /// The value if this instance is a single-character replacement, otherwise <c>\0</c>.
        /// </value>
        public char Value { get; }

        /// <summary>
        /// Gets the multi-character value that this instance represents. This is null if
        /// this instance is a single-character replacement.
        /// </summary>
        /// <value>
        /// The value if this instance is a multi-character replacement, otherwise <c>null</c>.
        /// </value>
        public string? Replacement { get; }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is PreprocessedInput input &&
                this.Equals(input);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Value, this.Replacement);
        }


        /// <summary>
        /// Performs an implicit conversion from <see cref="System.Char" /> to <see cref="PreprocessedInput" />.
        /// </summary>
        /// <param name="value">
        /// The single character value that this instance represents.
        /// </param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator PreprocessedInput(char value)
        {
            return new PreprocessedInput(value);
        }

        /// <summary>
        ///   Performs an implicit conversion from <see cref="System.String" /> to <see cref="PreprocessedInput" />.
        /// </summary>
        /// <param name="replacement">
        /// The multi-character text that this instance represents.
        /// </param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator PreprocessedInput(string replacement)
        {
            return new PreprocessedInput(replacement);
        }

        /// <inheritdoc />
        public bool Equals(PreprocessedInput other)
        {
            return this.Value == other.Value &&
                   this.Replacement == other.Replacement;
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(PreprocessedInput left, PreprocessedInput right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(PreprocessedInput left, PreprocessedInput right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Converts the given single character value to a <see cref="PreprocessedInput"/> instance.
        /// </summary>
        public static PreprocessedInput ToPreprocessedInput(char value)
        {
            return value;
        }

        /// <summary>
        /// Converts a multi-character text string value to a <see cref="PreprocessedInput"/> instance.
        /// </summary>
        public static PreprocessedInput ToPreprocessedInput(string value)
        {
            return value;
        }
    }
}
