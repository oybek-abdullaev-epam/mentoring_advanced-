using JobScheduler.Dal.Connections;
using Polly;
using Polly.Retry;

namespace JobScheduler.Dal.Common;

internal static class SqlRetryPipeline
{
    // onRetry gets the failure, the 1-based number of the attempt that just failed, and the delay before the next one.
    public static ResiliencePipeline Create(
        SqlRetryOptions options, Func<Exception, bool> isTransient, TimeProvider timeProvider, Action<Exception, int, TimeSpan> onRetry)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxAttempts, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.FirstDelay, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxDelay, TimeSpan.Zero);

        if (options.MaxAttempts == 1)
        {
            return ResiliencePipeline.Empty;
        }

        return new ResiliencePipelineBuilder { TimeProvider = timeProvider }
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxAttempts - 1,
                Delay = options.FirstDelay,
                MaxDelay = options.MaxDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Exception is { } ex && ex is not OperationCanceledException && isTransient(ex)),
                OnRetry = args =>
                {
                    onRetry(args.Outcome.Exception!, args.AttemptNumber + 1, args.RetryDelay);
                    return default;
                },
            })
            .Build();
    }
}
