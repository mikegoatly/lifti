using System;
using System.Collections.Generic;
using System.Text;

namespace Lifti.Tokenization.Stemming
{
    internal static partial class StringBuilderExtensions
    {
        /// <summary>
        /// The list of vowels. y is included as this is a requirement of the Porter stemmer algorithm.
        /// </summary>
        private static readonly HashSet<char> vowels = new HashSet<char>(new[] { 'a', 'i', 'e', 'o', 'u', 'y' });

        /// <summary>
        /// Determines whether the character at the given index is a vowel.
        /// </summary>
        /// <param name="builder">The builder to check within.</param>
        /// <param name="index">The index to check at.</param>
        /// <returns>
        ///     <c>true</c> if the character at the given index is a vowel; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsVowel(this StringBuilder builder, int index)
        {
            return vowels.Contains(builder[index]);
        }

        /// <summary>
        /// Determines whether the part of the word at the given offset is a short syllable.
        /// </summary>
        /// <param name="builder">The builder to analyse.</param>
        /// <param name="offset">The offset to check at.</param>
        /// <returns>
        ///    <c>true</c> if the part of the word at the given offset is a short syllable; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsShortSyllable(this StringBuilder builder, int offset)
        {
            if (offset < 0)
            {
                return false;
            }

            if (offset == 0)
            {
                return builder.IsVowel(0) && !builder.IsVowel(1);
            }

            // Note offset must be > 1 to get here
            if (builder.IsVC(offset))
            {
                var next = builder[offset + 1];
                return next != 'w' && next != 'x' && next != 'Y' && !builder.IsVowel(offset - 1);
            }

            return false;
        }

        /// <summary>
        /// Determines whether the word in the string builder is a short word.
        /// </summary>
        /// <param name="builder">The builder to analyse.</param>
        /// <param name="region">The calculated stem region for the word.</param>
        /// <returns>
        ///     <c>true</c> if the word is a short word; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsShortWord(this StringBuilder builder, StemRegion region)
        {
            return region.R1 >= builder.Length && IsShortSyllable(builder, builder.Length - 2);
        }

