using Cle.Common;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Syntax tree node for a variable assignment.
    /// </summary>
    public sealed class AssignmentSyntax : StatementSyntax
    {
        /// <summary>
        /// Gets the identifier that is the destination of this assignment.
        /// </summary>
        public IdentifierSyntax Variable { get; }

        /// <summary>
        /// Gets the new value of the variable.
        /// </summary>
        public ExpressionSyntax Value { get; }

        public AssignmentSyntax(
            IdentifierSyntax variable,
            ExpressionSyntax value,
            TextPosition position)
            : base(position)
        {
            Variable = variable;
            Value = value;
        }
    }
}
