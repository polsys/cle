using Cle.Common;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Syntax tree node for a standalone function call.
    /// This barely wraps <see cref="FunctionCallSyntax"/>.
    /// </summary>
    public sealed class FunctionCallStatementSyntax : StatementSyntax
    {
        /// <summary>
        /// Gets the proper function call expression.
        /// </summary>
        public FunctionCallSyntax Call { get; }

        public FunctionCallStatementSyntax(
            FunctionCallSyntax call,
            TextPosition position)
            : base(position)
        {
            Call = call;
        }
    }
}
