namespace Lifti.Querying
{
    public interface IBinaryQueryOperator : IQueryPart
    {
        IQueryPart Left { get; set; }

        IQueryPart Right { get; set; }

        OperatorPrecedence Precedence { get; }
    }
}
