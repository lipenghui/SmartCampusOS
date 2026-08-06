using SmartCampusOS.SharedKernel.Time;
using SmartCampusOS.SharedKernel.Tests.Support;

namespace SmartCampusOS.SharedKernel.Tests;

public class SystemClockTests : IDisposable
{
    private readonly FakeTimeProvider _clock =
        new(new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero));

    public SystemClockTests() => SystemClock.Provider = _clock;

    public void Dispose() => SystemClock.Provider = TimeProvider.System;

    [Fact]
    public void UtcNow_返回注入时钟的UTC时间()
    {
        Assert.Equal(new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero), SystemClock.UtcNow);
    }

    [Fact]
    public void BeijingNow_比UTC快8小时()
    {
        Assert.Equal(new DateTimeOffset(2026, 8, 6, 18, 0, 0, TimeSpan.FromHours(8)), SystemClock.BeijingNow);
    }

    [Fact]
    public void ToBeijing_偏移转换()
    {
        var utc = new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero);
        Assert.Equal(new DateTimeOffset(2026, 8, 6, 18, 0, 0, TimeSpan.FromHours(8)), SystemClock.ToBeijing(utc));
    }

    [Fact]
    public void ToBeijingDateTime_UTC转北京时间()
    {
        var utc = new DateTime(2026, 8, 6, 10, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2026, 8, 6, 18, 0, 0), SystemClock.ToBeijingDateTime(utc));
    }

    [Fact]
    public void UtcNowUnixMilliseconds_对齐纪元()
    {
        // 2026-08-06T10:00:00Z 相对 Unix 纪元的毫秒数
        long expected = new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        Assert.Equal(expected, SystemClock.UtcNowUnixMilliseconds);
    }
}
