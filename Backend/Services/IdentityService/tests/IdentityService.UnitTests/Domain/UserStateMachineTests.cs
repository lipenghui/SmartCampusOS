using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;

namespace IdentityService.UnitTests.Domain;

public sealed class UserStateMachineTests
{
    private static User NewUser() => new()
    {
        Id = 1,
        UserNo = "S001",
        RealName = "张三",
        Status = UserStatus.Inactive,
        UserType = UserType.Student
    };

    [Fact]
    public void Activate_Should_Move_Inactive_To_Active()
    {
        var user = NewUser();
        user.Activate();
        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact]
    public void Activate_Active_User_Should_Throw()
    {
        var user = NewUser();
        user.Activate();
        Assert.Throws<InvalidOperationException>(() => user.Activate());
    }

    [Fact]
    public void Activate_Disabled_Or_Locked_Should_Throw()
    {
        var disabled = NewUser();
        disabled.Activate();
        disabled.Disable();
        Assert.Throws<InvalidOperationException>(() => disabled.Activate());

        var locked = NewUser();
        locked.Lock();
        Assert.Throws<InvalidOperationException>(() => locked.Activate());
    }

    [Fact]
    public void Disable_Enable_Lock_Unlock_Should_Follow_State_Machine()
    {
        var user = NewUser();
        user.Activate();
        user.Disable();
        Assert.Equal(UserStatus.Disabled, user.Status);
        user.Enable();
        Assert.Equal(UserStatus.Active, user.Status);
        user.Lock();
        Assert.Equal(UserStatus.Locked, user.Status);
        user.Unlock();
        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact]
    public void Locked_User_Should_Not_Allow_Login_Attempt()
    {
        var user = NewUser();
        user.Lock();
        Assert.False(user.CanAttemptLogin);
    }
}
