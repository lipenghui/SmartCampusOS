using System.Text.Json;
using IdentityService.Application.Abstractions.Events;
using IdentityService.Domain.Events;
using Microsoft.Extensions.Logging;
using SmartCampusOS.SharedKernel.Events;

namespace IdentityService.Infrastructure.Events;

/// <summary>
/// 进程内事件发布（默认，LLD §7）：领域事件转为集成事件后仅记录日志。
/// 用于开发/单机独立启动；配置 RabbitMq.Enabled 时切换 <see cref="MassTransitEventPublisher"/>。
/// </summary>
public sealed class InMemoryEventPublisher(ILogger<InMemoryEventPublisher> logger) : IEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : class
    {
        var integrationEvent = ToIntegrationEvent(@event);
        if (integrationEvent is null)
        {
            logger.LogWarning("未识别的领域事件类型 {EventType}（跳过发布）", typeof(TEvent).Name);
            return Task.CompletedTask;
        }

        logger.LogInformation(
            "事件发布（内存）：{EventType} EventId={EventId} Source={Source} Payload={Payload}",
            integrationEvent.EventType,
            integrationEvent.EventId,
            integrationEvent.SourceService,
            JsonSerializer.Serialize(@event, JsonOptions));
        return Task.CompletedTask;
    }

    /// <summary>领域事件 → 集成事件（LLD §7：Infrastructure 发布前转换，补 EventId 幂等头）。</summary>
    internal static IntegrationEvent? ToIntegrationEvent<TEvent>(TEvent @event) where TEvent : class =>
        @event switch
        {
            UserActivated e => new UserActivatedIntegrationEvent(e.UserId, e.ActivatedBy),
            _ => null
        };
}
