using IdentityService.Domain.Common;
using IdentityService.Domain.Enums;

namespace IdentityService.Domain.Entities;

/// <summary>
/// 学生档案实体（表 sys_student_profile，LLD §4.2）：班级、年级、入学日期、学籍状态。
/// </summary>
[EntityTable("sys_student_profile")]
public sealed class StudentProfile : AuditableEntity
{
    /// <summary>学生用户 ID。</summary>
    public long UserId { get; set; }

    /// <summary>班级组织 ID（sys_org）。</summary>
    public long? ClassId { get; set; }

    /// <summary>年级组织 ID（sys_org）。</summary>
    public long? GradeId { get; set; }

    /// <summary>入学日期。</summary>
    public DateTime? EnrollDate { get; set; }

    /// <summary>学籍状态（在读/休学/毕业）。</summary>
    public StudentStatus Status { get; set; } = StudentStatus.Studying;
}
