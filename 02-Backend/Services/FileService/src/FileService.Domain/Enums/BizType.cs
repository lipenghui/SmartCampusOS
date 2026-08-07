namespace FileService.Domain.Enums;

/// <summary>
/// 业务类型枚举（对应 file_object.biz_type 字段）。
/// </summary>
public enum BizType
{
    /// <summary>报修图片。</summary>
    Repair = 1,

    /// <summary>用户头像。</summary>
    Avatar = 2,

    /// <summary>公告附件。</summary>
    Announcement = 3,

    /// <summary>成绩导入。</summary>
    ScoreImport = 4,

    /// <summary>学生导入。</summary>
    StudentImport = 5,

    /// <summary>报表导出。</summary>
    ReportExport = 6,

    /// <summary>其他。</summary>
    Other = 99
}