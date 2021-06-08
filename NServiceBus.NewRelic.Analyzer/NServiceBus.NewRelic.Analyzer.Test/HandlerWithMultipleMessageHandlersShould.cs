
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = NServiceBus.NewRelic.Analyzer.Test.CSharpCodeFixVerifier<
    NServiceBus.NewRelic.Analyzer.NServiceBusNewRelicAnalyzer,
    NServiceBus.NewRelic.Analyzer.NServiceBusNewRelicAnalyzerCodeFixProvider>;

namespace NServiceBus.NewRelic.Analyzer.Test
{
    [TestClass]
    public class HandlerWithMultipleMessageHandlersShould
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task NotShowDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;
using NServiceBus;
using NewRelic.Api.Agent;

namespace NServiceBus.PipelineModification.OtherEndpoint.Handlers
{
    public class CreateOrderHandler : IHandleMessages<object>, IHandleMessages<int>
    {
        [Transaction]
        public async Task Handle(object message, IMessageHandlerContext context)
        {
            await Task.Delay(2000);
        }
        
        [Transaction]
        public async Task Handle(int message, IMessageHandlerContext context)
        {
            await Task.Delay(2000);
        }
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Default.AddPackages(ImmutableArray.Create(new PackageIdentity("NServiceBus", "7.4.7"), new PackageIdentity("NewRelic.Agent.Api", "8.39.2"))),
                TestState = { Sources = { test } },

            }.RunAsync();
        }
        
        [TestMethod]
        public async Task WhenOneHandleMethodIsMissingAttribute_ShouldShowOneDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;
using NServiceBus;
using NewRelic.Api.Agent;

namespace NServiceBus.OrderingEndpoint.Handlers
{
    public class CreateOrderHandler : IHandleMessages<object>, IHandleMessages<int>
    {
        [Transaction]
        public async Task Handle(object message, IMessageHandlerContext context)
        {
            await Task.Delay(2000);
        }

        public async Task Handle(int message, IMessageHandlerContext context)
        {
            await Task.Delay(2000);
        }
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Default.AddPackages(ImmutableArray.Create(new PackageIdentity("NServiceBus", "7.4.7"), new PackageIdentity("NewRelic.Agent.Api", "8.39.2"))),
                TestState = { Sources = { test } },
                ExpectedDiagnostics =
                {
                    VerifyCS.Diagnostic(NServiceBusNewRelicAnalyzer.DiagnosticId).WithSpan(16, 27, 16, 33).WithArguments("CreateOrderHandler")
                }

            }.RunAsync();
        }

        [TestMethod]
        public async Task WhenTwoHandleMethodsAreMissingAttributes_ShouldShowTwoDiagnostic()
        {
            var test = @"
using System.Threading.Tasks;
using NServiceBus;
using NewRelic.Api.Agent;

namespace NServiceBus.OrderingEndpoint.Handlers
{
    public class CreateOrderHandler : IHandleMessages<object>, IHandleMessages<int>
    {
        public async Task Handle(object message, IMessageHandlerContext context)
        {
            await Task.Delay(2000);
        }

        public async Task Handle(int message, IMessageHandlerContext context)
        {
            await Task.Delay(2000);
        }
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Default.AddPackages(ImmutableArray.Create(new PackageIdentity("NServiceBus", "7.4.7"), new PackageIdentity("NewRelic.Agent.Api", "8.39.2"))),
                TestState = { Sources = { test } },
                ExpectedDiagnostics =
                {
                    VerifyCS.Diagnostic(NServiceBusNewRelicAnalyzer.DiagnosticId).WithSpan(10, 27, 10, 33).WithArguments("CreateOrderHandler"),
                    VerifyCS.Diagnostic(NServiceBusNewRelicAnalyzer.DiagnosticId).WithSpan(15, 27, 15, 33).WithArguments("CreateOrderHandler")
                }

            }.RunAsync();
        }
    }
}
