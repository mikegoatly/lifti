using Lifti.Querying.QueryParts;
using Lifti.Tokenization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lifti.Querying
{
    internal static class WildcardQueryPartParser
    {
        public static bool TryParse(ReadOnlySpan<char> token, ITokenizer tokenizer, [NotNullWhen(true)] out WildcardQueryPart? part)
        {
            List<WildcardQueryFragment>? fragments = null;
            void AddFragment(WildcardQueryFragment fragment)
            {
                fragments ??= new List<WildcardQueryFragment>();
                fragments.Add(fragment);
            }
            
            int? leadingTextIndex = null;
            void AddPrecedingTextFragment(ReadOnlySpan<char> token, int currentIndex)
            {
                if (leadingTextIndex != null)
                {
                    var startIndex = leadingTextIndex.GetValueOrDefault();
                    AddFragment(
                        WildcardQueryFragment.CreateText(
                            tokenizer.Normalize(token.Slice(startIndex, currentIndex - startIndex))));

                    leadingTextIndex = null;
                }
            }

            for (var i = 0; i < token.Length; i++)
            {
                var character = token[i];

                switch (character)
                {
                    case '*':
                        AddPrecedingTextFragment(token, i);
                        AddFragment(WildcardQueryFragment.MultiCharacter);
                        break;
                    case '%':
                        AddPrecedingTextFragment(token, i);
                        AddFragment(WildcardQueryFragment.SingleCharacter);
                        break;
                    default:
                        if (leadingTextIndex == null)
                        {
                            leadingTextIndex = i;
                        }

                        break;
                }
            }

            if (fragments != null)
            {
                // Only add any remaining preceding text fragment if we have encountered at least one
                // wildcard fragment
                AddPrecedingTextFragment(token, token.Length);

                part = new WildcardQueryPart(fragments);
                return true;
            }

            part = default;
            return false;
        }

    }
}
