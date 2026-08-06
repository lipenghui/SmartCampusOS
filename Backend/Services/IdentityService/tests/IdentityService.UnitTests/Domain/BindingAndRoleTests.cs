using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;

namespace IdentityService.UnitTests.Domain;

public sealed class ParentBindingTests
{
    private static ParentBinding NewBinding() => new()
    {
        Id = 1,
        ParentUserId = 10,
        StudentUserId = 20,
        Relation = ParentRelation.Father,
        AuditStatus = AuditStatus.Pending
    };

    [Fact]
    public void Approve_Should_Set_Approved_And_Auditor()
    {
        var binding = NewBinding();
        binding.Approve(99);

        Assert.Equal(AuditStatus.Approved, binding.AuditStatus);
        Assert.Equal(99, binding.AuditedBy);
        Assert.NotNull(binding.AuditedAt);
    }

    [Fact]
    public void Reject_Should_Set_Rejected()
    {
        var binding = NewBinding();
        binding.Reject(99);

        Assert.Equal(AuditStatus.Rejected, binding.AuditStatus);
        Assert.Equal(99, binding.AuditedBy);
    }

    [Fact]
    public void Approve_Non_Pending_Should_Throw()
    {
        var binding = NewBinding();
        binding.Approve(99);
        Assert.Throws<InvalidOperationException>(() => binding.Approve(100));
        Assert.Throws<InvalidOperationException>(() => binding.Reject(100));
    }
}

public sealed class RoleTests
{
    [Fact]
    public void Builtin_Role_Code_Change_Should_Be_Blocked()
    {
        var role = new Role { Id = 1, Code = "admin", Name = "系统管理员", Builtin = true };
        Assert.Throws<InvalidOperationException>(() => role.EnsureCodeChangeAllowed("superadmin"));
        role.EnsureCodeChangeAllowed("admin"); // 同编码允许
    }

    [Fact]
    public void Builtin_Role_Delete_Should_Be_Blocked()
    {
        var role = new Role { Id = 1, Code = "admin", Builtin = true };
        Assert.Throws<InvalidOperationException>(() => role.EnsureDeletable());
    }

    [Fact]
    public void Custom_Role_Should_Be_Deletable()
    {
        var role = new Role { Id = 2, Code = "custom", Builtin = false };
        role.EnsureDeletable(); // 不抛异常
        role.EnsureCodeChangeAllowed("custom2");
    }
}

public sealed class RefreshTokenTests
{
    [Fact]
    public void Usable_When_Not_Revoked_And_Not_Expired()
    {
        var token = new RefreshToken
        {
            Id = 1,
            UserId = 1,
            TokenHash = "hash",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            Revoked = false
        };

        Assert.True(token.IsUsable(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Not_Usable_When_Expired_Or_Revoked()
    {
        var expired = new RefreshToken { ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1) };
        Assert.False(expired.IsUsable(DateTimeOffset.UtcNow));

        var revoked = new RefreshToken { ExpiresAt = DateTimeOffset.UtcNow.AddDays(1), Revoked = true };
        Assert.False(revoked.IsUsable(DateTimeOffset.UtcNow));
    }
}
