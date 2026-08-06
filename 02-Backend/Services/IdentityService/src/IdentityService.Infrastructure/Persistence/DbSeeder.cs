using System.Security.Cryptography;
using System.Text;
using FreeSql;
using IdentityService.Application.Abstractions.Security;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using IdentityService.Application.Configuration;
using Microsoft.Extensions.Logging;
using SmartCampusOS.SharedKernel.Security;

namespace IdentityService.Infrastructure.Persistence;

/// <summary>
/// 种子数据（幂等）：
/// 1. 系统基础：内置角色、系统管理员账号、基础数据字典（仅空表时写入）；
/// 2. 演示数据：扩展角色、权限点、角色-权限分配、组织架构（学校-年级-班级-部门）、
///    多类型人员（教师/学生/宿管/后勤/教务/校领导/家长）、家长-子女绑定。
/// 演示数据按唯一键（Code / UserNo / 组合键）判重，重复启动不会重复插入。
/// 演示账号统一初始密码：Test@123（README 中说明）。
/// </summary>
public sealed class DbSeeder
{
    /// <summary>演示账号统一初始密码。</summary>
    public const string DemoPassword = "Test@123";

    private readonly IFreeSql _fsql;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IFieldEncryptor _fieldEncryptor;
    private readonly SeedOptions _seed;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(
        IFreeSql fsql,
        IPasswordHasher passwordHasher,
        IFieldEncryptor fieldEncryptor,
        SeedOptions seed,
        ILogger<DbSeeder> logger)
    {
        _fsql = fsql;
        _passwordHasher = passwordHasher;
        _fieldEncryptor = fieldEncryptor;
        _seed = seed;
        _logger = logger;
    }

    public void Seed()
    {
        // 系统基础
        SeedRoles();
        SeedAdminUser();
        SeedDicts();

        // 演示数据
        SeedExtendedRoles();
        SeedPermissions();
        SeedRolePermissions();
        SeedOrganizations();
        SeedDemoUsers();
        SeedParentBindings();
    }

    // ============================================================
    // 系统基础
    // ============================================================

    private void SeedRoles()
    {
        if (_fsql.Select<Role>().Any())
        {
            return;
        }

        var roles = new[]
        {
            new Role { Id = 1, Code = "admin", Name = "系统管理员", DataScope = DataScope.All, Builtin = true },
            new Role { Id = 2, Code = "teacher", Name = "教师", DataScope = DataScope.DeptClass, Builtin = true },
            new Role { Id = 3, Code = "student", Name = "学生", DataScope = DataScope.Self, Builtin = true },
            new Role { Id = 4, Code = "parent", Name = "家长", DataScope = DataScope.Self, Builtin = true },
            new Role { Id = 5, Code = "staff", Name = "教职工", DataScope = DataScope.DeptClass, Builtin = true }
        };

        _fsql.Insert(roles).ExecuteAffrows();
        _logger.LogInformation("已初始化内置角色 {Count} 个", roles.Length);
    }

    /// <summary>扩展角色：宿管/后勤/教务/校领导/班主任（按 Code 判重，PRD §3.1 角色清单）。</summary>
    private void SeedExtendedRoles()
    {
        var candidates = new[]
        {
            new Role { Id = 6, Code = "dorm_admin", Name = "宿管", DataScope = DataScope.DeptClass, Builtin = true },
            new Role { Id = 7, Code = "logistics", Name = "后勤", DataScope = DataScope.DeptClass, Builtin = true },
            new Role { Id = 8, Code = "dean", Name = "教务管理员", DataScope = DataScope.Grade, Builtin = true },
            new Role { Id = 9, Code = "leader", Name = "校领导", DataScope = DataScope.All, Builtin = true },
            new Role { Id = 10, Code = "head_teacher", Name = "班主任", DataScope = DataScope.DeptClass, Builtin = true }
        };

        var toInsert = candidates.Where(r => !_fsql.Select<Role>().Any(x => x.Code == r.Code)).ToList();
        if (toInsert.Count > 0)
        {
            _fsql.Insert(toInsert).ExecuteAffrows();
            _logger.LogInformation("已初始化扩展角色 {Count} 个", toInsert.Count);
        }
    }

