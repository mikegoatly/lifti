using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Tokenization.Stemming
{
    public class PorterStemmer
    {
        /// <summary>
        /// The list of apostrophe based endings that can be pruned in step 0.
        /// </summary>
        private readonly Dictionary<char, string[]> apostropheEnds = CreateSearchLookup(new[] { "'S'", "'S", "'" });

        /// <summary>
        /// The list of double characters that can be replaced with a single character in step 1B.
        /// </summary>
        private readonly Dictionary<char, string[]> doubles = CreateSearchLookup(new[] { "BB", "DD", "FF", "GG", "MM", "NN", "PP", "RR", "TT" });

        /// <summary>
        /// The set of exceptions that are obeyed prior to any steps being executed.
        /// </summary>
        private readonly WordReplacement[] exceptions =
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
        private readonly WordReplacement[] exceptions2 =
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
        /// The list of characters that must precede ION at the end of a word for it to be removable in step 4.
        /// </summary>
        private readonly HashSet<char> removableIonEndings = new HashSet<char>(new[] { 'S', 'T' });

        /// <summary>
        /// The list of characters that must precede LI at the end of a word for it to be removable in step 2.
        /// </summary>
        private readonly HashSet<char> removableLiEndings = new HashSet<char>(new[] { 'C', 'D', 'E', 'G', 'H', 'K', 'M', 'N', 'R', 'T' });

        /// <summary>
        /// The list of endings need to have an "e" appended to the word during step 1b.
        /// </summary>
        private readonly Dictionary<char, string[]> step1bAppendEEndings = CreateSearchLookup(new[] { "AT", "BL", "IZ" });

        /// <summary>
        /// The replacements that can be made in step 1B.
        /// </summary>
        private readonly Dictionary<char, WordReplacement[]> step1bReplacements = CreateReplacementLookup(new[]
        {
            new WordReplacement("EEDLY", "EE"),
            new WordReplacement("INGLY", string.Empty),
            new WordReplacement("EDLY", string.Empty),
            new WordReplacement("EED", "EE"),
            new WordReplacement("ING", string.Empty),
            new WordReplacement("ED", string.Empty)
        });

        /// <summary>
        /// The replacements that can be made in step 2.
        /// </summary>
        private readonly Dictionary<char, WordReplacement[]> step2Replacements = CreateReplacementLookup(new[]
        {
            new WordReplacement("IZATION", "IZE"),
            new WordReplacement("IVENESS", "IVE"),
            new WordReplacement("FULNESS", "FUL"),
            new WordReplacement("ATIONAL", "ATE"),
            new WordReplacement("OUSNESS", "OUS"),
            new WordReplacement("BILITI", "BLE"),
            new WordReplacement("TIONAL", "TION"),
            new WordReplacement("LESSLI", "LESS"),
            new WordReplacement("FULLI", "FUL"),
            new WordReplacement("ENTLI", "ENT"),
            new WordReplacement("ATION", "ATE"),
            new WordReplacement("ALITI", "AL"),
            new WordReplacement("IVITI", "IVE"),
            new WordReplacement("OUSLI", "OUS"),
            new WordReplacement("ALISM", "AL"),
            new WordReplacement("ABLI", "ABLE"),
            new WordReplacement("ANCI", "ANCE"),
            new WordReplacement("ALLI", "AL"),
            new WordReplacement("IZER", "IZE"),
            new WordReplacement("ENCI", "ENCE"),
            new WordReplacement("ATOR", "ATE"),
            new WordReplacement("BLI", "BLE"),
            new WordReplacement("OGI", "OG"),
            new WordReplacement("LI", string.Empty)
        });

        /// <summary>
        /// The replacements that can be made in step 3.
        /// </summary>
        private readonly Dictionary<char, WordReplacement[]> step3Replacements = CreateReplacementLookup(new[]
        {
            new WordReplacement("ATIONAL", "ATE"),
            new WordReplacement("TIONAL", "TION"),
            new WordReplacement("ALIZE", "AL"),
            new WordReplacement("ICATE", "IC"),
            new WordReplacement("ICITI", "IC"),
            new WordReplacement("ATIVE", string.Empty),
            new WordReplacement("ICAL", "IC"),
            new WordReplacement("NESS", string.Empty),
            new WordReplacement("FUL", string.Empty)
        });

        /// <summary>
        /// The replacements that can be made in step 4.
        /// </summary>
        private readonly Dictionary<char, WordReplacement[]> step4Replacements = CreateReplacementLookup(new[]
        {
            new WordReplacement("EMENT", string.Empty),
            new WordReplacement("MENT", string.Empty),
            new WordReplacement("ABLE", string.Empty),
            new WordReplacement("IBLE", string.Empty),
            new WordReplacement("ANCE", string.Empty),
            new WordReplacement("ENCE", string.Empty),
            new WordReplacement("ATE", string.Empty),
            new WordReplacement("ITI", string.Empty),
            new WordReplacement("ION", string.Empty),
            new WordReplacement("IZE", string.Empty),
            new WordReplacement("IVE", string.Empty),
            new WordReplacement("OUS", string.Empty),
            new WordReplacement("ANT", string.Empty),
            new WordReplacement("ISM", string.Empty),
            new WordReplacement("ENT", string.Empty),
            new WordReplacement("AL", string.Empty),
            new WordReplacement("ER", string.Empty),
            new WordReplacement("IC", string.Empty)
        });

        /// <inheritdoc />
        public ReadOnlySpan<char> Stem(ReadOnlySpan<char> original)
        {
            if (original == null || original.Length < 3)
            {
                return original;
            }

            if (original[0] == '\'')
            {
                original = original.Slice(1);
            }

            var word = new ProcessingWord(original);
            if (TryMatchExceptionWord(ref word, this.exceptions, out var exception))
            {
                return exception.AsSpan();
            }

            word.ChangeY();
            var stemRegion = word.ComputeStemRegion();

            this.Step0(ref word);
            Step1a(ref word);

            if (TryMatchExceptionWord(ref word, this.exceptions2, out exception))
            {
                return exception.AsSpan();
            }

            this.Step1b(ref word, stemRegion);
            Step1c(ref word);
            this.Step2(ref word, stemRegion);
            this.Step3(ref word, stemRegion);
            this.Step4(ref word, stemRegion);
            Step5(ref word, stemRegion);

            return word.AsSpan();
        }

        /// <summary>
        /// Creates the replacement lookup with sets of replacements keyed by the last character in the search text.
        /// </summary>
        /// <param name="replacements">The replacements to create the lookup for.</param>
        /// <returns>The lookup of replacements, keyed on the last character in the search text.</returns>
        private static Dictionary<char, WordReplacement[]> CreateReplacementLookup(IEnumerable<WordReplacement> replacements)
        {
            return (from r in replacements
                    group r by r.MatchWord[r.MatchWord.Length - 1]
                        into g
                    select g).ToDictionary(r => r.Key, r => r.ToArray());
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

        private static void Step1a(ref ProcessingWord word)
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

        private static void Step1c(ref ProcessingWord word)
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

        private static void Step5(ref ProcessingWord word, StemRegion stemRegion)
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
            else if (word.EndsWith("LL") && length - 1 >= stemRegion.R2)
            {
                word.Length -= 1;
            }
        }

        private static bool TryMatchExceptionWord(ref ProcessingWord word, IEnumerable<WordReplacement> possibleExceptions, out string match)
        {
            foreach (var possibleException in possibleExceptions)
            {
                if (word.Equals(possibleException.MatchWord))
                {
                    match = possibleException.MatchResult;
                    return true;
                }
            }

            match = null;
            return false;
        }

        private void Step0(ref ProcessingWord word)
        {
            var endsWith = word.EndsWith(this.apostropheEnds);
            if (endsWith != null)
            {
                word.Length -= endsWith.Length;
            }
        }

        private void Step1b(ref ProcessingWord word, StemRegion stemRegion)
        {
            var replacement = word.EndsWith(this.step1bReplacements);
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
                            if (word.EndsWith(this.step1bAppendEEndings) != null)
                            {
                                word.Append('E');
                            }
                            else if (word.EndsWith(this.doubles) != null)
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

        private void Step2(ref ProcessingWord word, StemRegion stemRegion)
        {
            var replacement = word.EndsWith(this.step2Replacements);
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
                        if (length > 1 && this.removableLiEndings.Contains(word[length - 3]))
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

        private void Step3(ref ProcessingWord word, StemRegion stemRegion)
        {
            var replacement = word.EndsWith(this.step3Replacements);
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

        private void Step4(ref ProcessingWord word, StemRegion stemRegion)
        {
            var replacement = word.EndsWith(this.step4Replacements);
            if (replacement.MatchWord != null && word.Length - replacement.MatchWord.Length >= stemRegion.R2)
            {
                if (replacement.MatchWord == "ION")
                {
                    if (word.Length > 3 && this.removableIonEndings.Contains(word[word.Length - 4]))
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
    }
}
