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
    public class SagaShould
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task NotShowDiagnostic()
        {
            var test = @"
using System;
using System.Threading.Tasks;
using NewRelic.Api.Agent;
using NServiceBus;

namespace NServiceBus.OrderingEndpoint.Handlers
{
    public class MySaga :
        Saga<SagaData>,
        IAmStartedByMessages<object>,
        IHandleMessages<int>,
        IHandleTimeouts<double>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            throw new NotImplementedException();
        }

        [Transaction]
        public Task Handle(object message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
        [Transaction]
        public Task Timeout(double state, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
        [Transaction]
        public Task Handle(int message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }
    
    public class SagaData : ContainSagaData {}
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Default.AddPackages(
                    ImmutableArray.Create(new PackageIdentity("NServiceBus", "7.4.7"),
                        new PackageIdentity("NewRelic.Agent.Api", "8.39.2"))),
                TestState = {Sources = {test}},

            }.RunAsync();
        }
        
               
        [TestMethod]
        public async Task ShowDiagnosticForSagaIAmStartedByMessageHandlers()
        {
            var test = @"
using System;
using System.Threading.Tasks;
using NewRelic.Api.Agent;
using NServiceBus;

namespace NServiceBus.OrderingEndpoint.Handlers
{
    public class MySaga :
        Saga<SagaData>,
        IAmStartedByMessages<object>,
        IHandleMessages<int>,
        IHandleTimeouts<double>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            throw new NotImplementedException();
        }

        public Task Handle(object message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
        [Transaction]
        public Task Timeout(double state, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        [Transaction]
        public Task Handle(int message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }
    
    public class SagaData : ContainSagaData {}
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Default.AddPackages(ImmutableArray.Create(new PackageIdentity("NServiceBus", "7.4.7"), new PackageIdentity("NewRelic.Agent.Api", "8.39.2"))),
                TestState = { Sources = { test } },
                ExpectedDiagnostics =
                {
                    VerifyCS.Diagnostic(NServiceBusNewRelicAnalyzer.DiagnosticId).WithSpan(20, 21, 20, 27).WithArguments("MySaga")
                }

            }.RunAsync();
        }
        
        [TestMethod]
        public async Task ShowDiagnosticForSagaTimeoutHandlers()
        {
            var test = @"
using System;
using System.Threading.Tasks;
using NewRelic.Api.Agent;
using NServiceBus;

namespace NServiceBus.OrderingEndpoint.Handlers
{
    public class MySaga :
        Saga<SagaData>,
        IAmStartedByMessages<object>,
        IHandleMessages<int>,
        IHandleTimeouts<double>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            throw new NotImplementedException();
        }

        [Transaction]
        public Task Handle(object message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        public Task Timeout(double state, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        [Transaction]
        public Task Handle(int message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }
    
    public class SagaData : ContainSagaData {}
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Default.AddPackages(ImmutableArray.Create(new PackageIdentity("NServiceBus", "7.4.7"), new PackageIdentity("NewRelic.Agent.Api", "8.39.2"))),
                TestState = { Sources = { test } },
                ExpectedDiagnostics =
                {
                    VerifyCS.Diagnostic(NServiceBusNewRelicAnalyzer.DiagnosticId).WithSpan(26, 21, 26, 28).WithArguments("MySaga")
                }

            }.RunAsync();
        }
    }
}