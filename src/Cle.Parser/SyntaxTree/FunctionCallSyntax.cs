using System.Collections.Immutable;
using Cle.Common;
using JetBrains.Annotations;

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
        [NotNull]
        public string Function { get; }

        /// <summary>
        /// Gets the parameters in order.
        /// </summary>
        [NotNull]
        [ItemNotNull]
        public ImmutableList<ExpressionSyntax> Parameters { get; }

        public FunctionCallSyntax([NotNull] string function,
            [NotNull, ItemNotNull] ImmutableList<ExpressionSyntax> parameters, TextPosition position)
            : base(position)
        {
            Function = function;
            Parameters = parameters;
        }
    }
}
