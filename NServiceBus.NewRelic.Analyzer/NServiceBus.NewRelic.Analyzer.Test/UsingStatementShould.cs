using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NServiceBus.NewRelic.Analyzer.Test.Extensions;
using VerifyCS = NServiceBus.NewRelic.Analyzer.Test.CSharpCodeFixVerifier<
    NServiceBus.NewRelic.Analyzer.NServiceBusNewRelicAnalyzer,
    NServiceBus.NewRelic.Analyzer.NServiceBusNewRelicAnalyzerCodeFixProvider>;

namespace NServiceBus.NewRelic.Analyzer.Test
{
    [TestClass]
    public class UsingStatementShould
    {
        [TestMethod]
        public async Task NotAddUsingStatementIfItExistsInFile()
        {
            var test = 
@"
using System.Threading.Tasks;
using NServiceBus;
using NewRelic.Api.Agent;

namespace NServiceBus.OrderingEndpoint.Handlers
{
    public class CreateOrderHandler : IHandleMessages<object>
    {
        public async Task Handle(object message, IMessageHandlerContext context)
        {
            await Task.Delay(2000);
        }
    }
}";
            var expected = 
@"
using System.Threading.Tasks;
using NServiceBus;
using NewRelic.Api.Agent;

namespace NServiceBus.OrderingEndpoint.Handlers
{
    public class CreateOrderHandler : IHandleMessages<object>
    {
        [Transaction]
        public async Task Handle(object message, IMessageHandlerContext context)
        {
            await Task.Delay(2000);
        }
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Default.AddPackages(ImmutableArray.Create(new PackageIdentity("NServiceBus", "7.4.7"), new PackageIdentity("NewRelic.Agent.Api", "8.39.2"))),
                TestCode  = test.NormalizeLineEndings(),
                FixedCode = expected.NormalizeLineEndings(),
                ExpectedDiagnostics = { VerifyCS.Diagnostic(NServiceBusNewRelicAnalyzer.DiagnosticId).WithSpan(10, 27, 10, 33).WithArguments("CreateOrderHandler") }
            }.RunAsync();
        }
        
        [TestMethod]
        public async Task AddUsingStatementIfDoesntExistsInFile()
        {
            var test = 
@"
using System.Threading.Tasks;
using NServiceBus;

namespace NServiceBus.OrderingEndpoint.Handlers
{
    public class CreateOrderHandler : IHandleMessages<object>
    {
        public async Task Handle(object message, IMessageHandlerContext context)
        {
            await Task.Delay(2000);
        }
    }
}";
            var expected = 
@"
using System.Threading.Tasks;
using NServiceBus;
using NewRelic.Api.Agent;

namespace NServiceBus.OrderingEndpoint.Handlers
{
    public class CreateOrderHandler : IHandleMessages<object>
    {
        [Transaction]
        public async Task Handle(object message, IMessageHandlerContext context)
        {
            await Task.Delay(2000);
        }
    }
}";

            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.Default.AddPackages(ImmutableArray.Create(new PackageIdentity("NServiceBus", "7.4.7"), new PackageIdentity("NewRelic.Agent.Api", "8.39.2"))),
                TestCode  = test.NormalizeLineEndings(),
                FixedCode = expected.NormalizeLineEndings(),
                ExpectedDiagnostics = { VerifyCS.Diagnostic(NServiceBusNewRelicAnalyzer.DiagnosticId).WithSpan(9, 27, 9, 33).WithArguments("CreateOrderHandler") }
            }.RunAsync();
            
        }
        
//         [TestMethod]
//         public async Task NotAddUsingAndFullyQualifyTransactionAttributeWhenTypeConflictExists()
//         {
//             var test = @"
// using System;
// using System.Threading.Tasks;
// using NServiceBus;
//
// namespace NServiceBus.OrderingEndpoint.Handlers
// {
//     public class CreateOrderHandler : IHandleMessages<object>
//     {
//         public async Task Handle(object message, IMessageHandlerContext context)
//         {
//             await Task.Delay(2000);
//         }
//     }
//
//     [System.AttributeUsage(AttributeTargets.Method)]  
//     public class TransactionAttribute : System.Attribute  
//     {
//     }
// }";
//             var expected = @"
// using System;
// using System.Threading.Tasks;
// using NServiceBus;
//
// namespace NServiceBus.OrderingEndpoint.Handlers
// {
//     public class CreateOrderHandler : IHandleMessages<object>
//     {
//         [NewRelic.Api.Agent.Transaction]
//         public async Task Handle(object message, IMessageHandlerContext context)
//         {
//             await Task.Delay(2000);
//         }
//     }
//
//     [System.AttributeUsage(AttributeTargets.Method)]  
//     public class TransactionAttribute : System.Attribute  
//     {
//     }
// }";
//
//             await new VerifyCS.Test
//             {
//                 ReferenceAssemblies = ReferenceAssemblies.Default.AddPackages(ImmutableArray.Create(new PackageIdentity("NServiceBus", "7.4.7"), new PackageIdentity("NewRelic.Agent.Api", "8.39.2"))),
//                 TestState = { Sources = { test } },
//                 FixedCode = expected,
//                 ExpectedDiagnostics = { VerifyCS.Diagnostic(NServiceBusNewRelicAnalyzer.DiagnosticId).WithSpan(10, 27, 10, 33).WithArguments("CreateOrderHandler") }
//             }.RunAsync();
//         }
    }
}