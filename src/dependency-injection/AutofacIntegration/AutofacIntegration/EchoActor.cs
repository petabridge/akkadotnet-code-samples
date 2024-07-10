// -----------------------------------------------------------------------
//  <copyright file="TestActor.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2024 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Event;
using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace AutoFacIntegration;

public class EchoActor: UntypedActor
{
    private readonly ILoggingAdapter _log;
    private readonly IServiceProvider _provider;
    private readonly AutofacInjected _injected;
    private readonly ILifetimeScope _autofacScope;
    
    public EchoActor(IServiceProvider provider, AutofacInjected injected, ILifetimeScope autofacScope)
    {
        _provider = provider;
        _injected = injected;
        _autofacScope = autofacScope;
        _log = Context.GetLogger();
    }
    
    protected override void OnReceive(object message)
    {
        _log.Info("Received {0}", message);
        Sender.Tell(message);
    }

    protected override void PreStart()
    {
        base.PreStart();
        var fromProvider = _provider.GetRequiredService<AutofacInjected>();
        var fromContainer = _autofacScope.Resolve<AutofacInjected>();
        _log.Info(
            "TestActor started. Injected = {0}, from IServiceProvider = {1}, from IContainer = {2}",
            _injected.TestString,
            fromProvider.TestString,
            fromContainer.TestString);
    }
}