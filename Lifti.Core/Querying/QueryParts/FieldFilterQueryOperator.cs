using System;
using System.Linq;

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

        public IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator)
        {
            return new IntermediateQueryResult(
                this.Statement.Evaluate(navigatorCreator)
                    .Matches.Select(m => new QueryWordMatch(
                        m.ItemId, 
                        m.FieldMatches.Where(fm => fm.FieldId == this.FieldId)))
                    .Where(m => m.FieldMatches.Count > 0));
        }

        public override string ToString()
        {
            return $"{this.FieldName}={this.Statement}";
        }
    }
}
