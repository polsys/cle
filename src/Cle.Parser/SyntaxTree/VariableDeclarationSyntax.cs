﻿using Cle.Common;

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
        public TypeSyntax Type { get; }

        /// <summary>
        /// Gets the name of the variable.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the initial value of the variable.
        /// </summary>
        public ExpressionSyntax InitialValueExpression { get; }

        public VariableDeclarationSyntax(
            TypeSyntax type,
            string name,
            ExpressionSyntax initialValue,
            TextPosition position)
            : base(position)
        {
            Type = type;
            Name = name;
            InitialValueExpression = initialValue;
        }
    }
}
