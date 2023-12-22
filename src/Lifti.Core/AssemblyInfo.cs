using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Lifti.Tests")]
[assembly: InternalsVisibleTo("PerformanceProfiling")]

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Required for use of C# 9 records with netstandard2.
    /// </summary>
    public class IsExternalInit { }
}