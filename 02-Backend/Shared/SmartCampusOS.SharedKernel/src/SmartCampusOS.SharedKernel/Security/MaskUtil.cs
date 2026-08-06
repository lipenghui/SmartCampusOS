namespace SmartCampusOS.SharedKernel.Security;

/// <summary>
/// 敏感数据脱敏工具，对齐 LLD §8.3.1：敏感字段（手机号、身份证号等）展示层脱敏。
/// </summary>
/// <remarks>
/// 规则：<c>null</c>/空白输入原样返回（不抛异常），便于展示层直接调用；
/// 长度不足时保底保留首位并全掩其余字符，保证绝不泄露中间位。
/// </remarks>
public static class MaskUtil
{
    /// <summary>掩码字符。</summary>
    public const char DefaultMaskChar = '*';

    /// <summary>
    /// 手机号脱敏：保留前 3 位与后 4 位，如 <c>138****5678</c>。
    /// </summary>
    public static string? MaskPhone(string? phone) => Mask(phone, keepHead: 3, keepTail: 4);

    /// <summary>
    /// 身份证号脱敏：保留前 3 位与后 4 位，如 <c>110***********1234</c>。
    /// </summary>
    public static string? MaskIdCard(string? idCard) => Mask(idCard, keepHead: 3, keepTail: 4);

    /// <summary>
    /// 姓名脱敏：仅保留姓（首位），如 <c>张三</c> → <c>张*</c>、<c>欧阳娜娜</c> → <c>欧***</c>。
    /// </summary>
    public static string? MaskName(string? name) => Mask(name, keepHead: 1, keepTail: 0);

    /// <summary>
    /// 邮箱脱敏：保留用户名首字符与完整域名，如 <c>zhangsan@example.com</c> → <c>z******@example.com</c>。
    /// </summary>
    public static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return email;
        }

        string[] parts = email.Split('@', 2);
        string local = parts[0];
        string domain = parts[1];
        string head = local.Length > 0 ? local[..1] : "";
        return head + new string(DefaultMaskChar, Math.Max(local.Length - 1, 1)) + "@" + domain;
    }

    /// <summary>
    /// 通用脱敏：保留前 <paramref name="keepHead"/> 位与后 <paramref name="keepTail"/> 位，中间以掩码字符填充。
    /// </summary>
    /// <param name="value">原始值；<c>null</c>/空白原样返回。</param>
    /// <param name="keepHead">头部保留字符数。</param>
    /// <param name="keepTail">尾部保留字符数。</param>
    /// <param name="maskChar">掩码字符，默认 <c>*</c>。</param>
    public static string? Mask(string? value, int keepHead, int keepTail = 0, char maskChar = DefaultMaskChar)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (keepHead < 0 || keepTail < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(keepHead), "保留位数不能为负");
        }

        // 保底规则：长度不足以同时保留头尾时，仅保留首位，其余全部掩码（长度为 1 时不再追加掩码）
        if (value.Length <= keepHead + keepTail)
        {
            return keepHead > 0
                ? value[..1] + new string(maskChar, Math.Max(value.Length - 1, 0))
                : new string(maskChar, value.Length);
        }

        string head = value[..keepHead];
        string tail = keepTail > 0 ? value[^keepTail..] : "";
        return head + new string(maskChar, value.Length - keepHead - keepTail) + tail;
    }
}
