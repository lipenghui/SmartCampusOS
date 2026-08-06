using IdentityService.Api.Common;
using Microsoft.AspNetCore.Http;
using SmartCampusOS.SharedKernel.Security;
using SmartCampusOS.SharedKernel.Users;

namespace IdentityService.Api.CurrentUser;

/// <summary>
/// 当前用户上下文实现：从网关透传的 X-User-Id / X-User-Roles / X-Data-Scope / X-Child-Id 头解析
/// （LLD §2.6.4 / §8.2）。服务内不解析 JWT，仅信任网关注入头。
/// </summary>
public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private string? Header(string name) =>
        _accessor.HttpContext?.Request.Headers.TryGetValue(name, out var value) == true
            ? value.ToString()
            : null;

    public bool IsAuthenticated => UserId.HasValue;

    public long? UserId => long.TryParse(Header(HeaderNames.UserId), out var id) ? id : null;

    /// <summary>学工号：网关上下文未透传，置 null（需要时由用户服务查询）。</summary>
    public string? UserNo => null;

    /// <summary>显示姓名：网关上下文未透传，置 null。</summary>
    public string? DisplayName => null;

    public IReadOnlyList<string> Roles =>
        Header(HeaderNames.UserRoles)?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

    /// <summary>数据范围：解析失败按最小可见范围 SELF 兜底（LLD §8.2）。</summary>
    public DataScope DataScope => DataScopeParser.Parse(Header(HeaderNames.DataScope), DataScope.Self);

    /// <summary>家长端当前访问的子女用户 ID（BR-04）。</summary>
    public long? ActiveChildId => long.TryParse(Header(HeaderNames.ChildId), out var id) ? id : null;
}
