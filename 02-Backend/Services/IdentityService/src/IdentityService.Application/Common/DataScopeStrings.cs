using SmartCampusOS.SharedKernel.Security;

namespace IdentityService.Application.Common;

/// <summary>
/// DataScope 字符串映射：统一输出大写格式（ALL / GRADE / DEPT / SELF），
/// 与 JWT claim、网关 X-Data-Scope 契约一致（LLD §8.2）。
/// </summary>
public static class DataScopeStrings
{
    public static string ToScopeString(DataScope scope) => scope switch
    {
        DataScope.All => "ALL",
        DataScope.Grade => "GRADE",
        DataScope.DeptClass => "DEPT",
        DataScope.Self => "SELF",
        _ => "SELF"
    };
}
