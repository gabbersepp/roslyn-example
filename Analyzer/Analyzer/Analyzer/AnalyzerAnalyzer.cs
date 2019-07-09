using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "hopefullyunique";
        private const string Title = "numbers can be multiplicated on your own you noob and use string interpolation";
        private const string MessageFormat = "noob + string interpolation";
        private const string Description = "Fix it fast";
        private const string Category = "Usage";

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(VerifyAction, SyntaxKind.MultiplyExpression);
        }

        private void VerifyAction(SyntaxNodeAnalysisContext obj)
        {
            var expr = (BinaryExpressionSyntax)obj.Node;

            if (expr.Left is LiteralExpressionSyntax && expr.Right is LiteralExpressionSyntax)
            {
                obj.ReportDiagnostic(Diagnostic.Create(Rule, obj.Node.GetLocation()));
            }
        }

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

    }
}
