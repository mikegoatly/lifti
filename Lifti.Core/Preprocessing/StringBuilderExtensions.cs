using System.Text;

namespace Lifti.Preprocessing
{
    public static class StringBuilderExtensions
    {
        public static StringBuilder Append(this StringBuilder builder, PreprocessedInput preprocessedInput)
        {
            if (preprocessedInput.Replacement == null)
            {
                builder.Append(preprocessedInput.Value);
            }
            else
            {
                builder.Append(preprocessedInput.Replacement);
            }

            return builder;
        }

        public static bool SequenceEqual(this StringBuilder builder, string chars)
        {
            if (chars.Length != builder.Length)
            {
                return false;
            }

            for (var i = 0; i < chars.Length; i++)
            {
                if (chars[i] != builder[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
