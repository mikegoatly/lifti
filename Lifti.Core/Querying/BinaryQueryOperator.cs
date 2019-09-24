using System;

namespace Lifti.Querying
{
    /// <summary>
    /// An abstract class representing a binary query operator, e.g. an AND or an OR operator.
    /// </summary>
    public abstract class BinaryQueryOperator : IBinaryQueryOperator
    {
        protected BinaryQueryOperator(IQueryPart left, IQueryPart right)
        {
            this.Left = left;
            this.Right = right;
        }

        public IQueryPart Left
        {
            get;
            set;
        }

        public IQueryPart Right
        {
            get;
            set;
        }

        public abstract OperatorPrecedence Precedence
        {
            get;
        }

        public abstract IntermediateQueryResult Evaluate(Func<IIndexNavigator> navigatorCreator);
    }

}
