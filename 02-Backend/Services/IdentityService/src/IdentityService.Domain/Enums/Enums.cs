namespace IdentityService.Domain.Enums;

/// <summary>用户类型（LLD §4.2 sys_user.user_type）。</summary>
public enum UserType
{
    /// <summary>学生。</summary>
    Student = 1,

    /// <summary>教师。</summary>
    Teacher = 2,

    /// <summary>家长。</summary>
    Parent = 3,

    /// <summary>教职工/行政人员。</summary>
    Staff = 4,
}

/// <summary>账号状态（LLD §4.2 sys_user.status）。</summary>
public enum UserStatus
{
    /// <summary>未激活（初始创建，待激活）。</summary>
    Inactive = 0,

    /// <summary>正常（激活/启用）。</summary>
    Active = 1,

    /// <summary>停用。</summary>
    Disabled = 2,

    /// <summary>锁定（连续登录失败等）。</summary>
    Locked = 3,
}

/// <summary>学生学籍状态（LLD §4.2 sys_student_profile.status：在读/休学/毕业）。</summary>
public enum StudentStatus
{
    /// <summary>在读。</summary>
    Studying = 1,

    /// <summary>休学。</summary>
    Suspended = 2,

    /// <summary>毕业。</summary>
    Graduated = 3,
}

/// <summary>组织类型（LLD §4.2 sys_org.org_type：school/grade/class/dept）。</summary>
public enum OrgType
{
    /// <summary>学校。</summary>
    School = 1,

    /// <summary>年级。</summary>
    Grade = 2,

    /// <summary>班级。</summary>
    Class = 3,

    /// <summary>部门。</summary>
    Dept = 4,
}

/// <summary>家长-子女关系（LLD §4.2 sys_parent_binding.relation）。</summary>
public enum ParentRelation
{
    /// <summary>父亲。</summary>
    Father = 1,

    /// <summary>母亲。</summary>
    Mother = 2,

    /// <summary>其他监护人。</summary>
    Other = 3,
}

/// <summary>家长绑定审核状态（BR-04，LLD §4.2 sys_parent_binding.audit_status）。</summary>
public enum AuditStatus
{
    /// <summary>待审核。</summary>
    Pending = 0,

    /// <summary>已通过。</summary>
    Approved = 1,

    /// <summary>已驳回。</summary>
    Rejected = 2,
}