    private void SeedAdminUser()
    {
        if (_fsql.Select<User>().Any(u => u.UserNo == _seed.AdminUserNo))
        {
            return;
        }

        var passwordHash = _passwordHasher.Hash(_seed.AdminPassword, out var salt);
        var admin = new User
        {
            Id = 1,
            UserNo = _seed.AdminUserNo,
            PasswordHash = passwordHash,
            PasswordSalt = salt,
            UserType = UserType.Staff,
            Status = UserStatus.Active,
            RealName = _seed.AdminRealName
        };

        _fsql.Ado.Transaction(() =>
        {
            _fsql.Insert(admin).ExecuteAffrows();
            _fsql.Insert(new UserRole { Id = 1, UserId = admin.Id, RoleId = 1 }).ExecuteAffrows();
        });
        _logger.LogInformation("已初始化系统管理员账号 {UserNo}", _seed.AdminUserNo);
    }

    private void SeedDicts()
    {
        if (_fsql.Select<Dict>().Any())
        {
            return;
        }

        var dicts = new List<Dict>();
        var items = new List<DictItem>();
        long id = 1;

        void AddDict(string code, string name, (string itemCode, string itemName, int sort)[] entries)
        {
            dicts.Add(new Dict { Id = id++, Code = code, Name = name });
            foreach (var (itemCode, itemName, sort) in entries)
            {
                items.Add(new DictItem
                {
                    Id = id++,
                    DictCode = code,
                    ItemCode = itemCode,
                    ItemName = itemName,
                    Sort = sort
                });
            }
        }

        AddDict("user_status", "账号状态",
            [("0", "未激活", 1), ("1", "正常", 2), ("2", "停用", 3), ("3", "锁定", 4)]);
        AddDict("user_type", "用户类型",
            [("1", "学生", 1), ("2", "教师", 2), ("3", "家长", 3), ("4", "教职工", 4)]);
        AddDict("audit_status", "审核状态",
            [("0", "待审核", 1), ("1", "已通过", 2), ("2", "已驳回", 3)]);
        AddDict("org_type", "组织类型",
            [("1", "学校", 1), ("2", "年级", 2), ("3", "班级", 3), ("4", "部门", 4)]);
        AddDict("parent_relation", "家长关系",
            [("1", "父亲", 1), ("2", "母亲", 2), ("3", "其他监护人", 3)]);
        AddDict("data_scope", "数据范围",
            [("1", "全部", 1), ("2", "年级", 2), ("3", "部门/班级", 3), ("4", "本人", 4)]);
        AddDict("repair_category", "报修类别",
            [("水电", "水电", 1), ("门窗", "门窗", 2), ("网络", "网络", 3), ("设备", "设备", 4), ("其他", "其他", 5)]);
        AddDict("venue_type", "场地类型",
            [("教室", "教室", 1), ("会议室", "会议室", 2), ("体育场馆", "体育场馆", 3), ("活动场地", "活动场地", 4)]);

        _fsql.Ado.Transaction(() =>
        {
            _fsql.Insert(dicts).ExecuteAffrows();
            _fsql.Insert(items).ExecuteAffrows();
        });
        _logger.LogInformation("已初始化基础数据字典 {DictCount} 组 / {ItemCount} 项", dicts.Count, items.Count);
    }

    // ============================================================
    // 演示数据：权限点与角色-权限（RBAC，LLD §8.2）
    // ============================================================

    /// <summary>权限点编码与前端路由 meta.permission / v-permission 对齐（LLD §2.7.2）。</summary>
    private static readonly (string Code, string Name, string Module)[] PermissionDefs =
    [
        // 系统管理
        ("sys:user", "用户管理", "系统管理"),
        ("sys:org", "组织架构", "系统管理"),
        ("sys:role", "角色权限", "系统管理"),
        ("sys:dict", "数据字典", "系统管理"),
        ("sys:audit", "审计日志", "系统管理"),
        // 教务管理
        ("edu:semester", "学期计划", "教务管理"),
        ("edu:schedule", "课表管理", "教务管理"),
        ("edu:adjust", "调课审批", "教务管理"),
        ("edu:selection", "选课管理", "教务管理"),
        ("edu:score", "成绩管理", "教务管理"),
        ("edu:exam", "考试管理", "教务管理"),
        ("edu:student", "学籍档案", "教务管理"),
        // 宿舍与后勤
        ("dorm:building", "宿舍结构", "宿舍后勤"),
        ("dorm:assign", "入住管理", "宿舍后勤"),
        ("dorm:repair", "报修工单", "宿舍后勤"),
        ("dorm:venue", "场地预约", "宿舍后勤"),
        ("dorm:facility", "设施管理", "宿舍后勤"),
        // 通知与家校
        ("notice:announce", "公告发布", "通知家校"),
        ("notice:message", "消息中心", "通知家校"),
        ("notice:conversation", "家校会话", "通知家校"),
        // 数据可视化
        ("data:dashboard", "管理驾驶舱", "数据可视化"),
        ("data:report", "报表中心", "数据可视化"),
        // 学生服务（学生门户：登录管理端后展示本人信息）
        ("student:portal", "学生门户", "学生服务")
    ];

