namespace IdentityService.Application.Abstractions.Auth;

/// <summary>
/// 验证码发送渠道抽象（LLD §3.2 验证码登录）：实现为短信/其他渠道。
/// 开发环境使用测试验证码兜底，不接第三方（安全原语决策）。
/// </summary>
public interface ICodeSender
{
    /// <summary>发送验证码到指定手机号。</summary>
    Task SendAsync(string mobile, string code, CancellationToken ct = default);
}
