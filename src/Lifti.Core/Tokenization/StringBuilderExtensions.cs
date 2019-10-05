using System.Text;

namespace Lifti.Tokenization
{
    public static class StringBuilderExtensions
    {
        public static bool SequenceEqual(this StringBuilder builder, string chars)
        {
            if (builder is null)
            {
                throw new System.ArgumentNullException(nameof(builder));
            }

            if (chars is null)
            {
                throw new System.ArgumentNullException(nameof(chars));
            }

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
