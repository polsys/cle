using System.Collections.Immutable;
using Cle.Common;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Syntax tree node for a function call expression.
    /// </summary>
    public sealed class FunctionCallSyntax : ExpressionSyntax
    {
        /// <summary>
        /// Gets the name of the called function.
        /// </summary>
        public string Function { get; }

        /// <summary>
        /// Gets the parameters in order.
        /// </summary>
        public ImmutableList<ExpressionSyntax> Parameters { get; }

        public FunctionCallSyntax(string function,
            ImmutableList<ExpressionSyntax> parameters, TextPosition position)
            : base(position)
        {
            Function = function;
            Parameters = parameters;
        }
    }
}
