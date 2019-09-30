namespace Lifti.Querying
{
    internal static class IntegerExtensions
    {
        internal static bool IsPositiveAndLessThanOrEqualTo(this int value, int target)
        {
            return value >= 0 && value <= target;
        }
    }
}
