using SmartCampusOS.SharedKernel.Security;
using SmartCampusOS.SharedKernel.Users;

namespace SmartCampusOS.SharedKernel.Tests;

public class CurrentUserTests
{
    [Fact]
    public void 已认证用户_完整上下文()
    {
        var user = new CurrentUser(
            UserId: 1001,
            UserNo: "T2026001",
            DisplayName: "张老师",
            DataScope: DataScope.Grade,
            Roles: ["teacher", "head_teacher"]);

        Assert.True(user.IsAuthenticated);
        Assert.Equal(1001, user.UserId);
        Assert.Equal("T2026001", user.UserNo);
        Assert.Equal(DataScope.Grade, user.DataScope);
        Assert.Equal(["teacher", "head_teacher"], user.Roles);
        Assert.Null(user.ActiveChildId);
    }

    [Fact]
    public void 家长访问子女_携带ActiveChildId()
    {
        var parent = new CurrentUser(
            UserId: 2001,
            UserNo: "P00001",
            DisplayName: "王家长",
            DataScope: DataScope.Self,
            Roles: ["parent"],
            ActiveChildId: 3001);

        Assert.Equal(3001, parent.ActiveChildId);
    }

    [Fact]
    public void 匿名用户_未认证且数据范围最小()
    {
        var anonymous = CurrentUser.Anonymous;

        Assert.False(anonymous.IsAuthenticated);
        Assert.Null(anonymous.UserId);
        Assert.Equal(DataScope.Self, anonymous.DataScope);
        Assert.Empty(anonymous.Roles);
    }

    [Fact]
    public void 可作ICurrentUser接口使用()
    {
        ICurrentUser user = new CurrentUser(
            UserId: 1,
            UserNo: "S001",
            DisplayName: "李同学",
            DataScope: DataScope.Self,
            Roles: ["student"]);

        Assert.True(user.IsAuthenticated);
        Assert.Equal(DataScope.Self, user.DataScope);
    }
}
