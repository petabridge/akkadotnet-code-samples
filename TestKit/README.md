# Akka.NET TestKit Sample

This sample will teach you everything you need to write 99% of the Akka.NET tests that you will ever write. (You can also [read more about the `TestKit` on the Petabridge blog](https://petabridge.com/blog/archive/#fndtn-byCategory).)

## Table of Contents
- [`TestKit` core API](#akkatestkit-essentials--core-components)
- [Advanced `TestKit` usage](#advanced-testkit-components--api)
- [`TestKit` FAQs](#akkatestkit-faqs)
- [NuGet dependencies](#nuget-dependencies)
- [Running the sample](#running-the-sample)


### What Testing Frameworks Are Available?
The `TestKit` currently has support for [NUnit](https://www.nuget.org/packages/Akka.TestKit.NUnit/), [XUnit](https://www.nuget.org/packages/Akka.TestKit.Xunit/), [XUnit2](https://www.nuget.org/packages/Akka.TestKit.Xunit2/) and [VSTest](https://www.nuget.org/packages/Akka.TestKit.VsTest/).

This sample will focus on the core pieces you need to know that are shared across all of the above testing frameworks. Each framework has its own specific extensions to the core `TestKit` API (assertions, etc.). All code samples in this post use the [NUnit](https://www.nuget.org/packages/Akka.TestKit.NUnit/) package.

## Akka.TestKit Essentials / Core Components
### The `TestActor`
The `TestActor` acts as the implicit sender of all messages sent to any of your actors during unit tests. Unless you specify otherwise, the `TestActor` will be the `Sender` of all messages sent to actors in your tests.

This makes it very easy to test how actors respond to messages they are sent. Many actors reply to sender, and this reply is collected by the `TestActor` in an internal `ConcurrentQueue`.

Here's a visual comparison of how the `TestActor` acts as the implicit `Sender` and intercepts responses back to `Sender` from actors in your tests:

![Akka.NET TestActor acts as implicit sender of test messages](/TestKit/diagrams/reply-to-sender-pattern.png)

### The `TestActorSystem`
This is the built in `ActorSystem` that used inside the `TestKit`. You access it by calling [`Sys`](http://api.getakka.net/docs/stable/html/9487E60D.htm). The `Sys` `ActorSystem` gets torn down and recreated between each individual test.

<a name="morelink"></a>
Inside your tests, you call `Sys` to instantiate actors within the `TestActorSystem`, like so:

<!-- more -->

```csharp
// create an actor in the TestActorSystem
var actor = Sys.ActorOf(Props.Create(() => new MyActorClass()));
```

Create your actors within `Sys` so that your actors exist in the same `ActorSystem` as the `TestActor`. If you don't, `TestActor` won't be able to receive messages and your `ExpectMsg` calls will time out.

FYI, all actors you create in a given test won't be alive in later tests as `Sys` is rebuilt for each test.

#### A Quick End-to-End `Akka.TestKit` Example
Before going deep into the `TestKit`, I want to show you an end-to-end example of what a test usually looks like.

```csharp
// Examples/Basics.cs

[Test]
public void UserIdentityActor_should_confirm_user_creation_success()
{
    var identity = Sys.ActorOf(Props.Create(() => new UserIdentityActor()));
    identity.Tell(new UserIdentityActor.CreateUserWithValidUserInfo());
    var result = ExpectMsg<UserIdentityActor.OperationResult>().Successful;
    Assert.True(result);
}
```

80% of your tests will be this simple: create an actor, send it a message, and expect a response back. **If you learn nothing else from this post, let it be this.**


### Expecting Messages: `ExpectMsg` & `ExpectNoMsg`
You can use [`ExpectMsg`](http://api.getakka.net/docs/stable/html/F337AAA8.htm) to assert that the `TestActor` receives a message of the given type. You can also use [`ExpectNoMsg`](http://api.getakka.net/docs/stable/html/2A54EF48.htm) to assert that the `TestActor` receives no message.

`ExpectMsg` calls to a queue in the `TestActor` and returns the message at the head of that collection. You can store this message (or a property of the message) in a variable to perform extra assertions on it, e.g.:

#### `ExpectMsg` Example
```csharp
// Examples/Basics.cs

public UserIdentityActor()
{
    Receive<CreateUserWithValidUserInfo>(create =>
    {
        // create user here
        Sender.Tell(new OperationResult() { Successful = true });
    });

    Receive<CreateUserWithInvalidUserInfo>(create =>
    {
        // fail to create user here
        Sender.Tell(new OperationResult());
    });

    Receive<IndexUsers>(index =>
    {
        _log.Info("indexing users");
        // index the users
    });
}

// other code omitted for brevity...

// ExpectMsg will assert that a message of the specified type is received
// and return it, so you can store the result in a variable for further
// inspection and assertions
[Test]
public void Identity_actor_should_confirm_user_creation_success()
{
    _identity = Sys.ActorOf(Props.Create(() => new UserIdentityActor()));
    _identity.Tell(new UserIdentityActor.CreateUserWithValidUserInfo());
    var result = ExpectMsg<UserIdentityActor.OperationResult>().Successful;
    Assert.True(result);
}
```

#### `ExpectNoMsg` Example
```csharp
// Examples/Basics.cs

[Test]
public void Identity_actor_should_not_respond_to_index_messages()
{
    _identity.Tell(new UserIdentityActor.IndexUsers());
    ExpectNoMsg();
}
```

Be aware that by default, `ExpectMsg` has a 3 second timeout. You can override this by passing a `TimeSpan` to it.

### Ensure Work Completes Fast Enough: `Within`
Many systems using Akka.NET have soft realtime requirements. Developers of such systems need to build time constraints into their tests. `Within` helps you introduce the same time constraints into your test that you have in your real system.

[`Within`](http://api.getakka.net/docs/stable/html/61FEE037.htm) sets the max time allowed for its block to execute. You can nest `Within` blocks several levels deep. You can use any other assertions or `TestKit` features inside of a `Within` block.

#### `Within()` Example
```csharp
// Examples/Timing.cs

public class TimeConstrainedActor : ReceiveActor
{
    private ILoggingAdapter _log = Context.GetLogger();
    private readonly int _delay;

    public TimeConstrainedActor() : this(0) { }

    public TimeConstrainedActor(int delay)
    {
        _delay = delay; // milliseconds

        Receive<string>(s =>
        {
            Thread.Sleep(_delay);
            Sender.Tell("ok");
        });
    }
}

public class TimeConstrainedActorSpecs : TestKit
{
    [TestCase(100, 300)]
    public void Actor_should_respond_within_max_allowable_time(int delay, int cutoff)
    {
        var a = Sys.ActorOf(Props.Create(() => new TimeConstrainedActor(delay)));

        // sets a maximum allowable time for entire block to finish
        Within(TimeSpan.FromMilliseconds(cutoff), () =>
        {
            a.Tell("respond to this");
            ExpectMsg("ok");
        });
    }
}
```

## Advanced `TestKit` Components & API
Now we'll cover the advanced tools within the `TestKit` that you will use less often. What we've already covered is what you'll use 80%+ of the time and handles almost all cases.

### `BlackHoleActor`
The `BlackHoleActor` is a special-purpose actor that will not respond to anything. No matter what message you tell the `BlackHoleActor`, it will not respond.  `BlackHoleActor` is perfect for testing failure conditions. Common failure conditions are when an actor or service dies, or an operation times out.

#### Example
Let's assume that we have an `IdentityManagerActor` that creates new users.  `IdentityManagerActor` collaborates with an `AuthenticationActor` to get CreateUser requests validated. If the `AuthenticationActor` times out, the IdentityManager should fail the CreateUserRequest.

This is easy to test: all we have to do is swap the `BlackHoleActor` for the `AuthenticationActor`. Then all requests—including `CreateUser`—will time out:

#### `BlackHoleActor` Example
```csharp
// Examples/Blackhole.cs

public IdentityManagerActor(IActorRef authenticationActor)
{
    _authenticator = authenticationActor;

    Receive<CreateUser>(create =>
    {
        var senderClosure = Sender;

        // this actor needs it create user request to be authenticated
        // within 2 seconds or this operation times out & cancels
        // the Task returned by Ask<>
        _authenticator.Ask<UserResult>(create, TimeSpan.FromSeconds(2))
            .ContinueWith(tr =>
            {
                if (tr.IsCanceled || tr.IsFaulted)
                    return new UserResult(false);

                return tr.Result;
            }).PipeTo(senderClosure);
    });
}

// other code omitted for brevity...

[Test]
public void IdentityManagerActor_should_fail_create_user_on_timeout()
{
    // the BlackHoleActor will NEVER respond to any message sent to it
    // which will force the CreateUser request to time out
    var blackhole = Sys.ActorOf(BlackHoleActor.Props);
    var identity = Sys.ActorOf(Props.Create(() => new IdentityManagerActor(blackhole)));

    identity.Tell(new CreateUser());
    var result = ExpectMsg<UserResult>().Successful;
    Assert.False(result);
}
```

### Handle Racy Tests With `AwaitAssert`
[`AwaitAssert`](http://api.getakka.net/docs/stable/html/C6A1034F.htm) is a assertion that will polls on an interval until its assertions pass, or it times out. `AwaitAssert` is useful for any test where we may need to check a condition many times (e.g. a race condition). This is also good for tests where you expect a condition within a time window, but you're not sure when exactly.

#### `AwaitAssert` Example
```csharp
// Examples/Blackhole.cs

[TestCase(3, 2)]
[TestCase(4, 3)]
public void AuthenticationActor_should_verify_its_ready_within_max_time(int maxSeconds, int authDelaySeconds)
{

    // create auth actor with 5 second delay built in
    var authProps = Props.Create(() => new AuthenticationActor(TimeSpan.FromSeconds(authDelaySeconds)));
    var auth = Sys.ActorOf(authProps);

    // assert that not ready
    auth.Tell(new CheckIfReady());
    ExpectMsg(false);

    // initialize the AuthenticationActor (with delay)
    auth.Tell(new GetReady());

    // poll every 250ms until AuthenticationActor says it's ready or we time out
    AwaitAssert(() =>
    {
        auth.Tell(new CheckIfReady());
        ExpectMsg(true);
    }, TimeSpan.FromSeconds(maxSeconds), TimeSpan.FromMilliseconds(250));
}

```

### Monitoring Actor Shutdown With `Watch()`
Just as with your normal actor code, you can set up any actor in a test to `DeathWatch` another actor just by `Watch()`ing its `IActorRef`.

This is often useful to check that a certain actor is killed by application logic, to check that self-terminating actors shut down when conditions are right. Any time you need to verify that an actor shuts down in a given scenario, you can use [`Watch`](http://api.getakka.net/docs/stable/html/8147EAC1.htm) in your tests. This pair nicely with `ExpectTerminated()`, which expects a `Terminated` message for a given `IActorRef`.

For example:

```csharp
// Examples/Watch.cs

[Test]
public void UserStatsActor_should_shut_down_when_UserSessionActor_dies()
{
    // using a TestProbe as a stand-in / throwaway actor here
    var user = CreateTestProbe("user");
    var stats = Sys.ActorOf(Props.Create(() => new UserStatsActor(user.Ref)));

    // TestActor is now watching the UserStatsActor
    Watch(stats);

    // kill the UserSessionActor
    Sys.Stop(user);

    // verify that the UserStatsActor shut itself down
    ExpectTerminated(stats);
}
```

### Test for Logs & Undeliverable Messages With `EventFilter`
Here is a simple example of testing to see if an actor logs a certain message. The syntax is almost identical for checking `DeadLetters`:

```csharp
// Examples/Basics.cs

[Test]
public void IdentityActor_should_log_user_indexing_operation()
{
    EventFilter.Info("indexing users").ExpectOne(() =>
    {
        _identity.Tell(new UserIdentityActor.IndexUsers());
    });
}
```

The [`EventFilter`](http://api.getakka.net/docs/stable/html/29436B5F.htm) is a tool to verify that actors write certain log messages. It can also detect if messages go to `DeadLetters`.

By default, `EventFilter` matches are exact. You can also change the match criteria to check for messages containing a substring.

### Accessing Internal Actor State: `TestActorRef`
You can use the `TestActorRef` to access the internal state of an actor. You get a `TestActorRef` by calling the [`ActorOfAsTestActorRef`](http://api.getakka.net/docs/stable/html/E7ACA2FD.htm) method.

Be aware that the `TestActorRef` is a fully synchronous actor / unit test, so it will not play well with actor types that employ additional `async` behavior to do their jobs (e.g. the `PersistentActor` & `AtLeastOnceDeliveryActor` from `Akka.Persistence`)

Using `TestActorRef` will allow you to read internal actor state without having to send the actor a message, which is normally not possible. It will not allow you to directly modify internal actor state, or access `private` fields.

#### How do I Get a `TestActorRef`?
You call [`ActorOfAsTestActorRef`](http://api.getakka.net/docs/stable/html/E7ACA2FD.htm).

By default, `ActorOfAsTestActorRef()` will create the `TestActorRef` as a child of Sys. There is an overload to create `TestActorRef` as a child of any actor you have a reference to.

#### When to Use `TestActorRef`
Generally, you want to avoid using `TestActorRef`. Using it for general testing scenarios is NOT RECOMMENDED. Why? You want your tests to simulate the real system as much as possible. Non-test actors can't access each others internal state. And your tests should reflect this as much as possible.

In reality, if one actor wants to know the internal state of another actor then it must send that actor a message. I recommend you follow the same pattern in your tests and don't abuse the `TestActorRef`. Stick to the messaging model in your tests that you actually use in your application.

That said, the `TestActorRef` has its uses. Such as verifying internal actor state or the side effects of an operation. This usually comes up when you need to verify data that you won't expose via a message contract.

#### `TestActorRef` Example
```csharp
// Examples/Blackhole.cs

[Test]
public void AuthenticationActor_should_keep_internal_count_of_users_created()
{
    // In this case, we're setting up a ActorOfAsTestActorRef
    // to be able to directly access the internal state of the actor.
    // Generally, THIS IS NOT RECOMMENDED as you want to test the actor
    // from the outside, as it will be used in reality.In real use,
    // actors can't access each others internal state.

    // make an AuthenticationActor that will always successfully create users
    var authProps = Props.Create(() => new AuthenticationActor(true));
    var auth = ActorOfAsTestActorRef<AuthenticationActor>(authProps);

    // ACCESSING INTERNAL ACTOR STATE
    // assert that actor will always return successful ops
    Assert.True(auth.UnderlyingActor.Successful);

    var identityProps = Props.Create(() => new IdentityManagerActor(auth));
    var identity = Sys.ActorOf(identityProps);

    // ACCESSING INTERNAL ACTOR STATE
    // assert that auth user has not created any users
    Assert.AreEqual(auth.UnderlyingActor.UsersCreated, 0);

    // create some users
    var usersToCreate = 4;
    for (int i = 0; i < usersToCreate; i++)
    {
        identity.Tell(new CreateUser());
        var result = ExpectMsg<UserResult>().Successful;
        Assert.True(result);
    }

    // ACCESSING INTERNAL ACTOR STATE
    // assert that count has been kept, and is correct
    Assert.AreEqual(auth.UnderlyingActor.UsersCreated, usersToCreate);
}
```


### The Programmable Utility Player: `TestProbe`
The [`TestProbe`](http://api.getakka.net/docs/stable/html/F0EE599D.htm) is a flexible, programmable utility player. Think of it like a more flexible and configurable `TestActor`. The `TestProbe` has the base assertions and message interception capabilities of the `TestActor`. But you can also program the `TestProbe` for whatever custom behavior you want.  `TestProbe` can send its own messages, ignore messages that don't match a rule, reply to the sender of the last message, and much more.

The `TestProbe` has all the capabilities of the `TestActor` and then some. As an added bonus, you can run many probes in parallel.

You can program the probe to do whatever you need it to, and use it in many locations and situations. If you don't know how else to solve your problem, odds are good you'll end up solving it with a `TestProbe`.

#### `TestProbe` Example
Here are two simple examples of using a `TestProbe`. First as a stand-in for another actor, and in the second example as a a more programmable `TestActor`.

```csharp
// Examples/TestProbe.cs

[Test]
public void UserSessionActor_should_register_itself_with_presence_on_startup()
{
    // use TestProbe as stand in for FriendsPresenceActor
    var presence = CreateTestProbe("userPresence");
    var userProps = Props.Create(() => new UserSessionActor(_fakeUser.Generate(), presence));
    var user = Sys.ActorOf(userProps);

    presence.ExpectMsgFrom<RegisterUser>(user, message => message.User.Equals(user));
}

// omitted for brevity...

[Test]
public void FriendsPresenceActor_should_alert_other_users_when_friend_comes_online()
{
    var presence = ActorOfAsTestActorRef<FriendsPresenceActor>("presence");

    // create probes in place of users
    var user1 = CreateTestProbe("user1");
    var user2 = CreateTestProbe("user2");
    var user3 = CreateTestProbe("user3");

    // make those probes send a message I want
    user1.Send(presence, new RegisterUser(user1));
    user2.Send(presence, new RegisterUser(user2));
    user3.Send(presence, new RegisterUser(user3));

    // pre-check we have the right number of subscribers
    Assert.AreEqual(3, presence.UnderlyingActor.Subscribers.Count);

    // trigger by having a probe send msg that I want
    user1.Send(presence, new UserOnline(user1));

    // other user probes should have been informed of new friend presence
    user1.ExpectNoMsg();
    user2.ExpectMsgFrom<UserOnline>(presence, online => online.User.Equals(user1));
    user3.ExpectMsgFrom<UserOnline>(presence, online => online.User.Equals(user1));
}
```

## `Akka.TestKit` FAQs
### How Do I Test Actor Lifecycle?
There are many ways to test actor lifecycle. Here are a few common approaches:

- verify that the actor calls `IDisposable.Dispose()` on a dependent resource
- have the actor send messages during certain lifecycle hook methods
- have the actor register itself with another actor
- have the actor change its response to a message after restarting

In this example, we verify that the actor disposes of a repository. That disposal is part of the actor shutdown process and lets us see if it actually stopped.

```csharp
// Examples/Lifecycle.cs

[Test]
public void RepositoryActor_should_dispose_repo_on_shutdown()
{
    var repo = new FooRepository();
    var repoActor = Sys.ActorOf(Props.Create(() => new RepositoryActor(repo)));

    // assert repo has not been disposed
    Assert.False(repo.Disposed);

    Sys.Stop(repoActor);

    Thread.Sleep(TimeSpan.FromSeconds(1));

    // assert repo has now been disposed by RepositoryActor's PostStop method
    Assert.True(repo.Disposed);
}
```

### How Do I Test a Parent/Child Relationship?
Testing the parent/child relationship is more complicated. This is one case where Akka.NET's commitment to providing simple abstractions makes testing harder.

The simplest way to test this relationship is with messaging. For example, you can create a parent actor whose child messages another actor once it starts. Or you may have the parent forward a message to the child, and then the child can reply to the original sender, e.g.:

```csharp
// Examples/ParentChild.cs

public class ChildActor : ReceiveActor
{
    public ChildActor()
    {
        ReceiveAny(o => Sender.Tell("hello!"));
    }
}

public class ParentActor : ReceiveActor
{
    public ParentActor()
    {
        var child = Context.ActorOf(Props.Create(() => new ChildActor()));
        ReceiveAny(o => child.Forward(o));
    }
}

[TestFixture]
public class ParentGreeterSpecs : TestKit
{
    [Test]
    public void Parent_should_create_child()
    {
        // verify child has been created by sending parent a message
        // that is forwarded to child, and which child replies to sender with
        var parentProps = Props.Create(() => new ParentActor());
        var parent = ActorOfAsTestActorRef<ParentActor>(parentProps, TestActor);
        parent.Tell("this should be forwarded to the child");
        ExpectMsg("hello!");
    }
}
```

#### A Word of Warning On Testing Parent/Child Relationships
Avoid over-coupling your code to your hierarchy!

Over-testing parent/child relationships can couple your tests to your hierarchy implementation. This increases the costs of refactoring your code later on by forcing many test rewrites. You need to strike a balance between verifying your intent and testing your implementation.

### How Do I Test a `SupervisorStrategy`?
This is another place it's easy to focus too much on the implementation of your code. You want to focus on testing the intended effects of your code, not testing the framework itself.

Here are a few common tests related to `SupervisorStrategy`s:

- test that actors throw exceptions in certain conditions
- test that actors stop or restarted given bad input
- test that exceptions create side-effects such as log messages, error reports, or disposed resources

`SupervisorStrategy` tests tend to be more integrative and need more thinking from you. Here are two example tests:

```csharp
// Examples/Supervision.cs

[Test]
public void OddEvenActor_should_throw_exception_on_bad_input()
{
    // create coordinator, which spins up odd/even child actors
    var coordinator = Sys.ActorOf(Props.Create(() => new OddEvenCoordinatorActor()), "coordinator");

    // assert we're set up right
    coordinator.Tell(1);
    coordinator.Tell(2);
    ExpectMsg<ValidInput>();
    ExpectMsg<ValidInput>();

    // test
    var even = ActorSelection("akka://test/user/coordinator/even").ResolveOne(TimeSpan.FromSeconds(5)).Result;
    var odd = ActorSelection("akka://test/user/coordinator/odd").ResolveOne(TimeSpan.FromSeconds(5)).Result;

    // expect exception
    EventFilter.Exception<BadDataException>().Expect(2, () =>
    {
        even.Tell(1);
        odd.Tell(2);
    });
}

// omitted for brevity...

[Test]
public void OddEvenCoordinatorActor_should_stop_child_on_bad_data()
{
    // create coordinator, which spins up odd/even child actors
    var coordinator = Sys.ActorOf(Props.Create(() => new OddEvenCoordinatorActor()), "coordinator");

    // assert we're set up right
    coordinator.Tell(1);
    coordinator.Tell(2);
    ExpectMsg<ValidInput>();
    ExpectMsg<ValidInput>();

    // test
    var even = ActorSelection("akka://test/user/coordinator/even").ResolveOne(TimeSpan.FromSeconds(5)).Result;
    var odd = ActorSelection("akka://test/user/coordinator/odd").ResolveOne(TimeSpan.FromSeconds(5)).Result;

    // even & odd should be killed when parent's SupervisorStrategy kicks in
    Watch(even);
    Watch(odd);

    // we cover BadDataShutdown being sent in another test
    IgnoreMessages(msg => msg is BadDataShutdown);

    // even & odd should be killed when parent's SupervisorStrategy kicks in
    even.Tell(1);
    ExpectTerminated(even);
    odd.Tell(2);
    ExpectTerminated(odd);
}
```

### How Do I Change the Configuration of the `TestActorSystem`?
You pass the HOCON string to `base` in the constructor of your `TestFixture`.

In this example, I am changing the `loglevel` to `INFO` to hide `DEBUG` messages. But you can override / manipulate whatever config settings you need to. You can only change the config per `TestFixture`, not per test.

For example:

```csharp
// Examples/HOCON.cs

[TestFixture]
public class TestHoconActorSpecs : TestKit
{
    /// <summary>
    /// TestFixture with custom HOCON passed into the TestActorSystem.
    /// </summary>
    public TestHoconActorSpecs() : base(@"akka.loglevel = INFO") { }

    // can also pass in a full HOCON string w/ nesting, e.g.
    //public TestHoconActorSpecs() : base(@"
    //akka {
    //    loglevel = INFO
    //}") { }

    [Test]
    public void TestHoconActor_should_log_debug_messages_invisibly_when_loglevel_is_info()
    {
        // should see no debug messages from the actor since overrode the config
        EventFilter.Debug("TestHoconActor got a message").Expect(0, () =>
        {
            var actor = Sys.ActorOf(Props.Create(() => new TestHoconActor()));
            actor.Tell("foo");
        });
    }
}
```

### How Do I Test Scheduled Messages?
A common question is this: "how do I test a message scheduled for an hour from now, without waiting for an hour?"

You can manipulate time in your tests so that you don't have to sit around waiting. `TestKit` uses virtual time that you can manipulate by changing the [`Scheduler`](http://api.getakka.net/docs/stable/html/7D461894.htm) used.  If you use the [`TestScheduler`](http://api.getakka.net/docs/stable/html/64701727.htm) you can call [`Advance`](http://api.getakka.net/docs/stable/html/8949BBD7.htm) or [`AdvanceTo`](http://api.getakka.net/docs/stable/html/CE4C5520.htm) to jump forward in time.

This is the process required to manipulate time in your tests:

1. Configure `Sys` to use the `Akka.TestKit.TestScheduler` by passing in HOCON.
1. Cast the `Scheduler` to the `TestScheduler` in a property. Use this new `Scheduler` in your tests instead of the built-in scheduler.
2. When creating your actors, you must configure `Props` so your actors use the [`CallingThreadDispatcher`](http://api.getakka.net/docs/stable/html/869903B3.htm). This forces the scheduler run on the same thread as the test, so that `Advance` call works as intended.
3. Use `Advance` and `AdvanceTo` to manipulate time as needed in your tests.
4. Use assertions, `TestProbe`, `ExpectMsg`, etc. as you wish.

Here's an example:

```csharp
// Examples/ScheduledMessages.cs

/// <summary>
/// TestFixture with virtual time <see cref="TestScheduler"/> turned on so we can jump forward in time!
/// </summary>
public ScheduledMessageActorSpecs() : base(@"akka.scheduler.implementation = ""Akka.TestKit.TestScheduler, Akka.TestKit""") { }

/// <summary>
/// Cast the TestActorSystem scheduler to be a TestScheduler
/// </summary>
private TestScheduler Scheduler => (TestScheduler)Sys.Scheduler;

[Test]
public void ScheduledMessageActor_should_schedule_ScheduleOnceMessage_appropriately()
{
    var actor = Sys.ActorOf(Props.Create(() => new FutureEchoActor()).WithDispatcher(CallingThreadDispatcher.Id));

    var delay1 = TimeSpan.FromHours(1.5);
    actor.Tell(new ScheduleOnceMessage(delay1, 1));
    Scheduler.Advance(delay1);
    var firstId = ExpectMsg<ScheduleOnceMessage>().Id;
    Assert.AreEqual(1, firstId);
}
```

***

### NuGet Dependencies
This sample depends on the following [NuGet](http://www.nuget.org/ "NuGet - package manager for.NET") packages in order to run:

* [Akka.NET](http://www.nuget.org/packages/Akka/) (core only)
* [Akka.TestKit](https://www.nuget.org/packages/Akka.TestKit/)
* [Akka.TestKit.NUnit](https://www.nuget.org/packages/Akka.TestKit.NUnit/)

## Running the Sample

1. Clone this repository to your local computer - we highly recommend installing [Github for Windows](https://windows.github.com/ "Github for Windows") if you don't already have a Git client installed.
2. Open `TestKitSample.sln` in Visual Studio 2012 or later.
3. Press `F6` to build the sample - this solution has [NuGet package restore](http://docs.nuget.org/docs/workflows/using-nuget-without-committing-packages) enabled, so any third party dependencies will automatically be downloaded and added as references.
4. Run whichever `TestFixture`s you like, or the tests for the whole solution.

From there you can add/change the demo code and tests to see what happens.

## Questions?

If you have any questions about this sample, please [create a Github issue for us](https://github.com/petabridge/akkadotnet-code-samples/issues)!
