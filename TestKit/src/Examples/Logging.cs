using Akka.Actor;
using Akka.Event;
using Akka.TestKit.NUnit;
using NUnit.Framework;

namespace TestKitSample.Examples
{
    #region Messages
    public class NormalOperation { }
    public class InvalidData : NormalOperation { }
    public class ValidData : NormalOperation { }
    #endregion

    public class LoggingActor : ReceiveActor
    {
        private ILoggingAdapter _log = Context.GetLogger();

        public LoggingActor()
        {
            Receive<ValidData>(data =>
            {
                // do some operation with the valid data
                _log.Info("Completed operation with valid data");
            });

            Receive<InvalidData>(data =>
            {
                // do some operation with the invalid data
                _log.Error("Could not complete operation! Data is invalid.");
            });
        }
    }

    [TestFixture]
    public class LoggingActorSpecs : TestKit
    {
        private IActorRef _logger;

        [SetUp]
        public void Setup()
        {
            _logger = Sys.ActorOf(Props.Create(() => new LoggingActor()));
        }

        [Test]
        public void LoggingActor_should_log_info_message_on_valid_operation()
        {
            // listen for specific INFO log message that valid data op should trigger
            // by default, this looks for an exact match on the data passed in
            EventFilter.Info("Completed operation with valid data").ExpectOne(() =>
            {
                _logger.Tell(new ValidData());
            });
        }

        [Test]
        public void LoggingActor_should_log_info_message_on_valid_operation_2()
        {
            // doing same thing, but checking for messages that contain this instead
            EventFilter.Info(contains: "completed").ExpectOne(() =>
            {
                _logger.Tell(new ValidData());
            });
        }

        [Test]
        public void LoggingActor_should_log_no_errors_on_valid_operation()
        {
            // we expect zero error messages
            EventFilter.Error().Expect(0, () =>
            {
                _logger.Tell(new ValidData());
            });
        }

        [Test]
        public void LoggingActor_should_log_error_on_invalid_operation()
        {
            // we expect one error message, but we're not specifying
            // the content of the error message we expect (just that one happens)
            EventFilter.Error("Could not complete operation! Data is invalid.").ExpectOne(() =>
            {
                _logger.Tell(new InvalidData());
            });
        }


    }
}
