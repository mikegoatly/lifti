using System;
using System.Collections.Generic;
using System.Linq;
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

        public ProcessingWord(ReadOnlySpan<char> word)
        {
            this.Word = word;
            this.changedY = false;
            this.MaterializedEdit = null;
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
            get { return this.MaterializedEdit?.Length ?? this.Word.Length; }
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
            if (replacement.MatchResult.Length == 0)
            {
                this.Length -= replacement.MatchWord.Length;
            }
            else
            {
                this.EnsureMaterialized();
                this.Length -= replacement.MatchWord.Length;
                this.MaterializedEdit.Append(replacement.MatchResult);
            }
        }

        public void Append(char letter)
        {
            this.EnsureMaterialized();
            this.MaterializedEdit.Append(letter);
        }

        public void Remove(int startIndex, int length)
        {
            this.EnsureMaterialized();
            this.MaterializedEdit.Remove(startIndex, length);
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
            var length = this.Length;
            if (length < substring.Length)
            {
                return false;
            }

            for (int i = length - substring.Length, j = 0; i < length; i++, j++)
            {
                if (this[i] != substring[j])
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
        public WordReplacement EndsWith(Dictionary<char, WordReplacement[]> replacementSetLookup)
        {
            WordReplacement[] potentialReplacements;
            if (this.Length > 0 &&
                replacementSetLookup.TryGetValue(this[this.Length - 1], out potentialReplacements))
            {
                return this.EndsWith(potentialReplacements, p => p.MatchWord);
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
            string[] potentialSubstrings;
            if (this.Length > 0 &&
                substringLookup.TryGetValue(this[this.Length - 1], out potentialSubstrings))
            {
                return this.EndsWith(potentialSubstrings, p => p);
            }

            return null;
        }

        /// <summary>
        /// Tests if any of the given substrings appear at the end of the string builder. Note that the character at the end of the
        /// match texts will already have been tested prior to entry of this method, so will not be tested again.
        /// </summary>
        /// <typeparam name="TMatch">The type of the match.</typeparam>
        /// <param name="potentialMatches">The potential matches to test.</param>
        /// <param name="matchText">The delegate capable of reading out the text to match from the match type.</param>
        /// <returns>
        /// The substring that was matched at the end of the string builder, or null if no matches were found.
        /// </returns>
        private TMatch EndsWith<TMatch>(IEnumerable<TMatch> potentialMatches, Func<TMatch, string> matchText)
        {
            var length = this.Length;
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
                    if (this[i] != test[j])
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
            if (replacement.MatchWord != null && word.Length - replacement.MatchWord.Length >= stemRegion.R1)
            {
                switch (replacement.MatchWord)
                {
                    case "OGI":
                        if (word.Length > 3 && word[word.Length - replacement.MatchWord.Length - 1] == 'L')
                        {
                            word.ReplaceEnd(replacement);
                        }

                        break;

                    case "LI":
                        if (word.Length > 1 && this.removableLiEndings.Contains(word[word.Length - 3]))
                        {
                            // TODO Isn't this just removing the last two characters, e.g. Length - 2? That would prevent an allocation
                            word.Remove(word.Length - 2, 2);
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
