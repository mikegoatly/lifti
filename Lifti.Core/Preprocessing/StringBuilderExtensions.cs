using System.Text;

namespace Lifti
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
    }
}