    /// <summary>各角色拥有的权限码（admin 在 SeedRolePermissions 中全量授予）。</summary>
    private static readonly Dictionary<string, string[]> RolePermissionMap = new()
    {
        ["teacher"] = ["edu:schedule", "edu:score", "edu:exam", "notice:announce", "notice:message", "notice:conversation"],
        ["student"] = ["student:portal"],
        ["parent"] = ["notice:message", "notice:conversation"],
        ["staff"] = ["sys:dict", "notice:message"],
        ["dorm_admin"] = ["dorm:building", "dorm:assign", "dorm:repair", "dorm:venue", "dorm:facility", "notice:message"],
        ["logistics"] = ["dorm:repair", "dorm:venue", "dorm:facility", "notice:announce", "notice:message"],
        ["dean"] = ["edu:semester", "edu:schedule", "edu:adjust", "edu:selection", "edu:score", "edu:exam", "edu:student", "notice:message"],
        ["leader"] = ["data:dashboard", "data:report", "edu:schedule", "edu:score", "dorm:repair", "dorm:venue", "notice:message"],
        ["head_teacher"] = ["notice:announce", "notice:message", "edu:score", "edu:schedule"]
    };

    private void SeedPermissions()
    {
        // 增量补种：按 Code 判重，已初始化的库也能补充新增权限点（旧表首次升级时全量写入）。
        var existing = _fsql.Select<Permission>().ToList();
        var existingCodes = existing.Select(p => p.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = PermissionDefs.Where(p => !existingCodes.Contains(p.Code)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        long nextId = existing.Count > 0 ? existing.Max(p => p.Id) + 1 : 1;
        var toInsert = missing
            .Select((p, i) => new Permission { Id = nextId + i, Code = p.Code, Name = p.Name, Module = p.Module })
            .ToList();
        _fsql.Insert(toInsert).ExecuteAffrows();
        _logger.LogInformation("已初始化/补种权限点 {Count} 个", toInsert.Count);
    }

    private void SeedRolePermissions()
    {
        var roles = _fsql.Select<Role>().ToList();
        var permissions = _fsql.Select<Permission>().ToList();
        if (roles.Count == 0 || permissions.Count == 0)
        {
            return;
        }

        var existing = _fsql.Select<RolePermission>().ToList()
            .Select(rp => (rp.RoleId, rp.PermissionId)).ToHashSet();

        var toInsert = new List<RolePermission>();
        long id = _fsql.Select<RolePermission>().Max(rp => (long?)rp.Id) ?? 0;

        foreach (var role in roles)
        {
            var codes = role.Code == "admin"
                ? permissions.Select(p => p.Code).ToArray()
                : RolePermissionMap.GetValueOrDefault(role.Code, []);
            foreach (var code in codes)
            {
                var permission = permissions.FirstOrDefault(p => p.Code == code);
                if (permission is null || existing.Contains((role.Id, permission.Id)))
                {
                    continue;
                }

                toInsert.Add(new RolePermission { Id = ++id, RoleId = role.Id, PermissionId = permission.Id });
                existing.Add((role.Id, permission.Id));
            }
        }

        if (toInsert.Count > 0)
        {
            _fsql.Insert(toInsert).ExecuteAffrows();
            _logger.LogInformation("已初始化角色-权限关联 {Count} 条", toInsert.Count);
        }
    }

    // ============================================================
    // 演示数据：组织架构（LLD §4.2 sys_org）
    // ============================================================

    private void SeedOrganizations()
    {
        if (_fsql.Select<OrgUnit>().Any())
        {
            return;
        }

        var orgs = new[]
        {
            // 学校
            new OrgUnit { Id = 1, ParentId = null, OrgType = OrgType.School, Name = "智慧中学", Code = "school" },
            // 年级
            new OrgUnit { Id = 2, ParentId = 1, OrgType = OrgType.Grade, Name = "2026 级（高一）", Code = "grade-2026" },
            new OrgUnit { Id = 3, ParentId = 1, OrgType = OrgType.Grade, Name = "2025 级（高二）", Code = "grade-2025" },
            new OrgUnit { Id = 4, ParentId = 1, OrgType = OrgType.Grade, Name = "2024 级（高三）", Code = "grade-2024" },
            // 班级
            new OrgUnit { Id = 5, ParentId = 2, OrgType = OrgType.Class, Name = "高一（1）班", Code = "class-2601" },
            new OrgUnit { Id = 6, ParentId = 2, OrgType = OrgType.Class, Name = "高一（2）班", Code = "class-2602" },
            new OrgUnit { Id = 7, ParentId = 3, OrgType = OrgType.Class, Name = "高二（1）班", Code = "class-2501" },
            new OrgUnit { Id = 8, ParentId = 4, OrgType = OrgType.Class, Name = "高三（1）班", Code = "class-2401" },
            // 部门
            new OrgUnit { Id = 9, ParentId = 1, OrgType = OrgType.Dept, Name = "教务处", Code = "dept-academic" },
            new OrgUnit { Id = 10, ParentId = 1, OrgType = OrgType.Dept, Name = "后勤处", Code = "dept-logistics" },
            new OrgUnit { Id = 11, ParentId = 1, OrgType = OrgType.Dept, Name = "学生处", Code = "dept-student" },
            new OrgUnit { Id = 12, ParentId = 1, OrgType = OrgType.Dept, Name = "信息中心", Code = "dept-it" }
        };

        _fsql.Insert(orgs).ExecuteAffrows();
        _logger.LogInformation("已初始化组织架构 {Count} 个节点（学校-年级-班级-部门）", orgs.Length);
    }

    // ============================================================
    // 演示数据：人员（教师/学生/宿管/后勤/教务/校领导/家长）
    // ============================================================

    /// <summary>演示用户定义：(Id, 学工号, 姓名, 用户类型, 手机号, 组织ID, 角色编码[])。</summary>
    private static readonly (long Id, string No, string Name, UserType Type, string Mobile, long OrgId, string[] Roles)[] UserDefs =
    [
        // 教师 / 班主任
        (2, "T1001", "张伟", UserType.Teacher, "13800138001", 5, ["teacher", "head_teacher"]),
        (3, "T1002", "李芳", UserType.Teacher, "13800138002", 6, ["teacher", "head_teacher"]),
        (4, "T1003", "王强", UserType.Teacher, "13800138003", 9, ["teacher"]),
        (5, "T1004", "赵敏", UserType.Teacher, "13800138004", 9, ["teacher"]),
        // 学生（高一（1）班 / 高一（2）班）
        (6, "S2026001", "王小明", UserType.Student, "13900139001", 5, ["student"]),
        (7, "S2026002", "李小红", UserType.Student, "13900139002", 5, ["student"]),
        (8, "S2026003", "张磊", UserType.Student, "13900139003", 5, ["student"]),
        (9, "S2026004", "赵雪", UserType.Student, "13900139004", 5, ["student"]),
        (10, "S2026005", "陈晨", UserType.Student, "13900139005", 5, ["student"]),
        (11, "S2026006", "刘洋", UserType.Student, "13900139006", 6, ["student"]),
        (12, "S2026007", "周婷", UserType.Student, "13900139007", 6, ["student"]),
        (13, "S2026008", "吴桐", UserType.Student, "13900139008", 6, ["student"]),
        (14, "S2026009", "郑浩", UserType.Student, "13900139009", 6, ["student"]),
        (15, "S2026010", "孙悦", UserType.Student, "13900139010", 6, ["student"]),
        // 宿管 / 后勤 / 教务 / 校领导
        (16, "D1001", "刘淑芬", UserType.Staff, "13700137001", 10, ["dorm_admin"]),
        (17, "L1001", "陈国强", UserType.Staff, "13700137002", 10, ["logistics"]),
        (18, "E1001", "孙志远", UserType.Staff, "13700137003", 9, ["dean"]),
        (19, "LD001", "周建国", UserType.Staff, "13600136001", 1, ["leader"]),
        // 家长
        (20, "P1001", "王小刚", UserType.Parent, "13500135001", 5, ["parent"]),
        (21, "P1002", "李美玲", UserType.Parent, "13500135002", 5, ["parent"]),
        (22, "P1003", "张桂芳", UserType.Parent, "13500135003", 5, ["parent"]),
        (23, "P1004", "赵大海", UserType.Parent, "13500135004", 5, ["parent"])
    ];

    private void SeedDemoUsers()
    {
        var existingNos = _fsql.Select<User>().ToList(u => u.UserNo).ToHashSet();
        var toInsert = UserDefs
            .Where(d => !existingNos.Contains(d.No))
            .ToList();
        if (toInsert.Count == 0)
        {
            return;
        }

        var passwordHash = _passwordHasher.Hash(DemoPassword, out var salt);
        var users = new List<User>();
        var userRoles = new List<UserRole>();
        var profiles = new List<StudentProfile>();
        long roleId = _fsql.Select<UserRole>().Max(ur => (long?)ur.Id) ?? 0;
        long profileId = _fsql.Select<StudentProfile>().Max(p => (long?)p.Id) ?? 0;

        foreach (var (id, no, name, type, mobile, orgId, roles) in toInsert)
        {
            users.Add(new User
            {
                Id = id,
                UserNo = no,
                RealName = name,
                UserType = type,
                Status = UserStatus.Active,
                PasswordHash = passwordHash,
                PasswordSalt = salt,
                MobileEncrypted = _fieldEncryptor.Encrypt(mobile),
                MobileHash = HashMobile(mobile)
            });

            foreach (var roleCode in roles)
            {
                var role = _fsql.Select<Role>().Where(r => r.Code == roleCode).First();
                userRoles.Add(new UserRole { Id = ++roleId, UserId = id, RoleId = role.Id });
            }

            if (type == UserType.Student)
            {
                profiles.Add(new StudentProfile
                {
                    Id = ++profileId,
                    UserId = id,
                    ClassId = orgId,
                    GradeId = orgId == 5 || orgId == 6 ? 2 : 3,
                    EnrollDate = new DateTime(2026, 9, 1),
                    Status = StudentStatus.Studying
                });
            }
        }

        _fsql.Ado.Transaction(() =>
        {
            _fsql.Insert(users).ExecuteAffrows();
            _fsql.Insert(userRoles).ExecuteAffrows();
            if (profiles.Count > 0)
            {
                _fsql.Insert(profiles).ExecuteAffrows();
            }
        });
        _logger.LogInformation("已初始化演示人员 {Count} 人（教师/学生/宿管/后勤/教务/校领导/家长，密码 {Password}）",
            users.Count, DemoPassword);
    }

    /// <summary>家长-子女绑定（BR-04）：3 条已通过 + 1 条待审核（演示审批流）。</summary>
    private void SeedParentBindings()
    {
        var defs = new[]
        {
            (ParentUserId: 20L, StudentUserId: 6L, Relation: ParentRelation.Father, Approved: true),
            (ParentUserId: 21L, StudentUserId: 7L, Relation: ParentRelation.Mother, Approved: true),
            (ParentUserId: 22L, StudentUserId: 8L, Relation: ParentRelation.Mother, Approved: true),
            (ParentUserId: 23L, StudentUserId: 9L, Relation: ParentRelation.Father, Approved: false)
        };

        var existing = _fsql.Select<ParentBinding>().ToList()
            .Select(b => (b.ParentUserId, b.StudentUserId)).ToHashSet();

        var toInsert = new List<ParentBinding>();
        long id = _fsql.Select<ParentBinding>().Max(b => (long?)b.Id) ?? 0;

        foreach (var (parentId, studentId, relation, approved) in defs)
        {
            if (existing.Contains((parentId, studentId)))
            {
                continue;
            }

            var binding = new ParentBinding
            {
                Id = ++id,
                ParentUserId = parentId,
                StudentUserId = studentId,
                Relation = relation,
                AuditStatus = AuditStatus.Pending,
                AppliedAt = DateTimeOffset.UtcNow.AddDays(-3)
            };
            if (approved)
            {
                binding.Approve(1); // 由系统管理员审核
            }

            toInsert.Add(binding);
        }

        if (toInsert.Count > 0)
        {
            _fsql.Insert(toInsert).ExecuteAffrows();
            _logger.LogInformation("已初始化家长-子女绑定 {Count} 条", toInsert.Count);
        }
    }

    /// <summary>手机号 SHA-256 十六进制哈希（与 UserService.HashMobile 一致：大写 hex，用于精确匹配）。</summary>
    private static string HashMobile(string mobile) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(mobile)));
}
