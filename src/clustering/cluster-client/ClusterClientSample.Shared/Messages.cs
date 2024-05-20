using System.Collections.Generic;

namespace ClusterClientSample.Shared;

public sealed record BatchedWork(int Size);

public sealed record Work(int Id);

public sealed class WorkComplete
{
    public static readonly WorkComplete Instance = new();
    private WorkComplete() { }
}

public sealed record Result(int Id, int Value);

public sealed class SendReport
{
    public static readonly SendReport Instance = new ();

    private SendReport() { }
}

public sealed record Report(IDictionary<string, int> Counts);
