using SmartCampusOS.SharedKernel.Ids;
using SmartCampusOS.SharedKernel.Tests.Support;

namespace SmartCampusOS.SharedKernel.Tests;

public class SnowflakeIdTests
{
    [Fact]
    public void NextId_产生正数long_id()
    {
        var generator = new SnowflakeId(0, 0);
        long id = generator.NextId();

        Assert.True(id > 0);
        Assert.Equal(typeof(long), id.GetType());
    }

    [Fact]
    public void NextId_十万个无重复()
    {
        var generator = new SnowflakeId(0, 0);
        var ids = new HashSet<long>(100_000);
        for (int i = 0; i < 100_000; i++)
        {
            Assert.True(ids.Add(generator.NextId()), $"第 {i} 个 ID 重复");
        }
    }

    [Fact]
    public void NextId_严格单调递增()
    {
        var generator = new SnowflakeId(0, 0);
        long previous = generator.NextId();
        for (int i = 0; i < 10_000; i++)
        {
            long current = generator.NextId();
            Assert.True(current > previous, $"ID 未递增：{current} <= {previous}");
            previous = current;
        }
    }

    [Fact]
    public void NextId_同一毫秒序列号递增()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero));
        var generator = new SnowflakeId(1, 2, clock);

        long first = generator.NextId();
        long second = generator.NextId();

        Assert.Equal(0, SnowflakeId.ExtractSequence(first));
        Assert.Equal(1, SnowflakeId.ExtractSequence(second));
        Assert.Equal(SnowflakeId.ExtractTimestamp(first), SnowflakeId.ExtractTimestamp(second));
        Assert.Equal(1, SnowflakeId.ExtractWorkerId(second));
        Assert.Equal(2, SnowflakeId.ExtractDatacenterId(second));
    }

    [Fact]
    public void NextId_跨毫秒序列号重置()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero));
        var generator = new SnowflakeId(0, 0, clock);

        generator.NextId(); // 毫秒 0
        clock.Advance(TimeSpan.FromMilliseconds(1));
        long next = generator.NextId(); // 毫秒 1

        Assert.Equal(0, SnowflakeId.ExtractSequence(next));
        // ExtractTimestamp 返回生成时刻的绝对 UTC 毫秒时间戳（非相对 Twepoch 的偏移）
        Assert.Equal(clock.GetUtcNow().ToUnixTimeMilliseconds(), SnowflakeId.ExtractTimestamp(next));
    }

    [Fact]
    public void NextId_时钟回拨抛异常且不产出重复()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero));
        var generator = new SnowflakeId(0, 0, clock);

        long first = generator.NextId();
        clock.Rewind(TimeSpan.FromMilliseconds(100));

        var ex = Assert.Throws<InvalidOperationException>(() => generator.NextId());
        Assert.Contains("时钟回拨", ex.Message);
        _ = first; // first 仍有效，未产出重复
    }

    [Fact]
    public void NextId_不同节点实例不冲突()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero));
        var a = new SnowflakeId(0, 0, clock);
        var b = new SnowflakeId(1, 0, clock);
        var c = new SnowflakeId(0, 1, clock);

        var ids = new HashSet<long>();
        for (int i = 0; i < 10_000; i++)
        {
            // 固定时钟下同一毫秒仅能生成 4096 个 ID，须推进时钟避免序列耗尽后自旋等待下一毫秒（永不前进 → 死循环）
            clock.Advance(TimeSpan.FromMilliseconds(1));
            Assert.True(ids.Add(a.NextId()));
            Assert.True(ids.Add(b.NextId()));
            Assert.True(ids.Add(c.NextId()));
        }
    }

    [Fact]
    public void 构造_节点ID越界抛异常()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SnowflakeId(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SnowflakeId(32, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SnowflakeId(0, 32));
    }

    [Fact]
    public void ExtractTimestamp_还原生成时间()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero));
        var generator = new SnowflakeId(0, 0, clock);

        long id = generator.NextId();
        long timestamp = SnowflakeId.ExtractTimestamp(id);

        Assert.True(timestamp > SnowflakeId.Twepoch, "生成时间应晚于纪元 2025-01-01");
        Assert.Equal(clock.GetUtcNow().ToUnixTimeMilliseconds(), timestamp);
    }

    [Fact]
    public void Default_单例可并发生成()
    {
        var results = new long[100_000];
        Parallel.For(0, results.Length, i => results[i] = SnowflakeId.Default.NextId());

        Assert.Equal(results.Length, results.Distinct().Count());
    }
}
