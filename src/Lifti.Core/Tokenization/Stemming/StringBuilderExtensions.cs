using Lifti.Querying;
using System.Collections.Generic;
using System.Text;

namespace Lifti.Tokenization.Stemming
{
    /// <summary>
    /// Extensions for the StringBuilder class to help with the Porter stemming code.
    /// </summary>
    internal static class StringBuilderExtensions
    {
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
            return builder[index] switch
            {
                'A' or 'I' or 'E' or 'O' or 'U' or 'Y' => true,
                _ => false,
            };
        }

        /// <summary>
        /// Reverts any changed 'Y' characters once stemming is complete.
        /// </summary>
        public static void RevertY(this StringBuilder builder)
        {
            for (var i = 0; i < builder.Length; i++)
            {
                if (builder[i] == 'y')
                {
                    builder[i] = 'Y';
                }
            }
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
                return next != 'W' && next != 'X' && next != 'y' && !builder.IsVowel(offset - 1);
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
            return region.R1 >= builder.Length && builder.IsShortSyllable(builder.Length - 2);
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
            if (builder[0] == 'Y')
            {
                builder[0] = 'y';
                changed = true;
            }

            var length = builder.Length;
            for (var i = 1; i < length; i++)
            {
                if (builder[i] == 'Y' && builder.IsVowel(i - 1))
                {
                    builder[i] = 'y';
                    changed = true;
                }
            }

            return changed;
        }

        public static bool EndsWith(this StringBuilder builder, string substring)
        {
            return builder.EndsWith(substring, builder.Length);
        }

        private static bool EndsWith(this StringBuilder builder, string substring, int endOffset)
        {
            var length = builder.Length;
            if (length < substring.Length)
            {
                return false;
            }

            for (int i = length - substring.Length, j = 0; i < endOffset; i++, j++)
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
        /// <param name="replacementSetLookup">The potential replacements, keyed by the last character of the search text.</param>
        /// <returns>
        /// The text to replace at the end of the string builder. The match word of the word replacement will be null if no matches were found.
        /// </returns>
        public static WordReplacement EndsWith(this StringBuilder builder, IFullTextIndex<WordReplacement> replacementSetLookup)
        {
            var length = builder.Length;
            if (length > 3)
            {
                using var navigator = replacementSetLookup.Snapshot.CreateNavigator();
                if (navigator.Process(builder[builder.Length - 1]))
                {
                    var bestMatch = IntermediateQueryResult.Empty;
                    for (var i = builder.Length - 2; i >= 0; i--)
                    {
                        if (!navigator.Process(builder[i]))
                        {
                            break;
                        }

                        if (navigator.HasExactMatches)
                        {
                            bestMatch = navigator.GetExactMatches();
                        }
                    }

                    if (bestMatch.Matches.Count > 0)
                    {
                        return replacementSetLookup.Metadata.GetMetadata(bestMatch.Matches[0].DocumentId).Key;
                    }
                }
            }

            return default;
        }

        /// <summary>
        /// Tests if any of the given substrings appear at the end of the string builder.
        /// </summary>
        /// <param name="builder">The builder to test the contents of.</param>
        /// <param name="substringLookup">The substrings to test, keyed by the last letter in the search string.</param>
        /// <returns>
        /// The substring that was matched at the end of the string builder, or null if no matches were found.
        /// </returns>
        public static string? EndsWith(this StringBuilder builder, Dictionary<char, string[]> substringLookup)
        {
            var length = builder.Length;
            if (length > 0 &&
                substringLookup.TryGetValue(builder[length - 1], out var potentialSubstrings))
            {
                var endTestOffset = length - 1;
                foreach (var potentialMatch in potentialSubstrings)
                {
                    if (builder.EndsWith(potentialMatch, endTestOffset))
                    {
                        return potentialMatch;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Replaces the end of the string builder.
        /// </summary>
        /// <param name="builder">The builder to replace the end for.</param>
        /// <param name="replacement">The replacement to make.</param>
        public static void ReplaceEnd(this StringBuilder builder, WordReplacement replacement)
        {
            if (replacement.MatchResult == null)
            {
                if (replacement.TrimCharacterCount > 0)
                {
                    builder.Length -= replacement.TrimCharacterCount;
                }
            }
            else
            {
                builder.Length -= replacement.MatchWord.Length;
                builder.Append(replacement.MatchResult);
            }
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

            if (r1 >= 5 && (builder.StartsWith("GENER") || builder.StartsWith("ARSEN")))
            {
                r1 = 5;
            }
            else if (r1 >= 6 && builder.StartsWith("COMMUN"))
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
    }
}
