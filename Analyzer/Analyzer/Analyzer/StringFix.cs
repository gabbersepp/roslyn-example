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
using System.Collections.Generic;

namespace Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StringFix)), Shared]
    public class StringFix : CodeFixProvider
    {
        private const string Title = "Interpolate";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(AnalyzerAnalyzer.DiagnosticId2); }
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

            var binExpr = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<BinaryExpressionSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: c => ChangeToInterpolation(context.Document, binExpr, c),
                    equivalenceKey: Title),
                diagnostic);
        }

        private async Task<Document> ChangeToInterpolation(Document document, BinaryExpressionSyntax binExpr, CancellationToken cancellationToken)
        {
            try
            {
                var parent = binExpr.Parent;

                var left = binExpr.Left as LiteralExpressionSyntax;
                var right = binExpr.Right as IdentifierNameSyntax;
                var stringContent = left.Token.ValueText;
                var varContent = right.Identifier.ValueText;

                var interPolExpr = SyntaxFactory.InterpolatedStringExpression(
                    SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken)).WithContents(
                    new SyntaxList<InterpolatedStringContentSyntax>(

                    // important: Use "new array[]" syntax and NOT the list initializer, otherwise the SyntaxList does not work as expected
                    new InterpolatedStringContentSyntax[]{
                        SyntaxFactory.InterpolatedStringText()
                            .WithTextToken(SyntaxFactory.Token(SyntaxFactory.TriviaList(),
                                SyntaxKind.InterpolatedStringTextToken, stringContent, stringContent, SyntaxFactory.TriviaList())),
                        SyntaxFactory.Interpolation(SyntaxFactory.IdentifierName(varContent))
                    }));

                var oldRoot = await document.GetSyntaxRootAsync(cancellationToken);
                var newRoot = oldRoot.ReplaceNode(binExpr, interPolExpr);
                return document.WithSyntaxRoot(newRoot);
            }
            catch (Exception e)
            {
                return document;
            }
        }
    }
}
