using IdentityService.Domain.Events;
using SmartCampusOS.SharedKernel.Events;

namespace IdentityService.Infrastructure.Events;

/// <summary>账号激活集成事件（LLD §7 UserActivated）：消费者 NoticeService 初始化消息订阅。</summary>
public sealed class UserActivatedIntegrationEvent : IntegrationEvent
{
    public UserActivatedIntegrationEvent(long userId, long? activatedBy)
        : base(ServiceNames.Identity, eventType: "UserActivated")
    {
        UserId = userId;
        ActivatedBy = activatedBy;
    }

    /// <summary>被激活用户 ID。</summary>
    public long UserId { get; }

    /// <summary>激活操作人 ID（可为 null）。</summary>
    public long? ActivatedBy { get; }
}
