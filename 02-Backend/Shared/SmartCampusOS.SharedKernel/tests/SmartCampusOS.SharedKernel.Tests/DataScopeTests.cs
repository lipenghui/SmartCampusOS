using SmartCampusOS.SharedKernel.Security;

namespace SmartCampusOS.SharedKernel.Tests;

public class DataScopeTests
{
    [Theory]
    [InlineData("ALL", DataScope.All)]
    [InlineData("GRADE", DataScope.Grade)]
    [InlineData("DEPT", DataScope.DeptClass)]
    [InlineData("CLASS", DataScope.DeptClass)]
    [InlineData("DEPT_CLASS", DataScope.DeptClass)]
    [InlineData("SELF", DataScope.Self)]
    [InlineData("all", DataScope.All)]       // 不区分大小写
    [InlineData("Dept", DataScope.DeptClass)]
    public void TryParse_网关透传值(string input, DataScope expected)
    {
        Assert.True(DataScopeParser.TryParse(input, out DataScope scope));
        Assert.Equal(expected, scope);
    }

    [Fact]
    public void TryParse_枚举名可解析()
    {
        Assert.True(DataScopeParser.TryParse(nameof(DataScope.Grade), out DataScope scope));
        Assert.Equal(DataScope.Grade, scope);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("UNKNOWN")]
    public void TryParse_非法值失败(string? input)
    {
        Assert.False(DataScopeParser.TryParse(input, out _));
    }

    [Fact]
    public void Parse_非法值回退到Self最小范围()
    {
        Assert.Equal(DataScope.Self, DataScopeParser.Parse("垃圾值"));
        Assert.Equal(DataScope.Self, DataScopeParser.Parse(null));
    }

    [Fact]
    public void Parse_显式回退值()
    {
        Assert.Equal(DataScope.All, DataScopeParser.Parse("垃圾值", DataScope.All));
    }
}
