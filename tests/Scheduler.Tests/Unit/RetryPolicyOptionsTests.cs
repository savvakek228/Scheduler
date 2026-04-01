using Scheduler.Application.Processing;
using Xunit;

namespace Scheduler.Tests.Unit;

public sealed class RetryPolicyOptionsTests
{
    [Fact]
    public void DefaultMaxAttempts_IsExpected()
    {
        var o = new RetryPolicyOptions();
        Assert.Equal(RetryPolicyOptions.DefaultMaxAttempts, o.MaxAttempts);
    }
}