        /// <summary>
        /// Swaps out 'y' characters for 'Y's where they appear at the start of the word or
        /// after a vowel.
        /// </summary>
        /// <param name="builder">The string builder to update.</param>
        /// <returns><c>true</c> if Y's were change, otherwise <c>false</c></returns>
        public static bool ChangeY(this StringBuilder builder)
        {
            var changed = false;
            if (builder[0] == 'y')
            {
                builder[0] = 'Y';
                changed = true;
            }

            var length = builder.Length;
            for (var i = 1; i < length; i++)
            {
                if (builder[i] == 'y' && builder.IsVowel(i - 1))
                {
                    builder[i] = 'Y';
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>
        /// Tests if the given substring appears at the end of the string builder.
        /// </summary>
        /// <param name="builder">The builder to check within.</param>
        /// <param name="substring">The substring to test.</param>
        /// <returns>
        ///     <c>true</c> if the given substring appears at the end of the string builder, otherwise <c>false</c>.
        /// </returns>
        public static bool EndsWith(this StringBuilder builder, string substring)
        {
            var length = builder.Length;
            if (length < substring.Length)
            {
                return false;
            }

            for (int i = length - substring.Length, j = 0; i < length; i++, j++)
            {
                if (builder[i] != substring[j])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Tests if any of the given substrings appear at the end of the string builder.
        /// </summary>
        /// <param name="builder">The builder to check within.</param>
        /// <param name="substringLookup">The substrings to test, keyed by the last letter in the search string.</param>
        /// <returns>
        /// The substring that was matched at the end of the string builder, or null if no matches were found.
        /// </returns>
        public static string EndsWith(this StringBuilder builder, Dictionary<char, string[]> substringLookup)
        {
            string[] potentialSubstrings;
            if (builder.Length > 0 &&
                substringLookup.TryGetValue(builder[builder.Length - 1], out potentialSubstrings))
            {
                return EndsWith(builder, potentialSubstrings, p => p);
            }

            return null;
        }

        /// <summary>
        /// Tests if any of the given substrings appear at the end of the string builder.
        /// </summary>
        /// <param name="builder">The builder to check within.</param>
        /// <param name="replacementSetLookup">The potential replacements, keyed by the last character of the search text.</param>
        /// <returns>
        /// The text to replace at the end of the string builder. The match word of the word replacement will be null if no matches were found.
        /// </returns>
        public static WordReplacement EndsWith(this StringBuilder builder, Dictionary<char, WordReplacement[]> replacementSetLookup)
        {
            WordReplacement[] potentialReplacements;
            if (builder.Length > 0 &&
                replacementSetLookup.TryGetValue(builder[builder.Length - 1], out potentialReplacements))
            {
                return EndsWith(builder, potentialReplacements, p => p.MatchWord);
            }

            return default(WordReplacement);
        }

        /// <summary>
        /// Replaces the end of the string builder.
        /// </summary>
        /// <param name="builder">The builder to replace the end for.</param>
        /// <param name="replacement">The replacement to make.</param>
        public static void ReplaceEnd(this StringBuilder builder, WordReplacement replacement)
        {
            builder.Length -= replacement.MatchWord.Length;
            builder.Append(replacement.MatchResult);
        }

        /// <summary>
        /// Tests if the given substring appears at the start of the string builder.
        /// </summary>
        /// <param name="builder">The builder to check within.</param>
        /// <param name="substring">The substring to test.</param>
        /// <returns>
        ///     <c>true</c> if the given substring appears at the start of the string builder, otherwise <c>false</c>.
        /// </returns>
        public static bool StartsWith(this StringBuilder builder, string substring)
        {
            if (builder.Length < substring.Length)
            {
                return false;
            }

            var length = substring.Length;
            for (var i = 0; i < length; i++)
            {
                if (builder[i] != substring[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether there is a vowel followed by a consonant at the given index.
        /// </summary>
        /// <param name="builder">The builder to check within.</param>
        /// <param name="index">The index to search at.</param>
        /// <returns>
        ///     <c>true</c> there is a vowel followed by a consonant at the given index; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsVC(this StringBuilder builder, int index)
        {
            return index < builder.Length - 1 &&
                builder.IsVowel(index) &&
                !builder.IsVowel(index + 1);
        }

        /// <summary>
        /// Computes the stem region within the given string builder.
        /// </summary>
        /// <param name="builder">The builder to analyse.</param>
        /// <returns>The computed stem region.</returns>
        public static StemRegion ComputeStemRegion(this StringBuilder builder)
        {
            var length = builder.Length;
            var r1 = length;
            var r2 = length;

            if (r1 >= 5 && (builder.StartsWith("gener") || builder.StartsWith("arsen")))
            {
                r1 = 5;
            }
            else if (r1 >= 6 && builder.StartsWith("commun"))
            {
                r1 = 6;
            }
            else
            {
                // Compute R1
                for (var i = 1; i < length; i++)
                {
                    if (!builder.IsVowel(i) && builder.IsVowel(i - 1))
                    {
                        r1 = i + 1;
                        break;
                    }
                }
            }

            // Compute R2
            for (var i = r1 + 1; i < length; i++)
            {
                if (!builder.IsVowel(i) && builder.IsVowel(i - 1))
                {
                    r2 = i + 1;
                    break;
                }
            }

            return new StemRegion(r1, r2);
        }

        /// <summary>
        /// Tests if any of the given substrings appear at the end of the string builder. Note that the character at the end of the
        /// match texts will already have been tested prior to entry of this method, so will not be tested again.
        /// </summary>
        /// <typeparam name="TMatch">The type of the match.</typeparam>
        /// <param name="builder">The builder to check within.</param>
        /// <param name="potentialMatches">The potential matches to test.</param>
        /// <param name="matchText">The delegate capable of reading out the text to match from the match type.</param>
        /// <returns>
        /// The substring that was matched at the end of the string builder, or null if no matches were found.
        /// </returns>
        private static TMatch EndsWith<TMatch>(this StringBuilder builder, IEnumerable<TMatch> potentialMatches, Func<TMatch, string> matchText)
        {
            var length = builder.Length;
            var endTestOffset = length - 1;
            foreach (var potentialMatch in potentialMatches)
            {
                var test = matchText(potentialMatch);
                if (length < test.Length)
                {
                    continue;
                }

                var matched = true;
                for (int i = length - test.Length, j = 0; i < endTestOffset; i++, j++)
                {
                    if (builder[i] != test[j])
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched)
                {
                    return potentialMatch;
                }
            }

            return default(TMatch);
        }
    }
}
