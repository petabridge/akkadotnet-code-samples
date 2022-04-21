namespace ReliableRabbitMQ.Shared;

public class RabbitMQSettings
{
    public string Host { get; set; }
    public int Port { get; set; }

    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    
    /// <summary>
    /// The maximum number of parallel messages that can be processed in-flight
    /// </summary>
    public int MaxParallelism { get; set; }
}