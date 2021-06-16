# NServiceBus.NewRelic.Analyzer

[![Build status](https://ci.appveyor.com/api/projects/status/32r7s2skrgm9ubva/branch/master?svg=true)](https://ci.appveyor.com/project/TraGicCode/nservicebus-newrelic-analyzer)
[![Nuget](https://img.shields.io/nuget/v/NServiceBus.NewRelic.Analyzer)](https://www.nuget.org/packages/NServiceBus.NewRelic.Analyzer)
[![Nuget downloads](https://img.shields.io/nuget/dt/NServiceBus.NewRelic.Analyzer)](https://www.nuget.org/packages/NServiceBus.NewRelic.Analyzer)
[![License](https://img.shields.io/github/license/tragiccode/nservicebus.newrelic.analyzer.svg)](https://github.com/tragiccode/nservicebus.newrelic.analyzer/blob/master/LICENSE)

#### Table of Contents

1. [Description](#description)
1. [Why do you need this?](#why-do-you-need-this)
1. [How it works](#how-it-works)
1. [Limitations](#limitations)
1. [Build Agent Requirements](#build-agent-requirements)
1. [Development - Guide for contributing to the module](#contributing)

## Description

A Roslyn Diagnostic Analyzer and corresponding Code Fix identify and automatically add NewRelic.Agent.Api [Transaction] attributes to all NServiceBus v6+ related message handlers.
Available via NuGet at https://www.nuget.org/packages/NServiceBus.NewRelic.Analyzer

## Why do you need this

The .NET NewRelic agent automatically instruments certain application frameworks ( as indicated https://docs.newrelic.com/docs/agents/net-agent/getting-started/net-agent-compatibility-requirements-net-framework/#messaging ).  NServiceBus
was one of these.  Unfortunately, when NServiceBus rewrote their whole message handling pipeline to ensure async/await was a first class citizen, this broke the automatic instrumentation that the NewRelic engineers wrote. This means that when upgrading NServiceBus to V6+ NewRelic will no longer create transactions for you.

The goal of this Analyzer is to have a reasonable workaround until the below feature request/issue is resolved by the NewRelic engineers. 

NewRelic Feature Request:
https://discuss.newrelic.com/t/feature-idea-add-support-for-nservicebus-version-6/44006

## How it works

Just like you should be monitoring all web requests to your application, you should be doing the same for all incoming messages to your NServiceBus endpoint.  Below shows the code before and after the code fix is applied from this analyzer.

## Message Handlers

### Before

```c#
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
}
```

### After

```c#
using System.Threading.Tasks;
using NServiceBus;
using NewRelic.Api.Agent;

namespace NServiceBus.OrderingEndpoint.Handlers
{
    [NewRelic.Api.Agent.Transaction]
    public class CreateOrderHandler : IHandleMessages<object>
    {
        public async Task Handle(object message, IMessageHandlerContext context)
        {
            await Task.Delay(2000);
        }
    }
}
```

## Sagas & Saga Timeouts

### Before

```c#
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

        public Task Timeout(double state, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        public Task Handle(int message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }
    
    public class SagaData : ContainSagaData {}
}
```

### After

```c#
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

        [NewRelic.Api.Agent.Transaction]
        public Task Handle(object message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
        
        [NewRelic.Api.Agent.Transaction]
        public Task Timeout(double state, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }

        [NewRelic.Api.Agent.Transaction]
        public Task Handle(int message, IMessageHandlerContext context)
        {
            throw new NotImplementedException();
        }
    }
    
    public class SagaData : ContainSagaData {}
}
```

## Limitations

- Only the code within your handler is part of the transaction
   - Behaviors and parts of the pipeline that happen before and after your handler is invoked are not included as part of the NewRelic transaction.


## Build Agent Requirements

In order to have the roslyn analyzer show up was warnings/errors as part of your build you will need to ensure the following:

1. At least Visual Studio 2019 build tools are installed.
2. Run your build using msbuild v16.0 ( installed from step 1 )

The build will either not show any analyzer warnings OR you will receive the following error on warning on build.

```c#
CSC : warning CS8032: An instance of analyzer NServiceBus.NewRelic.Analyzer.NServiceBusNewRelicAnalyzer cannot be created from C:\BuildAgent\work\9b7caaa76a57f65c\packages\NServiceBus.NewRelic.Analyzer.1.0.1\analyzers\dotnet\cs\NServiceBus.NewRelic.Analyzer.dll : 
Could not load file or assembly 'Microsoft.CodeAnalysis, Version=3.3.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35' or one of its dependencies. The system cannot find the file specified.. 
[C:\BuildAgent\work\9b7caaa76a57f65c\TestAnalysisBuild\TestAnalysisBuild.csproj]
```

## Contributing

1. Fork it ( <https://github.com/tragiccode/NServiceBus.NewRelic.Analyzer/fork> )
1. Create your feature branch (`git checkout -b my-new-feature`)
1. Commit your changes (`git commit -am 'Add some feature'`)
1. Push to the branch (`git push origin my-new-feature`)
1. Create a new Pull Request