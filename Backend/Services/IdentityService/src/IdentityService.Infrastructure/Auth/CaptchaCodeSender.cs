using IdentityService.Application.Abstractions.Auth;
using Microsoft.Extensions.Logging;
using SmartCampusOS.SharedKernel.Security;

namespace IdentityService.Infrastructure.Auth;

/// <summary>
/// 验证码发送实现：开发环境记录日志输出验证码（测试验证码兜底）。
/// 真实短信渠道为扩展点：实现 <see cref="ICodeSender"/> 接入短信服务商（LLD §3.2）。
/// </summary>
public sealed class CaptchaCodeSender(ILogger<CaptchaCodeSender> logger) : ICodeSender
{
    public Task SendAsync(string mobile, string code, CancellationToken ct = default)
    {
        logger.LogInformation("验证码已生成：手机号 {Mobile}，验证码 {Code}（开发模式，未接真实短信渠道）",
            MaskUtil.MaskPhone(mobile), code);
        return Task.CompletedTask;
    }
}
