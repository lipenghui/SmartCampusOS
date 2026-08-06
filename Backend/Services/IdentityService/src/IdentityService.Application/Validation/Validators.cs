using System.Text.RegularExpressions;
using FluentValidation;
using IdentityService.Application.DTOs.Auth;
using IdentityService.Application.DTOs.Orgs;
using IdentityService.Application.DTOs.Roles;
using IdentityService.Application.DTOs.Users;

namespace IdentityService.Application.Validation;

/// <summary>登录请求校验（LLD §5.1.4 / §6.1）。</summary>
public sealed partial class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.GrantType)
            .Must(g => g is "password" or "captcha")
            .WithMessage("登录方式必须为 password 或 captcha");

        When(x => x.GrantType == "password", () =>
        {
            RuleFor(x => x.UserNo).NotEmpty().WithMessage("学工号不能为空");
            RuleFor(x => x.Password).NotEmpty().WithMessage("密码不能为空");
        });

        When(x => x.GrantType == "captcha", () =>
        {
            RuleFor(x => x.Mobile).NotEmpty().Matches(MobileRegex()).WithMessage("手机号格式不正确");
            RuleFor(x => x.Code).NotEmpty().WithMessage("验证码不能为空");
        });
    }

    [GeneratedRegex(@"^1[3-9]\d{9}$")]
    private static partial Regex MobileRegex();
}

/// <summary>发送验证码请求校验。</summary>
public sealed class SendCaptchaRequestValidator : AbstractValidator<SendCaptchaRequest>
{
    public SendCaptchaRequestValidator()
    {
        RuleFor(x => x.Mobile).NotEmpty().Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确");
    }
}

/// <summary>刷新令牌请求校验。</summary>
public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("刷新令牌不能为空");
    }
}

/// <summary>创建用户请求校验（LLD §3.2 / Common-02）。</summary>
public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.UserNo).NotEmpty().MaximumLength(32).WithMessage("学工号不能为空且不超过 32 字符");
        RuleFor(x => x.RealName).NotEmpty().MaximumLength(64).WithMessage("姓名不能为空");
        RuleFor(x => x.UserType).IsInEnum().WithMessage("用户类型不合法");
        When(x => !string.IsNullOrWhiteSpace(x.Mobile), () =>
        {
            RuleFor(x => x.Mobile).Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确");
        });
        When(x => !string.IsNullOrWhiteSpace(x.Password), () =>
        {
            RuleFor(x => x.Password).MinimumLength(6).WithMessage("密码至少 6 位");
        });
    }
}

/// <summary>更新用户请求校验。</summary>
public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.RealName).NotEmpty().MaximumLength(64).WithMessage("姓名不能为空");
        When(x => !string.IsNullOrWhiteSpace(x.Mobile), () =>
        {
            RuleFor(x => x.Mobile).Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确");
        });
        When(x => x.Status.HasValue, () =>
        {
            RuleFor(x => x.Status).Must(s => s is null || Enum.IsDefined(typeof(IdentityService.Domain.Enums.UserStatus), s))
                .WithMessage("账号状态不合法");
        });
    }
}

/// <summary>创建组织请求校验。</summary>
public sealed class CreateOrgRequestValidator : AbstractValidator<CreateOrgRequest>
{
    public CreateOrgRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64).WithMessage("组织名称不能为空");
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32).WithMessage("组织编码不能为空");
        RuleFor(x => x.OrgType).IsInEnum().WithMessage("组织类型不合法");
    }
}

/// <summary>更新组织请求校验。</summary>
public sealed class UpdateOrgRequestValidator : AbstractValidator<UpdateOrgRequest>
{
    public UpdateOrgRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64).WithMessage("组织名称不能为空");
    }
}

/// <summary>创建角色请求校验。</summary>
public sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32).WithMessage("角色编码不能为空");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64).WithMessage("角色名称不能为空");
        RuleFor(x => x.DataScope).IsInEnum().WithMessage("数据范围不合法");
    }
}

/// <summary>更新角色请求校验。</summary>
public sealed class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64).WithMessage("角色名称不能为空");
        RuleFor(x => x.DataScope).IsInEnum().WithMessage("数据范围不合法");
    }
}
