using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tokenization.Stemming
{
    internal class PorterStemmer : IStemmer
    {
        /// <summary>
        /// The list of apostrophe based endings that can be pruned in step 0.
        /// </summary>
        private static readonly Dictionary<char, string[]> apostropheEnds = CreateSearchLookup(["'S'", "'S", "'"]);

        /// <summary>
        /// The set of exceptions that are obeyed prior to any steps being executed.
        /// </summary>
        private static readonly WordReplacement[] exceptions =
            [
                new("SKIS", "SKI"),
                new("SKIES", "SKY"),
                new("DYING", "DIE"),
                new("LYING", "LIE"),
                new("TYING", "TIE"),
                new("IDLY", "IDL"),
                new("GENTLY", "GENTL"),
                new("UGLY", "UGLI"),
                new("EARLY", "EARLI"),
                new("ONLY", "ONLI"),
                new("SINGLY", "SINGL"),
                new("SKY"),
                new("NEWS"),
                new("HOWE"),
                new("ATLAS"),
                new("COSMOS"),
                new("BIAS"),
                new("ANDES")
            ];

        /// <summary>
        /// The set of exceptions that are obeyed between steps 1A and 1B.
        /// </summary>
        private static readonly WordReplacement[] exceptions2 =
            [
                new("INNING"),
                new("OUTING"),
                new("CANNING"),
                new("HERRING"),
                new("EARRING"),
                new("PROCEED"),
                new("EXCEED"),
                new("SUCCEED")
            ];

        /// <summary>
        /// The list of endings need to have an "e" appended to the word during step 1b.
        /// </summary>
        private static readonly Dictionary<char, string[]> step1bAppendEEndings = CreateSearchLookup(["AT", "BL", "IZ"]);

        /// <summary>
        /// The replacements that can be made in step 1B.
        /// </summary>
        private static readonly FullTextIndex<WordReplacement> step1bReplacements = CreateReplacementLookup(
            new WordReplacement[]
            {
                new("EEDLY", 3),
                new("INGLY", 5),
                new("EDLY", 4),
                new("EED", 1),
                new("ING", 3),
                new("ED", 2)
            });

        /// <summary>
        /// The replacements that can be made in step 2.
        /// </summary>
        private static readonly FullTextIndex<WordReplacement> step2Replacements = CreateReplacementLookup(
            new WordReplacement[]
            {
                new("IZATION", "IZE"),
                new("IVENESS", 4),
                new("FULNESS", 4),
                new("ATIONAL", "ATE"),
                new("OUSNESS", 4),
                new("BILITI", "BLE"),
                new("TIONAL", 2),
                new("LESSLI", 2),
                new("FULLI", 2),
                new("ENTLI", 2),
                new("ATION", "ATE"),
                new("ALITI", 3),
                new("IVITI", "IVE"),
                new("OUSLI", 2),
                new("ALISM", 3),
                new("ABLI", "ABLE"),
                new("ANCI", "ANCE"),
                new("ALLI", 2),
                new("IZER", 1),
                new("ENCI", "ENCE"),
                new("ATOR", "ATE"),
                new("BLI", "BLE"),
                new("OGI", 1),
                new("LI", 2)
            });

        /// <summary>
        /// The replacements that can be made in step 3.
        /// </summary>
        private static readonly FullTextIndex<WordReplacement> step3Replacements = CreateReplacementLookup(
            new WordReplacement[]
            {
                new("ATIONAL", "ATE"),
                new("TIONAL", 2),
                new("ALIZE", 3),
                new("ICATE", 3),
                new("ICITI", 3),
                new("ATIVE", 5),
                new("ICAL", 2),
                new("NESS", 4),
                new("FUL", 3)
            });

        /// <summary>
        /// The replacements that can be made in step 4.
        /// </summary>
        private static readonly FullTextIndex<WordReplacement> step4Replacements = CreateReplacementLookup(
            new WordReplacement[]
            {
                new("EMENT", 5),
                new("MENT", 4),
                new("ABLE", 4),
                new("IBLE", 4),
                new("ANCE", 4),
                new("ENCE", 4),
                new("ATE", 3),
                new("ITI", 3),
                new("ION", 3),
                new("IZE", 3),
                new("IVE", 3),
                new("OUS", 3),
                new("ANT", 3),
                new("ISM", 3),
                new("ENT", 3),
                new("AL", 2),
                new("ER", 2),
                new("IC", 2)
            });

        /// <inheritdoc />
        public bool RequiresCaseInsensitivity => true;

        /// <inheritdoc />
        public bool RequiresAccentInsensitivity => true;

        /// <inheritdoc />
        public void Stem(ref CharacterBuffer buffer)
        {
            if (buffer.Length < 3)
            {
                return;
            }

            if (buffer[0] == '\'')
            {
                // Remove the leading apostrophe by shifting left
                // Remove the leading apostrophe by shifting left efficiently
                buffer.AsSpan().Slice(1, buffer.Length - 1).CopyTo(buffer.AsSpan());
                buffer.Length--;
            }

            if (MatchExceptionWord(ref buffer, exceptions))
            {
                return;
            }

            var changedY = buffer.ChangeY();
            var stemRegion = buffer.ComputeStemRegion();

            Step0(ref buffer);
            Step1a(ref buffer);

            if (MatchExceptionWord(ref buffer, exceptions2))
            {
                return;
            }

            Step1b(ref buffer, stemRegion);
            Step1c(ref buffer);
            Step2(ref buffer, stemRegion);
            Step3(ref buffer, stemRegion);
            Step4(ref buffer, stemRegion);
            Step5(ref buffer, stemRegion);

            if (changedY)
            {
                buffer.RevertY();
            }
        }

        /// <summary>
        /// Creates the replacement lookup with sets of replacements keyed by the last character in the search text.
        /// </summary>
        /// <param name="replacements">The replacements to create the lookup for.</param>
        /// <returns>The lookup of replacements, keyed on the last character in the search text.</returns>
        private static FullTextIndex<WordReplacement> CreateReplacementLookup(
            IEnumerable<WordReplacement> replacements)
        {
            var index = new FullTextIndexBuilder<WordReplacement>()
                .WithObjectTokenization<WordReplacement>(
                    i => i.WithKey(item => item)
                        .WithField(
                            "find",
                            x => new string(x.MatchWord.Reverse().ToArray()),
                            to => to.SplitOnPunctuation(false)
                                .CaseInsensitive(false)
                                .AccentInsensitive(false)))
                .Build();

            // This is safe because the only time this could become actually async is if the index
            // is extended to use some extension that is truly async
            index.AddRangeAsync(replacements).GetAwaiter().GetResult();

            return index;
        }

        /// <summary>
        /// Creates the search lookup with sets of search criteria keyed by the last character in the search text.
        /// </summary>
        /// <param name="searchPossibilities">The search possibilities.</param>
        /// <returns>The lookup of search criteria, keyed on the last character in the search text.</returns>
        private static Dictionary<char, string[]> CreateSearchLookup(string[] searchPossibilities)
        {
            return (from r in searchPossibilities
                    group r by r[r.Length - 1]
                        into g
                    select g).ToDictionary(r => r.Key, r => r.ToArray());
        }

        private static bool IsRemovableLiEnding(char letter)
        {
            return letter switch
            {
                'C' or 'D' or 'E' or 'G' or 'H' or 'K' or 'M' or 'N' or 'R' or 'T' => true,
                _ => false,
            };
        }

        private static bool IsRemovableIonEnding(char letter)
        {
            return letter switch
            {
                'S' or 'T' => true,
                _ => false,
            };
        }

        // CharacterBuffer overloads for all stemming methods
        private static bool MatchExceptionWord(ref CharacterBuffer word, IEnumerable<WordReplacement> possibleExceptions)
        {
            foreach (var possibleException in possibleExceptions)
            {
                if (word.SequenceEqual(possibleException.MatchWord))
                {
                    if (possibleException.MatchResult == null)
                    {
                        word.Length -= possibleException.TrimCharacterCount;
                    }
                    else
                    {
                        word.Clear();
                        word.Append(possibleException.MatchResult);
                    }

                    return true;
                }
            }

            return false;
        }

        private static void Step0(ref CharacterBuffer word)
        {
            var endsWith = word.EndsWith(apostropheEnds);
            if (endsWith != null)
            {
                word.Length -= endsWith.Length;
            }
        }

        private static void Step1a(ref CharacterBuffer word)
        {
            if (word.EndsWith("SSES"))
            {
                word.Length -= 2;
            }
            else
            {
                var length = word.Length;
                var lastChar = word[length - 1];
                if (length > 2 &&
                    word[length - 3] == 'I' &&
                    word[length - 2] == 'E' &&
                    (lastChar == 'D' || lastChar == 'S'))
                {
                    word.Length -= (length > 4) ? 2 : 1;
                }
                else
                {
                    if (length > 1 &&
                        lastChar == 'S' &&
                        (word[length - 2] == 'U' || word[length - 2] == 'S'))
                    {
                        return;
                    }

                    if (lastChar == 'S')
                    {
                        for (var i = 0; i < word.Length - 2; i++)
                        {
                            if (word.IsVowel(i))
                            {
                                word.Length -= 1;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static void Step1b(ref CharacterBuffer word, StemRegion stemRegion)
        {
            var replacement = word.EndsWith(step1bReplacements);
            var matchedWord = replacement.MatchWord;
            if (matchedWord != null)
            {
                switch (matchedWord)
                {
                    case "EEDLY":
                    case "EED":
                        if (word.Length - matchedWord.Length >= stemRegion.R1)
                        {
                            word.ReplaceEnd(replacement);
                        }

                        break;
                    default:
                        var found = false;
                        for (var j = 0; j < word.Length - matchedWord.Length; ++j)
                        {
                            if (word.IsVowel(j))
                            {
                                word.ReplaceEnd(replacement);
                                found = true;
                                break;
                            }
                        }

                        if (found)
                        {
                            if (word.EndsWith(step1bAppendEEndings) != null)
                            {
                                word.Append('E');
                            }
                            else if (HasDoubleLetterEnding(ref word))
                            {
                                word.Length -= 1;
                            }
                            else if (word.IsShortWord(stemRegion))
                            {
                                word.Append('E');
                            }
                        }

                        break;
                }
            }
        }

        private static void Step1c(ref CharacterBuffer word)
        {
            var length = word.Length;
            if (length <= 2)
            {
                return;
            }

            var lastChar = word[length - 1];
            if ((lastChar == 'y' || lastChar == 'Y') && !word.IsVowel(length - 2))
            {
                word[length - 1] = 'I';
            }
        }

        private static void Step2(ref CharacterBuffer word, StemRegion stemRegion)
        {
            var replacement = word.EndsWith(step2Replacements);
            var length = word.Length;
            if (replacement.MatchWord != null && length - replacement.MatchWord.Length >= stemRegion.R1)
            {
                switch (replacement.MatchWord)
                {
                    case "OGI":
                        if (length > 3 && word[length - replacement.MatchWord.Length - 1] == 'L')
                        {
                            word.ReplaceEnd(replacement);
                        }

                        break;

                    case "LI":
                        if (length > 1 && IsRemovableLiEnding(word[length - 3]))
                        {
                            word.Length -= 2;
                        }

                        break;

                    default:
                        word.ReplaceEnd(replacement);
                        break;
                }
            }
        }

        private static void Step3(ref CharacterBuffer word, StemRegion stemRegion)
        {
            var replacement = word.EndsWith(step3Replacements);
            if (replacement.MatchWord != null && word.Length - replacement.MatchWord.Length >= stemRegion.R1)
            {
                if (replacement.MatchWord == "ATIVE")
                {
                    if (word.Length - replacement.MatchWord.Length >= stemRegion.R2)
                    {
                        word.ReplaceEnd(replacement);
                    }
                }
                else
                {
                    word.ReplaceEnd(replacement);
                }
            }
        }

        private static void Step4(ref CharacterBuffer word, StemRegion stemRegion)
        {
            var replacement = word.EndsWith(step4Replacements);
            if (replacement.MatchWord != null && word.Length - replacement.MatchWord.Length >= stemRegion.R2)
            {
                if (replacement.MatchWord == "ION")
                {
                    if (word.Length > 3 && IsRemovableIonEnding(word[word.Length - 4]))
                    {
                        word.Length -= replacement.MatchWord.Length;
                    }
                }
                else
                {
                    word.Length -= replacement.MatchWord.Length;
                }
            }
        }

        private static void Step5(ref CharacterBuffer word, StemRegion stemRegion)
        {
            var length = word.Length;
            if (length == 0)
            {
                return;
            }

            if (word[length - 1] == 'E' &&
                (length - 1 >= stemRegion.R2 || (length - 1 >= stemRegion.R1 && !word.IsShortSyllable(length - 3))))
            {
                word.Length -= 1;
            }
            else if (length - 1 >= stemRegion.R2 && word.EndsWith("LL"))
            {
                word.Length -= 1;
            }
        }

        private static bool HasDoubleLetterEnding(ref CharacterBuffer word)
        {
            var last = word[word.Length - 1];
            return last switch
            {
                'B' or 'D' or 'F' or 'G' or 'M' or 'N' or 'P' or 'R' or 'T' => word[word.Length - 2] == last,
                _ => false,
            };
        }
    }
}
