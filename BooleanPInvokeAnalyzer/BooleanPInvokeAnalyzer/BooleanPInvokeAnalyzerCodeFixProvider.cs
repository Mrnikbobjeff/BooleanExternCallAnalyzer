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
using Microsoft.CodeAnalysis.Editing;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Formatting;

namespace BooleanPInvokeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BooleanPInvokeAnalyzerCodeFixProvider)), Shared]
    public class BooleanPInvokeAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string title = "Make uppercase";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(BooleanPInvokeAnalyzerAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach(var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;
                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedSolution: c => AddMarshalAsAttribute(context.Document, declaration, c),
                        equivalenceKey: title),
                    diagnostic);
            }
        }

        private async Task<Solution> AddMarshalAsAttribute(Document document, MethodDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var argumentSyntax = SyntaxFactory.AttributeArgument(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, 
                    SyntaxFactory.IdentifierName(
                       @"UnmanagedType"),
                   SyntaxFactory.IdentifierName(
                       @"Bool")));
            var seperatedList = new SeparatedSyntaxList<AttributeArgumentSyntax>().Add(argumentSyntax);
            var argumentListSyntax = SyntaxFactory.AttributeArgumentList(seperatedList);
            var newAttribute = SyntaxFactory.Attribute(
                SyntaxFactory.ParseName("MarshalAsAttribute"), argumentListSyntax);
            var attributeSeparatedList = new SeparatedSyntaxList<AttributeSyntax>().Add(newAttribute);
            var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            foreach(var par in typeDecl.ParameterList.Parameters)
            {
                if (par.Type is PredefinedTypeSyntax predefined
                    && predefined.Keyword.Kind() == SyntaxKind.BoolKeyword
                    && !par.AttributeLists.SelectMany(al => al.Attributes).Any(att => att.Name is IdentifierNameSyntax id && id.Identifier.ValueText.Equals("MarshalAs")))
                    documentEditor.ReplaceNode(par, par.WithAttributeLists(
                        new SyntaxList<AttributeListSyntax>().Add(SyntaxFactory.AttributeList(attributeSeparatedList))));
            }

            var newDocument = documentEditor.GetChangedDocument();
            var formatted = await Formatter.FormatAsync(newDocument, await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
            return document.Project.Solution.WithDocumentSyntaxRoot(document.Id, await formatted.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false));
        }
    }
}
