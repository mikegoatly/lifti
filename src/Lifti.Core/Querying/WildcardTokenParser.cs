using Lifti.Querying.QueryParts;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lifti.Querying
{
    internal static class WildcardQueryPartParser
    {
        public static bool TryParse(ReadOnlySpan<char> token, [NotNullWhen(true)] out WildcardQueryPart? part)
        {
            List<WildcardQueryFragment>? fragments = null;
            void AddFragment(WildcardQueryFragment fragment)
            {
                fragments ??= new List<WildcardQueryFragment>();
                fragments.Add(fragment);
            }

            int? leadingTextIndex = null;
            for (var i = 0; i < token.Length; i++)
            {
                var character = token[i];

                switch (character)
                {
                    case '*':
                        if (leadingTextIndex != null)
                        {
                            AddFragment(WildcardQueryFragment.CreateText(token.Slice(leadingTextIndex.GetValueOrDefault(), i).ToString()));
                        }

                        AddFragment(WildcardQueryFragment.MultiCharacter);
                        break;
                    case '%':
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
                part = new WildcardQueryPart(fragments);
                return true;
            }

            part = default;
            return false;
        }

    }
}
