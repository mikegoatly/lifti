using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lifti.Tokenization.Stemming
{
    public class PorterStemmer
    {
        /// <summary>
        /// The list of apostrophe based endings that can be pruned in step 0.
        /// </summary>
        private readonly Dictionary<char, string[]> apostropheEnds = CreateSearchLookup(new[] { "'s'", "'s", "'" });

        /// <summary>
        /// The list of double characters that can be replaced with a single character in step 1B.
        /// </summary>
        private readonly Dictionary<char, string[]> doubles = CreateSearchLookup(new[] { "bb", "dd", "ff", "gg", "mm", "nn", "pp", "rr", "tt" });

        /// <summary>
        /// The set of exceptions that are obeyed prior to any steps being executed.
        /// </summary>
        private readonly WordReplacement[] exceptions =
        {
            new WordReplacement("skis", "ski"),
            new WordReplacement("skies", "sky"),
            new WordReplacement("dying", "die"),
            new WordReplacement("lying", "lie"),
            new WordReplacement("tying", "tie"),
            new WordReplacement("idly", "idl"),
            new WordReplacement("gently", "gentl"),
            new WordReplacement("ugly", "ugli"),
            new WordReplacement("early", "earli"),
            new WordReplacement("only", "onli"),
            new WordReplacement("singly", "singl"),
            new WordReplacement("sky"),
            new WordReplacement("news"),
            new WordReplacement("howe"),
            new WordReplacement("atlas"),
            new WordReplacement("cosmos"),
            new WordReplacement("bias"),
            new WordReplacement("andes")
        };

        /// <summary>
        /// The set of exceptions that are obeyed between steps 1A and 1B.
        /// </summary>
        private readonly WordReplacement[] exceptions2 =
        {
            new WordReplacement("inning"),
            new WordReplacement("outing"),
            new WordReplacement("canning"),
            new WordReplacement("herring"),
            new WordReplacement("earring"),
            new WordReplacement("proceed"),
            new WordReplacement("exceed"),
            new WordReplacement("succeed")
        };

        /// <summary>
        /// The list of characters that must precede ION at the end of a word for it to be removable in step 4.
        /// </summary>
        private readonly HashSet<char> removableIonEndings = new HashSet<char>(new[] { 's', 't' });

        /// <summary>
        /// The list of characters that must precede LI at the end of a word for it to be removable in step 2.
        /// </summary>
        private readonly HashSet<char> removableLiEndings = new HashSet<char>(new[] { 'c', 'd', 'e', 'g', 'h', 'k', 'm', 'n', 'r', 't' });

        /// <summary>
        /// The list of endings need to have an "e" appended to the word during step 1b.
        /// </summary>
        private readonly Dictionary<char, string[]> step1bAppendEEndings = CreateSearchLookup(new[] { "at", "bl", "iz" });

        /// <summary>
        /// The replacements that can be made in step 1B.
        /// </summary>
        private readonly Dictionary<char, WordReplacement[]> step1bReplacements = CreateReplacementLookup(new[]
        {
            new WordReplacement("eedly", "ee"),
            new WordReplacement("ingly", string.Empty),
            new WordReplacement("edly", string.Empty),
            new WordReplacement("eed", "ee"),
            new WordReplacement("ing", string.Empty),
            new WordReplacement("ed", string.Empty)
        });

        /// <summary>
        /// The replacements that can be made in step 2.
        /// </summary>
        private readonly Dictionary<char, WordReplacement[]> step2Replacements = CreateReplacementLookup(new[]
        {
            new WordReplacement("ization", "ize"),
            new WordReplacement("iveness", "ive"),
            new WordReplacement("fulness", "ful"),
            new WordReplacement("ational", "ate"),
            new WordReplacement("ousness", "ous"),
            new WordReplacement("biliti", "ble"),
            new WordReplacement("tional", "tion"),
            new WordReplacement("lessli", "less"),
            new WordReplacement("fulli", "ful"),
            new WordReplacement("entli", "ent"),
            new WordReplacement("ation", "ate"),
            new WordReplacement("aliti", "al"),
            new WordReplacement("iviti", "ive"),
            new WordReplacement("ousli", "ous"),
            new WordReplacement("alism", "al"),
            new WordReplacement("abli", "able"),
            new WordReplacement("anci", "ance"),
            new WordReplacement("alli", "al"),
            new WordReplacement("izer", "ize"),
            new WordReplacement("enci", "ence"),
            new WordReplacement("ator", "ate"),
            new WordReplacement("bli", "ble"),
            new WordReplacement("ogi", "og"),
            new WordReplacement("li", string.Empty)
        });

        /// <summary>
        /// The replacements that can be made in step 3.
        /// </summary>
        private readonly Dictionary<char, WordReplacement[]> step3Replacements = CreateReplacementLookup(new[]
        {
            new WordReplacement("ational", "ate"),
            new WordReplacement("tional", "tion"),
            new WordReplacement("alize", "al"),
            new WordReplacement("icate", "ic"),
            new WordReplacement("iciti", "ic"),
            new WordReplacement("ative", string.Empty),
            new WordReplacement("ical", "ic"),
            new WordReplacement("ness", string.Empty),
            new WordReplacement("ful", string.Empty)
        });

        /// <summary>
        /// The replacements that can be made in step 4.
        /// </summary>
        private readonly Dictionary<char, WordReplacement[]> step4Replacements = CreateReplacementLookup(new[]
        {
            new WordReplacement("ement", string.Empty),
            new WordReplacement("ment", string.Empty),
            new WordReplacement("able", string.Empty),
            new WordReplacement("ible", string.Empty),
            new WordReplacement("ance", string.Empty),
            new WordReplacement("ence", string.Empty),
            new WordReplacement("ate", string.Empty),
            new WordReplacement("iti", string.Empty),
            new WordReplacement("ion", string.Empty),
            new WordReplacement("ize", string.Empty),
            new WordReplacement("ive", string.Empty),
            new WordReplacement("ous", string.Empty),
            new WordReplacement("ant", string.Empty),
            new WordReplacement("ism", string.Empty),
            new WordReplacement("ent", string.Empty),
            new WordReplacement("al", string.Empty),
            new WordReplacement("er", string.Empty),
            new WordReplacement("ic", string.Empty)
        });

        /// <inheritdoc />
        public string Stem(string word)
        {
            if (word == null || word.Length < 3)
            {
                return word;
            }

            var sb = new StringBuilder(word.ToLower());
            if (sb[0] == '\'')
            {
                sb.Remove(0, 1);
            }

            var exception = MatchExceptionWord(word, this.exceptions);
            if (exception != null)
            {
                return exception;
            }

            var changedYs = sb.ChangeY();
            var stemRegion = sb.ComputeStemRegion();

            this.Step0(sb);
            Step1a(sb);

            exception = MatchExceptionWord(sb.ToString(), this.exceptions2);
            if (exception != null)
            {
                return exception;
            }

            this.Step1b(sb, stemRegion);
            Step1c(sb);
            this.Step2(sb, stemRegion);
            this.Step3(sb, stemRegion);
            this.Step4(sb, stemRegion);
            Step5(sb, stemRegion);

            // Only need to call ToString again if Y's were changed to uppercase
            return changedYs ? sb.ToString().ToLower() : sb.ToString();
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

        /// <summary>
        /// The first stemming step, part A.
        /// </summary>
        /// <param name="builder">The string builder.</param>
        private static void Step1a(StringBuilder builder)
        {
            if (builder.EndsWith("sses"))
            {
                builder.Length -= 2;
            }
            else
            {
                var length = builder.Length;
                var lastChar = builder[length - 1];
                if (length > 2 &&
                    builder[length - 3] == 'i' &&
                    builder[length - 2] == 'e' &&
                    (lastChar == 'd' || lastChar == 's'))
                {
                    builder.Length -= (builder.Length > 4) ? 2 : 1;
                }
                else
                {
                    if (length > 1 &&
                        lastChar == 's' &&
                        (builder[length - 2] == 'u' || builder[length - 2] == 's'))
                    {
                        return;
                    }

                    if (lastChar == 's')
                    {
                        for (var i = 0; i < builder.Length - 2; i++)
                        {
                            if (builder.IsVowel(i))
                            {
                                builder.Length -= 1;
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The first stemming step, part C.
        /// </summary>
        /// <param name="builder">The string builder.</param>
        private static void Step1c(StringBuilder builder)
        {
            var length = builder.Length;
            if (length <= 2)
            {
                return;
            }

            var lastChar = builder[length - 1];
            if ((lastChar == 'y' || lastChar == 'Y') && !builder.IsVowel(length - 2))
            {
                builder[length - 1] = 'i';
            }
        }

        /// <summary>
        /// The fifth stemming step.
        /// </summary>
        /// <param name="builder">The string builder.</param>
        /// <param name="stemRegion">The calculated stem region for the word.</param>
        private static void Step5(StringBuilder builder, StemRegion stemRegion)
        {
            var length = builder.Length;
            if (length == 0)
            {
                return;
            }

            if (builder[length - 1] == 'e' &&
                (length - 1 >= stemRegion.R2 || (length - 1 >= stemRegion.R1 && !builder.IsShortSyllable(length - 3))))
            {
                builder.Length -= 1;
            }
            else if (builder.EndsWith("ll") && length - 1 >= stemRegion.R2)
            {
                builder.Length -= 1;
            }
        }

        /// <summary>
        /// Attempts to match an exception word.
        /// </summary>
        /// <param name="word">The word to match.</param>
        /// <param name="possibleExceptions">The possible exceptions.</param>
        /// <returns>The matched exception text to return, or null if no exceptions matched.</returns>
        private static string MatchExceptionWord(string word, IEnumerable<WordReplacement> possibleExceptions)
        {
            return (from e in possibleExceptions
                    where e.MatchWord == word
                    select e.MatchResult).FirstOrDefault();
        }

        /// <summary>
        /// The step applied before stemming begins in ernest - handles apostrophe removal.
        /// </summary>
        /// <param name="builder">The string builder.</param>
        private void Step0(StringBuilder builder)
        {
            var endsWith = builder.EndsWith(this.apostropheEnds);
            if (endsWith != null)
            {
                builder.Length -= endsWith.Length;
            }
        }

        /// <summary>
        /// The first stemming step, part B.
        /// </summary>
        /// <param name="builder">The string builder.</param>
        /// <param name="stemRegion">The calculated stem region for the word.</param>
        private void Step1b(StringBuilder builder, StemRegion stemRegion)
        {
            var replacement = builder.EndsWith(this.step1bReplacements);
            var matchedWord = replacement.MatchWord;
            if (matchedWord != null)
            {
                switch (matchedWord)
                {
                    case "eedly":
                    case "eed":
                        if (builder.Length - matchedWord.Length >= stemRegion.R1)
                        {
                            builder.ReplaceEnd(replacement);
                        }

                        break;
                    default:
                        var found = false;
                        for (var j = 0; j < builder.Length - matchedWord.Length; ++j)
                        {
                            if (builder.IsVowel(j))
                            {
                                builder.ReplaceEnd(replacement);
                                found = true;
                                break;
                            }
                        }

                        if (found)
                        {
                            if (builder.EndsWith(this.step1bAppendEEndings) != null)
                            {
                                builder.Append('e');
                            }
                            else if (builder.EndsWith(this.doubles) != null)
                            {
                                builder.Length -= 1;
                            }
                            else if (builder.IsShortWord(stemRegion))
                            {
                                builder.Append('e');
                            }
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// The second stemming step.
        /// </summary>
        /// <param name="builder">The string builder.</param>
        /// <param name="stemRegion">The calculated stem region for the word.</param>
        private void Step2(StringBuilder builder, StemRegion stemRegion)
        {
            var replacement = builder.EndsWith(this.step2Replacements);
            if (replacement.MatchWord != null && builder.Length - replacement.MatchWord.Length >= stemRegion.R1)
            {
                switch (replacement.MatchWord)
                {
                    case "ogi":
                        if (builder.Length > 3 && builder[builder.Length - replacement.MatchWord.Length - 1] == 'l')
                        {
                            builder.ReplaceEnd(replacement);
                        }

                        break;

                    case "li":
                        if (builder.Length > 1 && this.removableLiEndings.Contains(builder[builder.Length - 3]))
                        {
                            builder.Remove(builder.Length - 2, 2);
                        }

                        break;

                    default:
                        builder.ReplaceEnd(replacement);
                        break;
                }
            }
        }

        /// <summary>
        /// The third stemming step.
        /// </summary>
        /// <param name="builder">The string builder.</param>
        /// <param name="stemRegion">The calculated stem region for the word.</param>
        private void Step3(StringBuilder builder, StemRegion stemRegion)
        {
            var replacement = builder.EndsWith(this.step3Replacements);
            if (replacement.MatchWord != null && builder.Length - replacement.MatchWord.Length >= stemRegion.R1)
            {
                if (replacement.MatchWord == "ative")
                {
                    if (builder.Length - replacement.MatchWord.Length >= stemRegion.R2)
                    {
                        builder.ReplaceEnd(replacement);
                    }
                }
                else
                {
                    builder.ReplaceEnd(replacement);
                }
            }
        }

        /// <summary>
        /// The fourth stemming step.
        /// </summary>
        /// <param name="builder">The string builder.</param>
        /// <param name="stemRegion">The calculated stem region for the word.</param>
        private void Step4(StringBuilder builder, StemRegion stemRegion)
        {
            var replacement = builder.EndsWith(this.step4Replacements);
            if (replacement.MatchWord != null && builder.Length - replacement.MatchWord.Length >= stemRegion.R2)
            {
                if (replacement.MatchWord == "ion")
                {
                    if (builder.Length > 3 && this.removableIonEndings.Contains(builder[builder.Length - 4]))
                    {
                        builder.Length -= replacement.MatchWord.Length;
                    }
                }
                else
                {
                    builder.Length -= replacement.MatchWord.Length;
                }
            }
        }
    }
}
