using System;

namespace Lifti.Querying.QueryParts
{
    /// <summary>
    /// An <see cref="IQueryPart"/> that restricts the resulting item matches to only those 
    /// that include matching tokens in a specific field.
    /// </summary>
    public class FieldFilterQueryOperator : IQueryPart
    {
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
        public IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, IQueryContext queryContext)
        {
            return this.Statement.Evaluate(
                    navigatorCreator,
                    QueryContext.Create(queryContext, this.FieldId));
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.FieldName}={this.Statement}";
        }
    }
}
