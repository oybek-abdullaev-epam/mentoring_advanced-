namespace JobScheduler.Dal.Contracts;

public sealed record LaunchDetailsPayload
{
    public required string Endpoint { get; init; }
    public required LaunchHttpMethod HttpMethod { get; init; }

    // Raw JSON strings.
    public string? Headers { get; init; }
    public string? Payload { get; init; }
}
