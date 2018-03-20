using System;
using Newtonsoft.Json;
using WebCrawler.Shared.State;

namespace WebCrawler.Shared.Commands.V1
{
    public enum JobStatus
    {
        Running = 0,
        Starting = 1,
        Failed = 2,
        Finished = 3,
        Stopped = 4
    }

    public class JobStatusUpdate : IStatusUpdateV1
    {
        public JobStatusUpdate(CrawlJob job) : this(job, null, JobStatus.Starting, DateTime.UtcNow, null)
        {
        }

        [JsonConstructor] // need this to tell JSON.NET which constructor to pick
        public JobStatusUpdate(CrawlJob job, CrawlJobStats stats, JobStatus status, DateTime startTime, DateTime? endTime)
        {
            Job = job;
            StartTime = startTime;
            EndTime = endTime;
            Status = status;
            Stats = stats;
        }

        public CrawlJob Job { get; private set; }

        public CrawlJobStats Stats { get; private set; }

        public DateTime StartTime { get; private set; }

        public DateTime? EndTime { get; private set; }

        public TimeSpan Elapsed
        {
            get
            {
                return ((EndTime.HasValue ? EndTime.Value : DateTime.UtcNow) - StartTime);
            }
        }

        public JobStatus Status { get; private set; }

        public JobStatusUpdate WithStats(CrawlJobStats newStats)
        {
            return new JobStatusUpdate(Job, newStats, Status, StartTime, EndTime);
        }

        public JobStatusUpdate WithEndTime(DateTime endTime)
        {
            return new JobStatusUpdate(Job, Stats, Status, StartTime, endTime);
        }

        public JobStatusUpdate WithStatus(JobStatus status)
        {
            return new JobStatusUpdate(Job, Stats, status, StartTime, EndTime);
        }
    }
}
