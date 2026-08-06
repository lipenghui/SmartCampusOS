namespace SmartCampusOS.SharedKernel.Time;

/// <summary>
/// 统一时钟：系统时间以 UTC 存储、展示层转东八区（LLD §1.4 时间约定）。
/// </summary>
/// <remarks>
/// 通过 <see cref="Provider"/> 可注入可控时钟（测试用）；生产环境保持 <see cref="TimeProvider.System"/>。
/// 东八区使用固定偏移 <c>+08:00</c>，不依赖主机时区数据库，避免容器时区缺失导致的异常。
/// </remarks>
public static class SystemClock
{
    /// <summary>东八区固定偏移（北京时间）。</summary>
    public static readonly TimeSpan BeijingOffset = TimeSpan.FromHours(8);

    /// <summary>时钟源，默认系统时钟；测试可替换为可控实现。</summary>
    public static TimeProvider Provider { get; set; } = TimeProvider.System;

    /// <summary>当前 UTC 时间。</summary>
    public static DateTimeOffset UtcNow => Provider.GetUtcNow();

    /// <summary>当前 UTC 时间（<see cref="DateTime"/>，Kind=Utc）。</summary>
    public static DateTime UtcNowDateTime => Provider.GetUtcNow().UtcDateTime;

    /// <summary>当前 UTC 时间的 Unix 毫秒时间戳。</summary>
    public static long UtcNowUnixMilliseconds => Provider.GetUtcNow().ToUnixTimeMilliseconds();

    /// <summary>当前北京时间（东八区）。</summary>
    public static DateTimeOffset BeijingNow => UtcNow.ToOffset(BeijingOffset);

    /// <summary>UTC 时间转换为北京时间（东八区）。</summary>
    public static DateTimeOffset ToBeijing(DateTimeOffset utc) => utc.ToOffset(BeijingOffset);

    /// <summary>
    /// UTC <see cref="DateTime"/> 转换为北京时间的 <see cref="DateTime"/>（Kind=Unspecified）。
    /// </summary>
    /// <remarks>输入 Kind=Unspecified 时按 UTC 解释（约定存储即 UTC）。</remarks>
    public static DateTime ToBeijingDateTime(DateTime utc)
    {
        DateTimeOffset asUtc = utc.Kind switch
        {
            DateTimeKind.Utc => new DateTimeOffset(utc),
            DateTimeKind.Local => new DateTimeOffset(utc.ToUniversalTime()),
            _ => new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)),
        };

        return asUtc.ToOffset(BeijingOffset).DateTime;
    }
}
