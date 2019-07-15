using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AnalyzerFix)), Shared]
    public class AnalyzerFix : CodeFixProvider
    {
        private const string Title = "Multiplicate";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AnalyzerAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var varDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: c => AggregateMultiplication(context.Document, varDeclaration, c),
                    equivalenceKey: Title),
                diagnostic);
        }

        private async Task<Document> AggregateMultiplication(Document document, LocalDeclarationStatementSyntax varDecl, CancellationToken cancellationToken)
        {
            try
            {
                var multiplication = varDecl.DescendantNodes().OfType<BinaryExpressionSyntax>().First();
                var left = int.Parse(multiplication.Left.GetText().ToString());
                var right = int.Parse(multiplication.Right.GetText().ToString());

                var updated = varDecl.Declaration.Variables.First().Initializer.Update(varDecl.Declaration.Variables.First().Initializer.EqualsToken, 
                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(left * right)));

                var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
                var newRoot = oldRoot.ReplaceNode(varDecl.Declaration.Variables.First().Initializer, updated);
                return document.WithSyntaxRoot(newRoot);
            }
            catch (Exception e)
            {
                return document;
            }
        }
    }
}