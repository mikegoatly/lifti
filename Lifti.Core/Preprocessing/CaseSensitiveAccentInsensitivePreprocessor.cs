using System.Globalization;
using System.Text;

namespace Lifti
{
    public class CaseSensitiveAccentInsensitivePreprocessor : TextPreprocessor
    {
        public CaseSensitiveAccentInsensitivePreprocessor()
        {
        }

        protected override string PreprocessInput(string input)
        {
            var stFormD = input.Normalize(NormalizationForm.FormD);
            var len = stFormD.Length;
            var sb = new StringBuilder(input.Length);

            for (var i = 0; i < len; i++)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(stFormD[i]);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(stFormD[i]);
                }
            }

            return (sb.ToString().Normalize(NormalizationForm.FormC));
        }
    }
}
