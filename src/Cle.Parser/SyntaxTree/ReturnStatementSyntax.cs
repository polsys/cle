using Cle.Common;
using JetBrains.Annotations;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Syntax tree node for a return statement with either an expression or void value.
    /// </summary>
    public sealed class ReturnStatementSyntax : StatementSyntax
    {
        /// <summary>
        /// Gets the expression that is returned.
        /// Null for void returns.
        /// </summary>
        [CanBeNull]
        public SyntaxNode ResultExpression { get; }

        public ReturnStatementSyntax(
            [CanBeNull] SyntaxNode expression,
            TextPosition position)
            : base(position)
        {
            ResultExpression = expression;
        }
    }
}
