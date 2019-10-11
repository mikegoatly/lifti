using System;
using System.Collections.Generic;
using System.Text;

namespace Lifti.Tokenization.Stemming
{
    internal ref struct ProcessingWord
    {
        /// <summary>
        /// The list of vowels. Y is included as this is a requirement of the Porter stemmer algorithm.
        /// </summary>
        private static readonly HashSet<char> vowels = new HashSet<char>(new[] { 'A', 'I', 'E', 'O', 'U', 'Y' });

        private bool changedY;

        private int length;

        public ProcessingWord(ReadOnlySpan<char> word)
        {
            this.Word = word;
            this.changedY = false;
            this.MaterializedEdit = null;
            this.length = word.Length;
        }

        public ReadOnlySpan<char> Word { get; private set; }
        public StringBuilder MaterializedEdit { get; private set; }

        public bool Equals(string other)
        {
            if (this.MaterializedEdit != null)
            {
                return this.MaterializedEdit.SequenceEqual(other);
            }

            return this.Word.SequenceEqual(other);
        }

        public int Length
        {
            get { return this.length; }
            set
            {
                if (this.MaterializedEdit != null)
                {
                    this.MaterializedEdit.Length = value;
                }
                else
                {
                    this.Word = this.Word.Slice(0, value);
                }

                this.length = value;
            }
        }

        public char this[int index]
        {
            get { return this.MaterializedEdit?[index] ?? this.Word[index]; }
            set
            {
                this.EnsureMaterialized();
                this.MaterializedEdit[index] = value;
            }
        }

        /// <summary>
        /// Determines whether this word is a short word.
        /// </summary>
        /// <param name="region">The calculated stem region for the word.</param>
        /// <returns>
        ///     <c>true</c> if the word is a short word; otherwise, <c>false</c>.
        /// </returns>
        public bool IsShortWord(StemRegion region)
        {
            return region.R1 >= this.Length && this.IsShortSyllable(this.Length - 2);
        }

        /// <summary>
        /// Determines whether the part of the word at the given offset is a short syllable.
        /// </summary>
        /// <param name="offset">The offset to check at.</param>
        /// <returns>
        ///    <c>true</c> if the part of the word at the given offset is a short syllable; otherwise, <c>false</c>.
        /// </returns>
        public bool IsShortSyllable(int offset)
        {
            if (offset < 0)
            {
                return false;
            }

            if (offset == 0)
            {
                return this.IsVowel(0) && !this.IsVowel(1);
            }

            // Note offset must be > 1 to get here
            if (this.IsVC(offset))
            {
                var next = this[offset + 1];
                return next != 'W' && next != 'X' && next != 'y' && !this.IsVowel(offset - 1);
            }

            return false;
        }

        /// <summary>
        /// Replaces the end of the word.
        /// </summary>
        /// <param name="replacement">The replacement to make.</param>
        public void ReplaceEnd(WordReplacement replacement)
        {
            if (replacement.MatchResult == null)
            {
                this.Length -= replacement.TrimCharacterCount;
            }
            else
            {
                this.EnsureMaterialized();
                this.MaterializedEdit.Length -= replacement.MatchWord.Length;
                this.MaterializedEdit.Append(replacement.MatchResult);

                this.length = this.MaterializedEdit.Length;
            }
        }

        public void Append(char letter)
        {
            this.EnsureMaterialized();
            this.MaterializedEdit.Append(letter);
            this.length = this.MaterializedEdit.Length;
        }

        public void Remove(int startIndex, int length)
        {
            this.EnsureMaterialized();
            this.MaterializedEdit.Remove(startIndex, length);
            this.length = this.MaterializedEdit.Length;
        }

        /// <summary>
        /// Returns a Span that represents this instance, post any processing.
        /// </summary>
        /// <returns></returns>
        public ReadOnlySpan<char> AsSpan()
        {
            if (this.MaterializedEdit != null)
            {
                if (this.changedY)
                {
                    for (var i = 0; i < this.MaterializedEdit.Length; i++)
                    {
                        if (this.MaterializedEdit[i] == 'y')
                        {
                            this.MaterializedEdit[i] = 'Y';
                        }
                    }
                }

                return this.MaterializedEdit.ToString().AsSpan();
            }

            return this.Word;
        }

        /// <summary>
        /// Determines whether there is a vowel followed by a consonant at the given index.
        /// </summary>
        /// <param name="index">The index to search at.</param>
        /// <returns>
        ///     <c>true</c> there is a vowel followed by a consonant at the given index; otherwise, <c>false</c>.
        /// </returns>
        public bool IsVC(int index)
        {
            return index < this.Length - 1 &&
                this.IsVowel(index) &&
                !this.IsVowel(index + 1);
        }

        /// <summary>
        /// Determines whether the character at the given index is a vowel.
        /// </summary>
        /// <param name="index">The index to check at.</param>
        /// <returns>
        ///     <c>true</c> if the character at the given index is a vowel; otherwise, <c>false</c>.
        /// </returns>
        public bool IsVowel(int index)
        {
            return vowels.Contains(this[index]);
        }

        /// <summary>
        /// Swaps out 'y' characters for 'Y's where they appear at the start of the word or
        /// after a vowel.
        /// </summary>
        public void ChangeY()
        {
            if (this[0] == 'Y')
            {
                this[0] = 'y';
                this.changedY = true;
            }

            var length = this.Length;
            for (var i = 1; i < length; i++)
            {
                if (this[i] == 'Y' && this.IsVowel(i - 1))
                {
                    this[i] = 'y';
                    this.changedY = true;
                }
            }
        }

        public bool StartsWith(string substring)
        {
            if (this.Length < substring.Length)
            {
                return false;
            }

            var length = substring.Length;
            for (var i = 0; i < length; i++)
            {
                if (this[i] != substring[i])
                {
                    return false;
                }
            }

            return true;
        }

        public bool EndsWith(string substring)
        {
            return this.EndsWith(substring, this.Length);
        }

        private bool EndsWith(string substring, int endOffset)
        {
            var length = this.Length;
            if (length < substring.Length)
            {
                return false;
            }

            if (this.MaterializedEdit == null)
            {
                for (int i = length - substring.Length, j = 0; i < endOffset; i++, j++)
                {
                    if (this.Word[i] != substring[j])
                    {
                        return false;
                    }
                }
            }
            else
            {
                for (int i = length - substring.Length, j = 0; i < endOffset; i++, j++)
                {
                    if (this.MaterializedEdit[i] != substring[j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Tests if any of the given substrings appear at the end of the string builder.
        /// </summary>
        /// <param name="builder">The builder to check within.</param>
        /// <param name="replacementSetLookup">The potential replacements, keyed by the last character of the search text.</param>
        /// <returns>
        /// The text to replace at the end of the string builder. The match word of the word replacement will be null if no matches were found.
        /// </returns>
        public WordReplacement EndsWith(Dictionary<char, WordReplacement[]> replacementSetLookup)
        {
            var length = this.Length;
            if (length > 0 &&
                replacementSetLookup.TryGetValue(this[length - 1], out var potentialReplacements))
            {
                var endTestOffset = length - 1;
                foreach (var potentialMatch in potentialReplacements)
                {
                    if (this.EndsWith(potentialMatch.MatchWord, endTestOffset))
                    {
                        return potentialMatch;
                    }
                }
            }

            return default(WordReplacement);
        }

        /// <summary>
        /// Tests if any of the given substrings appear at the end of the string builder.
        /// </summary>
        /// <param name="substringLookup">The substrings to test, keyed by the last letter in the search string.</param>
        /// <returns>
        /// The substring that was matched at the end of the string builder, or null if no matches were found.
        /// </returns>
        public string EndsWith(Dictionary<char, string[]> substringLookup)
        {
            var length = this.Length;
            if (length > 0 &&
                substringLookup.TryGetValue(this[length - 1], out var potentialSubstrings))
            {
                var endTestOffset = length - 1;
                foreach (var potentialMatch in potentialSubstrings)
                {
                    if (this.EndsWith(potentialMatch, endTestOffset))
                    {
                        return potentialMatch;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Computes the stem region within the given string builder.
        /// </summary>
        /// <returns>The computed stem region.</returns>
        public StemRegion ComputeStemRegion()
        {
            var length = this.Length;
            var r1 = length;
            var r2 = length;

            if (r1 >= 5 && (this.StartsWith("GENER") || this.StartsWith("ARSEN")))
            {
                r1 = 5;
            }
            else if (r1 >= 6 && this.StartsWith("COMMUN"))
            {
                r1 = 6;
            }
            else
            {
                // Compute R1
                for (var i = 1; i < length; i++)
                {
                    if (!this.IsVowel(i) && this.IsVowel(i - 1))
                    {
                        r1 = i + 1;
                        break;
                    }
                }
            }

            // Compute R2
            for (var i = r1 + 1; i < length; i++)
            {
                if (!this.IsVowel(i) && this.IsVowel(i - 1))
                {
                    r2 = i + 1;
                    break;
                }
            }

            return new StemRegion(r1, r2);
        }

        private void EnsureMaterialized()
        {
            if (this.MaterializedEdit != null)
            {
                return;
            }

            this.MaterializedEdit = new StringBuilder(this.Word.Length); // TODO Pool?
            foreach (var letter in this.Word)
            {
                this.MaterializedEdit.Append(letter);
            }
        }
    }
}
