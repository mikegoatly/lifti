using Lifti.Querying;
using System;
using System.Collections.Generic;

namespace Lifti.Tokenization.Stemming
{
    /// <summary>
    /// Extensions for the CharacterBuffer struct to help with the Porter stemming code.
    /// </summary>
    internal static class CharacterBufferExtensions
    {
        /// <summary>
        /// Determines whether the character at the given index is a vowel.
        /// </summary>
        /// <param name="buffer">The buffer to check within.</param>
        /// <param name="index">The index to check at.</param>
        /// <returns>
        ///     <c>true</c> if the character at the given index is a vowel; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsVowel(this ref CharacterBuffer buffer, int index)
        {
            return buffer[index] switch
            {
                'A' or 'I' or 'E' or 'O' or 'U' or 'Y' => true,
                _ => false,
            };
        }

        /// <summary>
        /// Reverts any changed 'Y' characters once stemming is complete.
        /// </summary>
        public static void RevertY(this ref CharacterBuffer buffer)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] == 'y')
                {
                    buffer[i] = 'Y';
                }
            }
        }

        /// <summary>
        /// Determines whether the part of the word at the given offset is a short syllable.
        /// </summary>
        /// <param name="buffer">The buffer to analyse.</param>
        /// <param name="offset">The offset to check at.</param>
        /// <returns>
        ///    <c>true</c> if the part of the word at the given offset is a short syllable; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsShortSyllable(this ref CharacterBuffer buffer, int offset)
        {
            if (offset < 0)
            {
                return false;
            }

            if (offset == 0)
            {
                return buffer.IsVowel(0) && !buffer.IsVowel(1);
            }

            // Note offset must be > 1 to get here
            if (buffer.IsVC(offset))
            {
                var next = buffer[offset + 1];
                return next != 'W' && next != 'X' && next != 'y' && !buffer.IsVowel(offset - 1);
            }

            return false;
        }

        /// <summary>
        /// Determines whether the word in the character buffer is a short word.
        /// </summary>
        /// <param name="buffer">The buffer to analyse.</param>
        /// <param name="region">The calculated stem region for the word.</param>
        /// <returns>
        ///     <c>true</c> if the word is a short word; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsShortWord(this ref CharacterBuffer buffer, StemRegion region)
        {
            return region.R1 >= buffer.Length && buffer.IsShortSyllable(buffer.Length - 2);
        }

        /// <summary>
        /// Swaps out 'y' characters for 'Y's where they appear at the start of the word or
        /// after a vowel.
        /// </summary>
        /// <param name="buffer">The character buffer to update.</param>
        /// <returns><c>true</c> if Y's were changed, otherwise <c>false</c></returns>
        public static bool ChangeY(this ref CharacterBuffer buffer)
        {
            var changed = false;
            if (buffer[0] == 'Y')
            {
                buffer[0] = 'y';
                changed = true;
            }

            var length = buffer.Length;
            for (var i = 1; i < length; i++)
            {
                if (buffer[i] == 'Y' && buffer.IsVowel(i - 1))
                {
                    buffer[i] = 'y';
                    changed = true;
                }
            }

            return changed;
        }

        public static bool EndsWith(this ref CharacterBuffer buffer, string substring)
        {
            return buffer.EndsWith(substring, buffer.Length);
        }

        private static bool EndsWith(this ref CharacterBuffer buffer, string substring, int endOffset)
        {
            var length = buffer.Length;
            if (length < substring.Length)
            {
                return false;
            }

            for (int i = length - substring.Length, j = 0; i < endOffset; i++, j++)
            {
                if (buffer[i] != substring[j])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Tests if any of the given substrings appear at the end of the character buffer.
        /// </summary>
        /// <param name="buffer">The buffer to check within.</param>
        /// <param name="replacementSetLookup">The potential replacements, keyed by the last character of the search text.</param>
        /// <returns>
        /// The text to replace at the end of the character buffer. The match word of the word replacement will be null if no matches were found.
        /// </returns>
        public static WordReplacement EndsWith(this ref CharacterBuffer buffer, IFullTextIndex<WordReplacement> replacementSetLookup)
        {
            var length = buffer.Length;
            if (length > 3)
            {
                using var navigator = replacementSetLookup.Snapshot.CreateNavigator();
                if (navigator.Process(buffer[buffer.Length - 1]))
                {
                    var bestMatch = IntermediateQueryResult.Empty;
                    for (var i = buffer.Length - 2; i >= 0; i--)
                    {
                        if (!navigator.Process(buffer[i]))
                        {
                            break;
                        }

                        if (navigator.HasExactMatches)
                        {
                            bestMatch = navigator.GetExactMatches(QueryContext.Empty);
                        }
                    }

                    if (bestMatch.Matches.Count > 0)
                    {
                        return replacementSetLookup.Metadata.GetDocumentMetadata(bestMatch.Matches[0].DocumentId).Key;
                    }
                }
            }

            return default;
        }

        /// <summary>
        /// Tests if any of the given substrings appear at the end of the character buffer.
        /// </summary>
        /// <param name="buffer">The buffer to test the contents of.</param>
        /// <param name="substringLookup">The substrings to test, keyed by the last letter in the search string.</param>
        /// <returns>
        /// The substring that was matched at the end of the character buffer, or null if no matches were found.
        /// </returns>
        public static string? EndsWith(this ref CharacterBuffer buffer, Dictionary<char, string[]> substringLookup)
        {
            var length = buffer.Length;
            if (length > 0 &&
                substringLookup.TryGetValue(buffer[length - 1], out var potentialSubstrings))
            {
                var endTestOffset = length - 1;
                foreach (var potentialMatch in potentialSubstrings)
                {
                    if (buffer.EndsWith(potentialMatch, endTestOffset))
                    {
                        return potentialMatch;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Replaces the end of the character buffer.
        /// </summary>
        /// <param name="buffer">The buffer to replace the end for.</param>
        /// <param name="replacement">The replacement to make.</param>
        public static void ReplaceEnd(this ref CharacterBuffer buffer, WordReplacement replacement)
        {
            if (replacement.MatchResult == null)
            {
                if (replacement.TrimCharacterCount > 0)
                {
                    buffer.Length -= replacement.TrimCharacterCount;
                }
            }
            else
            {
                buffer.Length -= replacement.MatchWord.Length;
                buffer.Append(replacement.MatchResult);
            }
        }

        /// <summary>
        /// Tests if the given substring appears at the start of the character buffer.
        /// </summary>
        /// <param name="buffer">The buffer to check within.</param>
        /// <param name="substring">The substring to test.</param>
        /// <returns>
        ///     <c>true</c> if the given substring appears at the start of the character buffer, otherwise <c>false</c>.
        /// </returns>
        public static bool StartsWith(this ref CharacterBuffer buffer, string substring)
        {
            if (buffer.Length < substring.Length)
            {
                return false;
            }

            var length = substring.Length;
            for (var i = 0; i < length; i++)
            {
                if (buffer[i] != substring[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether there is a vowel followed by a consonant at the given index.
        /// </summary>
        /// <param name="buffer">The buffer to check within.</param>
        /// <param name="index">The index to search at.</param>
        /// <returns>
        ///     <c>true</c> there is a vowel followed by a consonant at the given index; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsVC(this ref CharacterBuffer buffer, int index)
        {
            return index < buffer.Length - 1 &&
                buffer.IsVowel(index) &&
                !buffer.IsVowel(index + 1);
        }

        /// <summary>
        /// Computes the stem region within the given character buffer.
        /// </summary>
        /// <param name="buffer">The buffer to analyse.</param>
        /// <returns>The computed stem region.</returns>
        public static StemRegion ComputeStemRegion(this ref CharacterBuffer buffer)
        {
            var length = buffer.Length;
            var r1 = length;
            var r2 = length;

            if (r1 >= 5 && (buffer.StartsWith("GENER") || buffer.StartsWith("ARSEN")))
            {
                r1 = 5;
            }
            else if (r1 >= 6 && buffer.StartsWith("COMMUN"))
            {
                r1 = 6;
            }
            else
            {
                // Compute R1
                for (var i = 1; i < length; i++)
                {
                    if (!buffer.IsVowel(i) && buffer.IsVowel(i - 1))
                    {
                        r1 = i + 1;
                        break;
                    }
                }
            }

            // Compute R2
            for (var i = r1 + 1; i < length; i++)
            {
                if (!buffer.IsVowel(i) && buffer.IsVowel(i - 1))
                {
                    r2 = i + 1;
                    break;
                }
            }

            return new StemRegion(r1, r2);
        }

        /// <summary>
        /// Determines whether the character buffer content is equal to the given string.
        /// </summary>
        /// <param name="buffer">The buffer to check.</param>
        /// <param name="chars">The string to compare against.</param>
        /// <returns>
        ///     <c>true</c> if the character buffer content equals the given string; otherwise, <c>false</c>.
        /// </returns>
        public static bool SequenceEqual(this ref CharacterBuffer buffer, string chars)
        {
            if (chars.Length != buffer.Length)
            {
                return false;
            }

            for (var i = 0; i < chars.Length; i++)
            {
                if (chars[i] != buffer[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
