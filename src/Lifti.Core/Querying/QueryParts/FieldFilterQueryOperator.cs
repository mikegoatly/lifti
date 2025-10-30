using System;
using System.Text.RegularExpressions;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// An <see cref="IQueryPart"/> that restricts the resulting matches to only those 
    /// that include matching tokens in a specific field.
    /// </summary>
    public sealed class FieldFilterQueryOperator : IQueryPart
    {
        private static readonly char[] escapableChars = new[] { '[', ']' };

        /// <summary>
        /// Constructs a new instance of <see cref="FieldFilterQueryOperator"/>.
        /// </summary>
        public FieldFilterQueryOperator(string fieldName, byte fieldId, IQueryPart statement)
        {
            this.FieldName = fieldName;
            this.FieldId = fieldId;
            this.Statement = statement;
        }

        /// <summary>
        /// The name of the field to match.
        /// </summary>
        public string FieldName { get; }

        /// <summary>
        /// The id of the field to match.
        /// </summary>
        public byte FieldId { get; }

        /// <summary>
        /// The <see cref="IQueryPart"/> for which to restrict the results.
        /// </summary>
        public IQueryPart Statement { get; }

        /// <inheritdoc/>
        public IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, QueryContext queryContext)
        {
            ArgumentNullException.ThrowIfNull(queryContext);

            // A field filter query part doesn't actually contribute anything to timings or measurements, so we don't bother recording it.
            return this.Statement.Evaluate(
                    navigatorCreator,
                    queryContext with { FilterToFieldId = this.FieldId });
        }

        /// <inheritdoc/>
        public double CalculateWeighting(Func<IIndexNavigator> navigatorCreator)
        {
            // We're applying an additional level of filtering here, so reduce the weighting of the 
            // child statement by 50% to reflect this.
            return this.Statement.CalculateWeighting(navigatorCreator) * 0.5D;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var fieldName = this.FieldName;

            // Make sure to escape any [ or ] characters in the field name.
            if (fieldName.IndexOfAny(escapableChars) >= 0)
            {
                fieldName = Regex.Replace(fieldName, @"[\[\]]", @"\$0");
            }

            return $"[{fieldName}]={this.Statement}";
        }

        /// <summary>
        /// Creates a new <see cref="FieldFilterQueryOperator"/> instance for the field <paramref name="fieldName"/>.
        /// </summary>
        /// <param name="fieldLookup">The <see cref="IIndexedFieldLookup"/> capable of returning the information about the field 
        /// <paramref name="fieldName"/>. This will typically be <see cref="IFullTextIndex{TKey}.FieldLookup"/>.</param>
        /// <param name="fieldName">The name of the field for which to filter the statement's results to.</param>
        /// <param name="statement">The statement that should have its results filtered.</param>
        public static FieldFilterQueryOperator CreateForField(IIndexedFieldLookup fieldLookup, string fieldName, IQueryPart statement)
        {
            return new FieldFilterQueryOperator(
                fieldName,
                fieldLookup?.GetFieldInfo(fieldName).Id ?? throw new ArgumentNullException(nameof(fieldLookup)),
                statement);
        }
    }
}
