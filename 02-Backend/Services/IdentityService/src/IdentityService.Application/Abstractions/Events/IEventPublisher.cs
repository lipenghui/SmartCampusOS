namespace IdentityService.Application.Abstractions.Events;

/// <summary>
/// 集成事件发布抽象（LLD §7）：领域事件经 Infrastructure 转为集成事件发布。
/// 默认进程内实现（服务可独立启动）；配置 RabbitMq.Enabled 时切换 MassTransit 发布。
/// 消费者按 EventId 幂等去重（SharedKernel.IntegrationEvent.EventId）。
/// </summary>
public interface IEventPublisher
{
    /// <summary>发布事件。</summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : class;
}
