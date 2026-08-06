using IdentityService.Application.Abstractions.Events;
using MassTransit;
using SmartCampusOS.SharedKernel.Events;

namespace IdentityService.Infrastructure.Events;

/// <summary>
/// MassTransit + RabbitMQ 事件发布（LLD §2.4 / §7）：领域事件转集成事件后发往事件交换机，
/// 消费者按 EventId 幂等去重；失败进死信队列。
/// </summary>
public sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : class
    {
        var integrationEvent = InMemoryEventPublisher.ToIntegrationEvent(@event);
        if (integrationEvent is null)
        {
            return;
        }

        // 以集成事件的具体类型发布，MassTransit 按 CLR 类型路由到交换机
        await publishEndpoint.Publish(integrationEvent, integrationEvent.GetType(), ct);
    }
}
