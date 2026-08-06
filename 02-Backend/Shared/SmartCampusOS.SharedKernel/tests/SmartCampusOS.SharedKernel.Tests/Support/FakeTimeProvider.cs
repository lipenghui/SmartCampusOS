namespace SmartCampusOS.SharedKernel.Tests.Support;

/// <summary>
/// 可控时钟：供 SnowflakeId / SystemClock 测试注入，模拟时间推进与时钟回拨。
/// </summary>
internal sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FakeTimeProvider(DateTimeOffset start) => _now = start;

    public void SetUtcNow(DateTimeOffset value) => _now = value;

    public void Advance(TimeSpan delta) => _now += delta;

    public void Rewind(TimeSpan delta) => _now -= delta;

    public override DateTimeOffset GetUtcNow() => _now;
}
