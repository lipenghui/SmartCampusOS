using IdentityService.Application.DTOs.Users;

namespace IdentityService.Application.Abstractions.Import;

/// <summary>
/// 用户 Excel 导入解析抽象（LLD §3.2 批量导入）：NPOI 实现位于 Infrastructure。
/// </summary>
public interface IUserExcelImporter
{
    /// <summary>解析 Excel 内容为用户导入行。</summary>
    IReadOnlyList<UserImportRow> Parse(byte[] content);
}
