using SmartCampusOS.SharedKernel.Security;

namespace SmartCampusOS.SharedKernel.Users;

/// <summary>
/// 当前用户上下文抽象，对齐 LLD §2.6.4 / §8.2：网关解析 JWT 后透传
/// <c>X-User-Id</c> / <c>X-User-Roles</c> / <c>X-Data-Scope</c>（家长访问子女时另传 <c>X-Child-Id</c>），
/// 服务内经本抽象供 Application 层使用（实现由各服务 Api 层提供，不持有 JWT 解析逻辑）。
/// </summary>
public interface ICurrentUser
{
    /// <summary>是否已认证。</summary>
    bool IsAuthenticated { get; }

    /// <summary>当前用户 ID（雪花 ID，LLD §1.4）；未认证时为 <c>null</c>。</summary>
    long? UserId { get; }

    /// <summary>学工号。</summary>
    string? UserNo { get; }

    /// <summary>显示姓名（展示层自行脱敏，见 <see cref="MaskUtil"/>）。</summary>
    string? DisplayName { get; }

    /// <summary>当前用户全部角色编码（支持多角色，LLD §8.1.2）。</summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>数据范围（LLD §8.2 四档），仓储层据此追加过滤。</summary>
    DataScope DataScope { get; }

    /// <summary>家长端当前访问的子女用户 ID（<c>X-Child-Id</c>）；非家长场景为 <c>null</c>。</summary>
    long? ActiveChildId { get; }
}

/// <summary>
/// 当前用户上下文的默认实现（不可变记录）。
/// </summary>
/// <param name="UserId">用户 ID（雪花 ID）。</param>
/// <param name="UserNo">学工号。</param>
/// <param name="DisplayName">显示姓名。</param>
/// <param name="DataScope">数据范围（LLD §8.2）。</param>
/// <param name="Roles">角色编码集合（支持多角色）。</param>
/// <param name="ActiveChildId">家长端当前访问的子女用户 ID。</param>
public sealed record CurrentUser(
    long? UserId,
    string? UserNo,
    string? DisplayName,
    DataScope DataScope,
    IReadOnlyList<string> Roles,
    long? ActiveChildId = null) : ICurrentUser
{
    /// <summary>是否已认证。</summary>
    public bool IsAuthenticated => UserId.HasValue;

    /// <summary>匿名用户（未认证请求的占位上下文）。</summary>
    public static CurrentUser Anonymous { get; } =
        new(null, null, null, DataScope.Self, [], null);
}
