namespace IdentityService.Domain.Events;

/// <summary>
/// 领域事件：账号激活（LLD §7 UserActivated）。
/// 账号激活成功后发布，消费者 NoticeService 据此初始化该用户的消息订阅。
/// 领域事件为纯记录；Infrastructure 层发布前转为集成事件（继承 SharedKernel.IntegrationEvent）。
/// </summary>
public sealed record UserActivated(long UserId, long? ActivatedBy, DateTimeOffset OccurredAt);
