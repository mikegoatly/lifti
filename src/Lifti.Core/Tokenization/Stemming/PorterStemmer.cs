﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lifti.Tokenization.Stemming
{
    internal class PorterStemmer : IStemmer
    {
        /// <summary>
        /// The list of apostrophe based endings that can be pruned in step 0.
        /// </summary>
        private static readonly Dictionary<char, string[]> apostropheEnds = CreateSearchLookup(new[] { "'S'", "'S", "'" });

        /// <summary>
        /// The set of exceptions that are obeyed prior to any steps being executed.
        /// </summary>
        private static readonly WordReplacement[] exceptions =
            {
                new WordReplacement("SKIS", "SKI"),
                new WordReplacement("SKIES", "SKY"),
                new WordReplacement("DYING", "DIE"),
                new WordReplacement("LYING", "LIE"),
                new WordReplacement("TYING", "TIE"),
                new WordReplacement("IDLY", "IDL"),
                new WordReplacement("GENTLY", "GENTL"),
                new WordReplacement("UGLY", "UGLI"),
                new WordReplacement("EARLY", "EARLI"),
                new WordReplacement("ONLY", "ONLI"),
                new WordReplacement("SINGLY", "SINGL"),
                new WordReplacement("SKY"),
                new WordReplacement("NEWS"),
                new WordReplacement("HOWE"),
                new WordReplacement("ATLAS"),
                new WordReplacement("COSMOS"),
                new WordReplacement("BIAS"),
                new WordReplacement("ANDES")
            };

        /// <summary>
        /// The set of exceptions that are obeyed between steps 1A and 1B.
        /// </summary>
        private static readonly WordReplacement[] exceptions2 =
            {
                new WordReplacement("INNING"),
                new WordReplacement("OUTING"),
                new WordReplacement("CANNING"),
                new WordReplacement("HERRING"),
                new WordReplacement("EARRING"),
                new WordReplacement("PROCEED"),
                new WordReplacement("EXCEED"),
                new WordReplacement("SUCCEED")
            };

        /// <summary>
        /// The list of endings need to have an "e" appended to the word during step 1b.
        /// </summary>
        private static readonly Dictionary<char, string[]> step1bAppendEEndings = CreateSearchLookup(new[] { "AT", "BL", "IZ" });

        /// <summary>
        /// The replacements that can be made in step 1B.
        /// </summary>
        private static readonly FullTextIndex<WordReplacement> step1bReplacements = CreateReplacementLookup(
            new[]
            {
                new WordReplacement("EEDLY", 3),
                new WordReplacement("INGLY", 5),
                new WordReplacement("EDLY", 4),
                new WordReplacement("EED", 1),
                new WordReplacement("ING", 3),
                new WordReplacement("ED", 2)
            });

        /// <summary>
        /// The replacements that can be made in step 2.
        /// </summary>
        private static readonly FullTextIndex<WordReplacement> step2Replacements = CreateReplacementLookup(
            new[]
            {
                new WordReplacement("IZATION", "IZE"),
                new WordReplacement("IVENESS", 4),
                new WordReplacement("FULNESS", 4),
                new WordReplacement("ATIONAL", "ATE"),
                new WordReplacement("OUSNESS", 4),
                new WordReplacement("BILITI", "BLE"),
                new WordReplacement("TIONAL", 2),
                new WordReplacement("LESSLI", 2),
                new WordReplacement("FULLI", 2),
                new WordReplacement("ENTLI", 2),
                new WordReplacement("ATION", "ATE"),
                new WordReplacement("ALITI", 3),
                new WordReplacement("IVITI", "IVE"),
                new WordReplacement("OUSLI", 2),
                new WordReplacement("ALISM", 3),
                new WordReplacement("ABLI", "ABLE"),
                new WordReplacement("ANCI", "ANCE"),
                new WordReplacement("ALLI", 2),
                new WordReplacement("IZER", 1),
                new WordReplacement("ENCI", "ENCE"),
                new WordReplacement("ATOR", "ATE"),
                new WordReplacement("BLI", "BLE"),
                new WordReplacement("OGI", 1),
                new WordReplacement("LI", 2)
            });

        /// <summary>
        /// The replacements that can be made in step 3.
        /// </summary>
        private static readonly FullTextIndex<WordReplacement> step3Replacements = CreateReplacementLookup(
            new[]
            {
                new WordReplacement("ATIONAL", "ATE"),
                new WordReplacement("TIONAL", 2),
                new WordReplacement("ALIZE", 3),
                new WordReplacement("ICATE", 3),
                new WordReplacement("ICITI", 3),
                new WordReplacement("ATIVE", 5),
                new WordReplacement("ICAL", 2),
                new WordReplacement("NESS", 4),
                new WordReplacement("FUL", 3)
            });

        /// <summary>
        /// The replacements that can be made in step 4.
        /// </summary>
        private static readonly FullTextIndex<WordReplacement> step4Replacements = CreateReplacementLookup(
            new[]
            {
                new WordReplacement("EMENT", 5),
                new WordReplacement("MENT", 4),
                new WordReplacement("ABLE", 4),
                new WordReplacement("IBLE", 4),
                new WordReplacement("ANCE", 4),
                new WordReplacement("ENCE", 4),
                new WordReplacement("ATE", 3),
                new WordReplacement("ITI", 3),
                new WordReplacement("ION", 3),
                new WordReplacement("IZE", 3),
                new WordReplacement("IVE", 3),
                new WordReplacement("OUS", 3),
                new WordReplacement("ANT", 3),
                new WordReplacement("ISM", 3),
                new WordReplacement("ENT", 3),
                new WordReplacement("AL", 2),
                new WordReplacement("ER", 2),
                new WordReplacement("IC", 2)
            });

        /// <inheritdoc />
        public void Stem(StringBuilder builder)
        {
            if (builder.Length < 3)
            {
                return;
            }

            if (builder[0] == '\'')
            {
                builder.Remove(0, 1);
            }

            if (MatchExceptionWord(builder, exceptions))
            {
                return;
            }

            var changedY = builder.ChangeY();
            var stemRegion = builder.ComputeStemRegion();

            Step0(builder);
            Step1a(builder);

            if (MatchExceptionWord(builder, exceptions2))
            {
                return;
            }

            Step1b(builder, stemRegion);
            Step1c(builder);
            Step2(builder, stemRegion);
            Step3(builder, stemRegion);
            Step4(builder, stemRegion);
            Step5(builder, stemRegion);

            if (changedY)
            {
                builder.RevertY();
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

        private static void Step1a(StringBuilder word)
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

        private static void Step1c(StringBuilder word)
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

        private static void Step5(StringBuilder word, StemRegion stemRegion)
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

        private static bool MatchExceptionWord(StringBuilder word, IEnumerable<WordReplacement> possibleExceptions)
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
                        word.Length = 0;
                        word.Append(possibleException.MatchResult);
                    }

                    return true;
                }
            }

            return false;
        }

        private static void Step0(StringBuilder word)
        {
            var endsWith = word.EndsWith(apostropheEnds);
            if (endsWith != null)
            {
                word.Length -= endsWith.Length;
            }
        }

        private static void Step1b(StringBuilder word, StemRegion stemRegion)
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
                            else if (HasDoubleLetterEnding(word))
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

        private static bool HasDoubleLetterEnding(StringBuilder word)
        {
            var last = word[word.Length - 1];
            return last switch
            {
                'B' or 'D' or 'F' or 'G' or 'M' or 'N' or 'P' or 'R' or 'T' => word[word.Length - 2] == last,
                _ => false,
            };
        }

        private static void Step2(StringBuilder word, StemRegion stemRegion)
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

        private static bool IsRemovableLiEnding(char letter)
        {
            return letter switch
            {
                'C' or 'D' or 'E' or 'G' or 'H' or 'K' or 'M' or 'N' or 'R' or 'T' => true,
                _ => false,
            };
        }

        private static void Step3(StringBuilder word, StemRegion stemRegion)
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

        private static void Step4(StringBuilder word, StemRegion stemRegion)
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

        private static bool IsRemovableIonEnding(char letter)
        {
            return letter switch
            {
                'S' or 'T' => true,
                _ => false,
            };
        }
    }
}
