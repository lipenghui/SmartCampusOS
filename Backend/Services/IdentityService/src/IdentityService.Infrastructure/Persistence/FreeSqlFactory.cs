using System.Reflection;
using FreeSql;
using FreeSql.Internal;
using IdentityService.Domain.Common;
using IdentityService.Application.Configuration;
using SmartCampusOS.SharedKernel.Ids;

namespace IdentityService.Infrastructure.Persistence;

/// <summary>
/// FreeSql 实例工厂（LLD §2.4 数据访问选型）：
/// CodeFirst 结构同步（LLD §10.4）、PascalCase→snake_case 列名转换（对齐 §4.2）、
/// 表名读取 Domain 的 [EntityTable] 特性（sys_* 前缀）、枚举映射为整数存储（§4.2 状态 TINYINT/整数）。
/// </summary>
public static class FreeSqlFactory
{
    public static IFreeSql Build(IdentityOptions options)
    {
        var fsql = new FreeSqlBuilder()
            .UseConnectionString(DataType.MySql, options.Database.ConnectionString)
            .UseAutoSyncStructure(options.Database.SyncStructureOnStartup)
            .UseNameConvert(NameConvertType.PascalCaseToUnderscoreWithLower)
            .Build();

        // 表名：读取 Domain [EntityTable("sys_user")] 特性，对齐 LLD §4.2 identity_db 表清单
        fsql.Aop.ConfigEntity += (_, e) =>
        {
            var attr = e.EntityType.GetCustomAttribute<EntityTableAttribute>();
            if (attr is not null)
            {
                e.ModifyResult.Name = attr.TableName;
            }
        };

        // 枚举映射为整数存储（LLD §4.2：状态/类型用整数 + 数据字典解释）
        // DateTimeOffset → DateTime（MySQL 无原生 DateTimeOffset，存 UTC datetime）
        fsql.Aop.ConfigEntityProperty += (_, e) =>
        {
            if (e.Property.PropertyType.IsEnum)
            {
                e.ModifyResult.MapType = typeof(int);
            }
            else if (e.Property.PropertyType == typeof(DateTimeOffset)
                     || e.Property.PropertyType == typeof(DateTimeOffset?))
            {
                e.ModifyResult.MapType = typeof(DateTime);
            }
        };

        // 主键自动生成：雪花 ID（LLD §1.4），Id 为 0 时插入前生成
        fsql.Aop.AuditValue += (_, e) =>
        {
            if (e.Column.CsType == typeof(long) && e.Property.Name == "Id"
                && e.Value is long id && id == 0)
            {
                e.Value = SnowflakeId.Default.NextId();
            }
        };

        return fsql;
    }
}
