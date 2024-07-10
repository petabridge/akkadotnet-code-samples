// -----------------------------------------------------------------------
//  <copyright file="TestActor.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2024 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Event;
using Akka.Hosting;

namespace AutoFacIntegration;

internal sealed class SendMessage
{
    public static readonly SendMessage Instance = new();
    private SendMessage() { }
}

public class TestActor: ReceiveActor, IWithTimers
{
    private const string Message = "hello";
    private readonly ILoggingAdapter _log;
    private readonly IActorRef _echoActor;
    
    public TestActor(IRequiredActor<EchoActor> requiredActor)
    {
        _log = Context.GetLogger();
        _echoActor = requiredActor.ActorRef;

        Receive<SendMessage>(_ =>
        {
            _log.Info($"Sending ping: {Message}");
            _echoActor.Tell(Message);
        });

        Receive<string>(msg =>
        {
            _log.Info($"Received pong: {msg}");
        });
    }

    protected override void PreStart()
    {
        base.PreStart();
        Timers.StartPeriodicTimer(SendMessage.Instance, SendMessage.Instance, TimeSpan.FromSeconds(5));
    }

    public ITimerScheduler Timers { get; set; } = null!;
}