// -----------------------------------------------------------------------
//  <copyright file="TestActor.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2024 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Event;
using Microsoft.Extensions.DependencyInjection;

namespace AutoFacIntegration;

public class EchoActor: UntypedActor
{
    private readonly ILoggingAdapter _log;
    private readonly IServiceProvider _provider;
    
    public EchoActor(IServiceProvider provider)
    {
        _provider = provider;
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
        var moduleFromProvider = _provider.GetRequiredService<AutofacInjected>();
        _log.Info(
            "TestActor started. Injected module TestString = {0}", 
            moduleFromProvider.TestString);
    }
}