namespace SmartCampusOS.SharedKernel.Ids;

/// <summary>
/// 雪花 ID 生成器：生成 64 位 <see cref="long"/> 全局唯一 ID（LLD §1.4 全局标识约定）。
/// </summary>
/// <remarks>
/// 位布局（自左向右）：
/// <list type="bullet">
/// <item>1 bit：符号位，恒为 0；</item>
/// <item>41 bit：毫秒时间戳，相对纪元 <see cref="Twepoch"/>（2025-01-01 UTC），可用约 69 年；</item>
/// <item>5 bit：数据中心 ID（0~31）；</item>
/// <item>5 bit：工作节点 ID（0~31）；</item>
/// <item>12 bit：同毫秒序列号（0~4095）。</item>
/// </list>
/// 线程安全；同一毫秒内最多生成 4096 个 ID，超出后自旋等待下一毫秒。
/// 检测到时钟回拨（当前毫秒小于已生成 ID 的毫秒）时抛出 <see cref="InvalidOperationException"/>，
/// 拒绝生成可能重复的 ID（对齐可观测原则：快速失败优于静默错乱）。
/// </remarks>
public sealed class SnowflakeId
{
    /// <summary>纪元：2025-01-01 00:00:00 UTC。</summary>
    public const long Twepoch = 1_735_689_600_000L;

    private const int WorkerIdBits = 5;
    private const int DatacenterIdBits = 5;
    private const int SequenceBits = 12;

    private const long MaxWorkerId = (1L << WorkerIdBits) - 1;             // 31
    private const long MaxDatacenterId = (1L << DatacenterIdBits) - 1;     // 31
    private const long MaxSequence = (1L << SequenceBits) - 1;             // 4095

    private const int WorkerIdShift = SequenceBits;                        // 12
    private const int DatacenterIdShift = SequenceBits + WorkerIdBits;     // 17
    private const int TimestampLeftShift = SequenceBits + WorkerIdBits + DatacenterIdBits; // 22

    private readonly long _workerId;
    private readonly long _datacenterId;
    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();

    private long _lastTimestamp = -1L;
    private long _sequence;

    /// <summary>工作节点 ID（0~31）。</summary>
    public long WorkerId => _workerId;

    /// <summary>数据中心 ID（0~31）。</summary>
    public long DatacenterId => _datacenterId;

    /// <summary>默认实例（数据中心 0 / 节点 0）；单机或未显式配置节点时使用。</summary>
    public static SnowflakeId Default { get; } = new(0, 0);

    /// <summary>
    /// 创建雪花 ID 生成器实例。
    /// </summary>
    /// <param name="workerId">工作节点 ID（0~31），多副本部署时须唯一，可用 Pod 序号等分配。</param>
    /// <param name="datacenterId">数据中心 ID（0~31），多校区/多集群隔离时使用。</param>
    /// <param name="timeProvider">时间源，默认 <see cref="TimeProvider.System"/>；测试可注入可控时钟。</param>
    /// <exception cref="ArgumentOutOfRangeException">ID 超出 0~31 范围。</exception>
    public SnowflakeId(long workerId, long datacenterId, TimeProvider? timeProvider = null)
    {
        if (workerId is < 0 or > MaxWorkerId)
        {
            throw new ArgumentOutOfRangeException(nameof(workerId), $"workerId 必须在 0~{MaxWorkerId} 之间");
        }

        if (datacenterId is < 0 or > MaxDatacenterId)
        {
            throw new ArgumentOutOfRangeException(nameof(datacenterId), $"datacenterId 必须在 0~{MaxDatacenterId} 之间");
        }

        _workerId = workerId;
        _datacenterId = datacenterId;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>生成下一个全局唯一 ID。</summary>
    /// <exception cref="InvalidOperationException">检测到时钟回拨。</exception>
    public long NextId()
    {
        lock (_gate)
        {
            long timestamp = CurrentMilliseconds();

            if (timestamp < _lastTimestamp)
            {
                long offset = _lastTimestamp - timestamp;
                throw new InvalidOperationException(
                    $"检测到时钟回拨 {offset}ms（上次 {_lastTimestamp}，当前 {timestamp}），拒绝生成 ID 以防止重复");
            }

            if (timestamp == _lastTimestamp)
            {
                _sequence = (_sequence + 1) & MaxSequence;
                if (_sequence == 0)
                {
                    // 同毫秒序列耗尽：自旋等到下一毫秒
                    timestamp = WaitNextMillisecond(_lastTimestamp);
                }
            }
            else
            {
                _sequence = 0;
            }

            _lastTimestamp = timestamp;
            return ((timestamp - Twepoch) << TimestampLeftShift)
                   | (_datacenterId << DatacenterIdShift)
                   | (_workerId << WorkerIdShift)
                   | _sequence;
        }
    }

    /// <summary>生成下一个全局唯一 ID 的十进制字符串形式。</summary>
    public string NextIdString() => NextId().ToString();

    /// <summary>从 ID 中解析出生成时的毫秒时间戳（UTC）。</summary>
    public static long ExtractTimestamp(long id) => (id >> TimestampLeftShift) + Twepoch;

    /// <summary>从 ID 中解析出数据中心 ID。</summary>
    public static long ExtractDatacenterId(long id) => (id >> DatacenterIdShift) & MaxDatacenterId;

    /// <summary>从 ID 中解析出工作节点 ID。</summary>
    public static long ExtractWorkerId(long id) => (id >> WorkerIdShift) & MaxWorkerId;

    /// <summary>从 ID 中解析出同毫秒序列号。</summary>
    public static long ExtractSequence(long id) => id & MaxSequence;

    private long CurrentMilliseconds() => _timeProvider.GetUtcNow().ToUnixTimeMilliseconds();

    private long WaitNextMillisecond(long lastTimestamp)
    {
        long timestamp = CurrentMilliseconds();
        while (timestamp <= lastTimestamp)
        {
            // 允许 OS 调度让出，避免空转烧 CPU
            Thread.SpinWait(16);
            timestamp = CurrentMilliseconds();
        }

        return timestamp;
    }
}
