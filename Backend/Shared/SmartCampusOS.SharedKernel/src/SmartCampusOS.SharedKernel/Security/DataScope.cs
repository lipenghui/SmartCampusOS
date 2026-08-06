namespace SmartCampusOS.SharedKernel.Security;

/// <summary>
/// 数据范围（DataScope），对齐 LLD §8.2 四档：ALL / GRADE / DEPT·CLASS / SELF。
/// </summary>
/// <remarks>
/// 网关解析角色数据范围并注入 <c>X-Data-Scope</c>，各服务在仓储层统一追加过滤条件，
/// 禁止在 Controller 层拼接（LLD §8.2）。见 <see cref="DataScopeParser"/>。
/// </remarks>
public enum DataScope
{
    /// <summary>全部数据（校领导/系统管理员）。</summary>
    All = 1,

    /// <summary>年级范围（年级负责人）。</summary>
    Grade = 2,

    /// <summary>部门/班级范围（部门负责人/班主任）。</summary>
    DeptClass = 3,

    /// <summary>本人/本人子女范围。</summary>
    Self = 4,
}

/// <summary>数据范围字符串解析：兼容 <c>X-Data-Scope</c> 网关透传值与枚举名。</summary>
public static class DataScopeParser
{
    /// <summary>
    /// 尝试解析数据范围字符串。
    /// </summary>
    /// <param name="raw">原始值，如 <c>ALL</c>、<c>GRADE</c>、<c>DEPT</c>、<c>CLASS</c>、<c>SELF</c> 或枚举名（不区分大小写）。</param>
    /// <param name="scope">解析成功时输出的数据范围。</param>
    /// <returns>解析成功返回 <c>true</c> 并输出 <paramref name="scope"/>。</returns>
    public static bool TryParse(string? raw, out DataScope scope)
    {
        scope = default;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        string normalized = raw.Trim().ToUpperInvariant();
        switch (normalized)
        {
            case "ALL":
                scope = DataScope.All;
                return true;
            case "GRADE":
                scope = DataScope.Grade;
                return true;
            case "DEPT":
            case "CLASS":
            case "DEPT_CLASS":
                scope = DataScope.DeptClass;
                return true;
            case "SELF":
                scope = DataScope.Self;
                return true;
            default:
                return Enum.TryParse(raw.Trim(), ignoreCase: true, out scope);
        }
    }

    /// <summary>解析数据范围字符串，失败时返回 <paramref name="fallback"/>（默认 <see cref="DataScope.Self"/>，最小可见范围兜底）。</summary>
    public static DataScope Parse(string? raw, DataScope fallback = DataScope.Self) =>
        TryParse(raw, out DataScope scope) ? scope : fallback;
}
