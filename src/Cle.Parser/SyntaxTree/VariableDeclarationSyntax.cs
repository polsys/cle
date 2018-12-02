using Cle.Common;
using JetBrains.Annotations;

namespace Cle.Parser.SyntaxTree
{
    /// <summary>
    /// Syntax tree node for a variable declaration.
    /// </summary>
    public sealed class VariableDeclarationSyntax : StatementSyntax
    {
        /// <summary>
        /// Gets the declared type of the variable.
        /// </summary>
        [NotNull]
        public string TypeName { get; }

        /// <summary>
        /// Gets the name of the variable.
        /// </summary>
        [NotNull]
        public string Name { get; }

        /// <summary>
        /// Gets the initial value of the variable.
        /// </summary>
        [NotNull]
        public ExpressionSyntax InitialValueExpression { get; }

        public VariableDeclarationSyntax(
            [NotNull] string typeName,
            [NotNull] string name,
            [NotNull] ExpressionSyntax initialValue,
            TextPosition position)
            : base(position)
        {
            TypeName = typeName;
            Name = name;
            InitialValueExpression = initialValue;
        }
    }
}
