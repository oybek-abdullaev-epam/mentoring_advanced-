using JobScheduler.Dal.Common;

namespace JobScheduler.Dal.UnitTests;

public class TransientSqlErrorsTests
{
    public static TheoryData<int> TransientNumbers =>
    [
        4060, 40197, 40501, 40613, 49918, 49919, 49920, 4221, 615,
        1219, 3947, 3948, 3961,
        64, 233, 10053, 10054, 10060,
        1205,
    ];

    [Theory]
    [MemberData(nameof(TransientNumbers))]
    public void ListedNumber_IsTransient(int number) => Assert.True(TransientSqlErrors.IsTransient(number));

    [Theory]
    [InlineData(-2)]
    [InlineData(3906)]
    [InlineData(2601)]
    [InlineData(2627)]
    [InlineData(547)]
    [InlineData(0)]
    public void OtherNumber_IsNotTransient(int number) => Assert.False(TransientSqlErrors.IsTransient(number));

    [Fact]
    public void NonSqlException_IsNotTransient()
    {
        Assert.False(TransientSqlErrors.IsTransient(new InvalidOperationException()));
        Assert.False(TransientSqlErrors.IsTransient(new TimeoutException()));
        Assert.False(TransientSqlErrors.IsTransient(new OperationCanceledException()));
    }
}
