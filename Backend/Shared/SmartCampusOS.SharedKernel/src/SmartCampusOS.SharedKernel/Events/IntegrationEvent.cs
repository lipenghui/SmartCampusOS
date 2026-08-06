using SmartCampusOS.SharedKernel.Time;

namespace SmartCampusOS.SharedKernel.Events;

/// <summary>
/// 集成事件基类，对齐 LLD §7 事件规范：
/// 事件结构 <c>{ eventId, eventType, occurredAt, sourceService, payload }</c>，
/// 消费者按 <c>eventId</c> 幂等去重，失败进死信队列并告警。
/// </summary>
/// <remarks>
/// <para>用法：业务领域事件在 Domain 层定义，Infrastructure 层发布前继承本基类转为集成事件；
/// 派生类的属性即 <c>payload</c>（System.Text.Json 序列化时与基类属性平铺输出）。</para>
/// <para>跨进程反序列化：消费者依据 <c>eventType</c> 选择具体派生类型绑定（各服务自行实现
/// MassTransit/RabbitMQ 消费者路由，本基类不绑定任何消息框架，保持精简）。</para>
/// </remarks>
public abstract class IntegrationEvent
{
    /// <summary>
    /// 初始化集成事件。
    /// </summary>
    /// <param name="sourceService">来源服务，使用 <see cref="ServiceNames"/> 常量。</param>
    /// <param name="eventType">事件类型标识，默认取派生类类名；如需稳定契约可显式传入。</param>
    /// <param name="eventId">事件唯一 ID，默认 <see cref="Guid.NewGuid"/>，用于消费者幂等去重。</param>
    /// <param name="occurredAt">事件发生时间，默认当前 UTC 时间。</param>
    protected IntegrationEvent(
        string sourceService,
        string? eventType = null,
        Guid? eventId = null,
        DateTimeOffset? occurredAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceService);

        SourceService = sourceService;
        EventType = eventType ?? GetType().Name;
        EventId = eventId ?? Guid.NewGuid();
        OccurredAt = occurredAt ?? SystemClock.UtcNow;
    }

    /// <summary>事件唯一 ID（幂等去重键）。</summary>
    public Guid EventId { get; }

    /// <summary>事件类型标识（默认派生类类名）。</summary>
    public string EventType { get; }

    /// <summary>事件发生时间（UTC，对齐 LLD §1.4 时间约定）。</summary>
    public DateTimeOffset OccurredAt { get; }

    /// <summary>来源服务（LLD §2.3 服务名，见 <see cref="ServiceNames"/>）。</summary>
    public string SourceService { get; }
}
