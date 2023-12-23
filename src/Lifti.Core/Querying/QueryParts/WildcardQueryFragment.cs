using System;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// Represents a fragment of <see cref="WildcardQueryPart"/>.
    /// </summary>
    public readonly struct WildcardQueryFragment : IEquatable<WildcardQueryFragment>
    {
        private WildcardQueryFragment(WildcardQueryFragmentKind kind, string? text)
        {
            this.Kind = kind;
            this.Text = text;
        }

        /// <summary>
        /// A fragment representing a <see cref="WildcardQueryFragmentKind.MultiCharacter"/>/
        /// </summary>
        public static WildcardQueryFragment MultiCharacter { get; } = new WildcardQueryFragment(WildcardQueryFragmentKind.MultiCharacter, null);

        /// <summary>
        /// A fragment representing a <see cref="WildcardQueryFragmentKind.MultiCharacter"/>/
        /// </summary>
        public static WildcardQueryFragment SingleCharacter { get; } = new WildcardQueryFragment(WildcardQueryFragmentKind.SingleCharacter, null);

        /// <summary>
        /// Gets the <see cref="WildcardQueryFragmentKind"/> this instance represents.
        /// </summary>
        public WildcardQueryFragmentKind Kind { get; }

        /// <summary>
        /// Gets the text contained in this fragment. Only set with <see cref="Kind"/> is <see cref="WildcardQueryFragmentKind.Text"/>.
        /// </summary>
        public string? Text { get; }

        /// <summary>
        /// Creates a new instance of <see cref="WildcardQueryFragment"/> representing a textual part of a wildcard query.
        /// </summary>
        /// <param name="text">The text that must be explicitly matched.</param>
        public static WildcardQueryFragment CreateText(string text)
        {
            return new(WildcardQueryFragmentKind.Text, text);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is WildcardQueryFragment fragment && this.Equals(fragment);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Kind, this.Text);
        }

        /// <inheritdoc />
        public static bool operator ==(WildcardQueryFragment left, WildcardQueryFragment right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc />
        public static bool operator !=(WildcardQueryFragment left, WildcardQueryFragment right)
        {
            return !(left == right);
        }

        /// <inheritdoc />
        public bool Equals(WildcardQueryFragment other)
        {
            return other.Kind == this.Kind && other.Text == this.Text;
        }
    }
}
