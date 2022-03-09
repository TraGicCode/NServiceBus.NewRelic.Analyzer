using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Entia.Analyze;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NServiceBus.NewRelic.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NServiceBusNewRelicAnalyzerCodeFixProvider)), Shared]
    public class NServiceBusNewRelicAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(NServiceBusNewRelicAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedSolution: c => AddCustomAttribute(context.Document, declaration, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private async Task<Solution> AddCustomAttribute(Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var model = await document.GetSemanticModelAsync(cancellationToken);
            var attributes = methodDeclaration.AttributeLists.Add(
                SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                    SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Transaction"))
                )));
            var newRoot = root.ReplaceNode(
                methodDeclaration,
                methodDeclaration.WithAttributeLists(attributes));

            if (RequiresUsing(model, root))
            {
                if(newRoot is CompilationUnitSyntax compilationUnitSyntax)
                {
                    newRoot = compilationUnitSyntax.AddUsings(CreateUsing());
                }
            }

            return document.WithSyntaxRoot(
                newRoot).Project.Solution;
        }
        

        bool RequiresUsing(SemanticModel model, SyntaxNode root) =>
                model.Compilation.GlobalNamespace?.Namespace("NewRelic")?.Namespace("Api")?.Namespace("Agent") is INamespaceSymbol @namespace &&
                root.DescendantNodes().OfType<UsingDirectiveSyntax>().All(@using => !model.GetSymbolInfo(@using.Name).Symbol.Equals(@namespace, SymbolEqualityComparer.Default));


        UsingDirectiveSyntax CreateUsing() => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("NewRelic.Api.Agent"));

    }
}
