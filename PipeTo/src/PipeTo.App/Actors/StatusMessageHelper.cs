using System;

namespace PipeTo.App.Actors
{

        
    /// <summary>
    /// Internal status code used for color-coding <see cref="ConsoleWriterActor.ConsoleWriteMsg"/> messages
    /// in this sample
    /// </summary>
    public enum PipeToSampleStatusCode
    {
        Normal = 0,
        Success = 1,
        Failure = 2
    };

    /// <summary>
    /// Helper class used to write <see cref="ConsoleWriterActor.ConsoleWriteMsg"/> messages
    /// with proper color-coding
    /// </summary>
    public static class StatusMessageHelper
    {
        /// <summary>
        /// Map a <see cref="PipeToSampleStatusCode"/> to a <see cref="ConsoleColor"/>
        /// </summary>
        public static ConsoleColor MapConsoleColor(PipeToSampleStatusCode statusCode)
        {
            switch (statusCode)
            {
                case PipeToSampleStatusCode.Failure:
                    return ConsoleColor.DarkRed;
                case PipeToSampleStatusCode.Success:
                    return ConsoleColor.Green;
                default:
                case PipeToSampleStatusCode.Normal:
                    return ConsoleColor.Gray;
            }
        }

        /// <summary>
        /// Factory method for <see cref="ConsoleWriterActor.ConsoleWriteMsg"/> instances.
        /// </summary>
        public static ConsoleWriterActor.ConsoleWriteMsg CreateMessage(string message,
            PipeToSampleStatusCode statusCode = PipeToSampleStatusCode.Normal)
        {
            return new ConsoleWriterActor.ConsoleWriteMsg(message, MapConsoleColor(statusCode));
        }

        public static ConsoleWriterActor.ConsoleWriteFailureMessage CreateFailureMessage(string message, string url)
        {
            return new ConsoleWriterActor.ConsoleWriteFailureMessage(message, url, MapConsoleColor(PipeToSampleStatusCode.Failure));    
        }

        public static ConsoleWriterActor.ConsoleWriteTaskCompleteMessage CreateOperationCompletedSuccessfullyMessage(
            string message, string url)
        {
            return new ConsoleWriterActor.ConsoleWriteTaskCompleteMessage(message, url, MapConsoleColor(PipeToSampleStatusCode.Success));
        }
    }
}
