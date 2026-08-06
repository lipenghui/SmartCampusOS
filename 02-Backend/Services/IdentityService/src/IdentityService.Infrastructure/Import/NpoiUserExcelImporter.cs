using IdentityService.Application.Abstractions.Import;
using IdentityService.Application.DTOs.Users;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace IdentityService.Infrastructure.Import;

/// <summary>
/// 用户 Excel 导入解析实现（LLD §3.2 批量导入；NPOI）。
/// 模板列：学工号、姓名、手机号、用户类型（学生/教师/家长/教职工）、初始密码（可空）。
/// 首行为表头，从第 2 行开始读取。
/// </summary>
public sealed class NpoiUserExcelImporter : IUserExcelImporter
{
    public IReadOnlyList<UserImportRow> Parse(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var workbook = new XSSFWorkbook(stream);
        var sheet = workbook.GetSheetAt(0);
        if (sheet is null)
        {
            return [];
        }

        var rows = new List<UserImportRow>();
        for (var i = sheet.FirstRowNum + 1; i <= sheet.LastRowNum; i++)
        {
            var row = sheet.GetRow(i);
            if (row is null)
            {
                continue;
            }

            var userNo = GetCellString(row, 0);
            if (string.IsNullOrWhiteSpace(userNo))
            {
                continue; // 学工号为空视为空行
            }

            rows.Add(new UserImportRow(
                userNo,
                GetCellString(row, 1),
                string.IsNullOrWhiteSpace(GetCellString(row, 2)) ? null : GetCellString(row, 2),
                GetCellString(row, 3),
                string.IsNullOrWhiteSpace(GetCellString(row, 4)) ? null : GetCellString(row, 4)));
        }

        return rows;
    }

    private static string GetCellString(IRow row, int index)
    {
        var cell = row.GetCell(index);
        if (cell is null)
        {
            return string.Empty;
        }

        return cell.CellType switch
        {
            CellType.Numeric => cell.NumericCellValue.ToString("0"),
            CellType.Formula => cell.CachedFormulaResultType == CellType.Numeric
                ? cell.NumericCellValue.ToString("0")
                : cell.StringCellValue,
            _ => cell.ToString() ?? string.Empty
        };
    }
}
