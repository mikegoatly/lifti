using System;
using System.Globalization;

namespace Lifti
{
    public class CaseInsensitiveAccentSensitivePreprocessor : TextPreprocessor
    {
        private readonly CultureInfo cultureInfo;

        public CaseInsensitiveAccentSensitivePreprocessor(CultureInfo cultureInfo)
        {
            this.cultureInfo = cultureInfo;
        }

        protected override string PreprocessInput(string input)
        {
            return input.ToUpper(this.cultureInfo);
        }
    }
}
