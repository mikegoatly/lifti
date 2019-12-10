using System;

namespace Lifti.Querying.QueryParts
{
    public class FieldFilterQueryOperator : IQueryPart
    {
        public FieldFilterQueryOperator(string fieldName, byte fieldId, IQueryPart statement)
        {
            this.FieldName = fieldName;
            this.FieldId = fieldId;
            this.Statement = statement;
        }

        public string FieldName { get; }

        public byte FieldId { get; }

        public IQueryPart Statement { get; }

        public IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator, IQueryContext queryContext)
        {
            return this.Statement.Evaluate(
                    navigatorCreator,
                    QueryContext.Create(queryContext, this.FieldId));
        }

        public override string ToString()
        {
            return $"{this.FieldName}={this.Statement}";
        }
    }
}
