namespace IdentityService.Domain.Common;

/// <summary>
/// 表名标注：Infrastructure 层 FreeSql 映射据此设置表名（对齐 LLD §4.2 identity_db 表清单）。
/// 使用本服务自有特性，避免 Domain 引用 FreeSql（LLD §2.6.4 Domain 零第三方依赖）。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class EntityTableAttribute(string tableName) : Attribute
{
    /// <summary>数据库表名（如 sys_user）。</summary>
    public string TableName { get; } = tableName;
}
