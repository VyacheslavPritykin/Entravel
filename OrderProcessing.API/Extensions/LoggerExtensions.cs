namespace OrderProcessing.API.Extensions;

public static class LoggerExtensions
{
    public static LogScope BeginPropertyScope(this ILogger logger, string property, object? value)
    {
        var dictionary = new Dictionary<string, object?>(1) { { property, value } };
        return new LogScope(logger.BeginScope(dictionary));
    }

    public static LogScope BeginPropertyScope(this ILogger logger, params (string Property, object? Value)[] properties)
    {
        var dictionary = properties.ToDictionary(p => p.Property, p => p.Value);
        return new LogScope(logger.BeginScope(dictionary));
    }
}

public sealed record LogScope(IDisposable? Scope) : IDisposable
{
    public void Dispose() => Scope?.Dispose();
}