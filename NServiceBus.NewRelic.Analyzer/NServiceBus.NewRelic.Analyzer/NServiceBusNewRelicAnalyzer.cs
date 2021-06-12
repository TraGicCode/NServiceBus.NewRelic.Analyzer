using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NServiceBus.NewRelic.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NServiceBusNewRelicAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "NSBNR0001";

        private static readonly string Title =
            "Use NewRelic [Transaction] Attribute on NServiceBus Message Handler";

        private static readonly string MessageFormat =
            "Consider using NewRelic [Transaction] Attribute on the NServiceBus Message Handler";

        private static readonly string Description =
            "NewRelic doesn't automatically instrument with NServiceBus v6+ message handlers as transactions. The best thing we can do as of now is add NewRelic [Transaction] Attributes.";

        private const string Category = "Monitoring";

        private static string[] MessageHandlerInterfaces = new[]
        {
            "IHandleMessages",
            "IHandleTimeouts",
            "IAmStartedByMessages"
        };

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;
            if (namedTypeSymbol.Interfaces.Any(x => MessageHandlerInterfaces.Contains(x.Name) && x.TypeArguments.Length == 1))
            {
                // Climb up inheritance hiearchy
                // Ex: IAmStartedByMessages : IHandleMessages
                var iHandleMessageInterfacesICareAbout = namedTypeSymbol.AllInterfaces.Where(x => MessageHandlerInterfaces.Contains(x.Name) && x.TypeArguments.Length == 1)
                    .ToList();

                foreach (var iHandleMesageInterface in iHandleMessageInterfacesICareAbout)
                {
                    var members = iHandleMesageInterface.GetMembers();
                    if (members.Any())
                    {
                        var handleMethod = namedTypeSymbol
                            .FindImplementationForInterfaceMember(members
                                .OfType<IMethodSymbol>()
                                .Single());
                        if (handleMethod.GetAttributes().Count(x => x.AttributeClass.ToString() == "NewRelic.Api.Agent.TransactionAttribute") == 0)
                        {
                            var diagnostic = Diagnostic.Create(Rule, handleMethod.Locations[0], namedTypeSymbol.Name);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }
}
