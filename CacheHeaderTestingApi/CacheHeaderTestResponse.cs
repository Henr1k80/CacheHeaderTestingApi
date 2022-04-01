namespace CacheHeaderTestingApi;

public record CacheHeaderTestResponse
{
    public DateTime UtcNow { get; } = DateTime.UtcNow;

    public TimeSpan ServerTimeTaken { get; init; }

    public ushort MaxAge  { get; init; }
    public ushort SMaxAge  { get; init; }
    public ushort StaleWhileRevalidate  { get; init; }
    public ushort StaleWhileError  { get; init; }
    public string ETag { get; init; }
    public string LastModified { get; init; }
}