using System;
using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace VersionAnalyzer
{
   [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(VersionAnalyzerCodeFixProvider)), Shared]
   public class VersionAnalyzerCodeFixProvider : CodeFixProvider
   {
      private const string title = "Add missing Digits";

      public sealed override ImmutableArray<string> FixableDiagnosticIds
      {
         get { return ImmutableArray.Create(VersionAnalyzerAnalyzer.DiagnosticId); }
      }

      public sealed override FixAllProvider GetFixAllProvider()
      {
         // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
         return WellKnownFixAllProviders.BatchFixer;
      }

      public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
      {
         var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

         // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
         var diagnostic = context.Diagnostics.First();
         var diagnosticSpan = diagnostic.Location.SourceSpan;

         // Find the type declaration identified by the diagnostic.
         var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ObjectCreationExpressionSyntax>().First();

         // Register a code action that will invoke the fix.
         context.RegisterCodeFix(
             CodeAction.Create(
                 title: title,
                 createChangedDocument: c => AddMissingDigits(context.Document, declaration, c),
                 equivalenceKey: title),
             diagnostic);
      }

      private async Task<Document> AddMissingDigits(Document document, ObjectCreationExpressionSyntax objectCreationExpressionSyntax, CancellationToken cancellationToken)
      {
         var existingArgumentList = objectCreationExpressionSyntax.ArgumentList;
         var newArgumentsList = SyntaxFactory.ArgumentList(existingArgumentList.OpenParenToken, existingArgumentList.Arguments, existingArgumentList.CloseParenToken);

         for (var argumentIndex = newArgumentsList.Arguments.Count; argumentIndex < 4; argumentIndex++)
         {
            var newArgument = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                  SyntaxFactory.Literal(0)));
            newArgumentsList = newArgumentsList.AddArguments(newArgument);
         }

         var newObjectCreationExpressionSyntax = objectCreationExpressionSyntax.WithArgumentList(newArgumentsList);
         var root = await document.GetSyntaxRootAsync();

         var newRoot = root.ReplaceNode(objectCreationExpressionSyntax, newObjectCreationExpressionSyntax);

         return document.WithSyntaxRoot(newRoot);
      }
   }
}
