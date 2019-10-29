using System.Collections.Generic;

namespace Lifti
{
    internal interface IItemTokenizationOptions
    {
        IEnumerable<IFieldTokenizationOptions> GetConfiguredFields();
    }
}
