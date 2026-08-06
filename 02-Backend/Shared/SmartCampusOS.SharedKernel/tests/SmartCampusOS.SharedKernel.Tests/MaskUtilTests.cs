using SmartCampusOS.SharedKernel.Security;

namespace SmartCampusOS.SharedKernel.Tests;

public class MaskUtilTests
{
    [Theory]
    [InlineData("13812345678", "138****5678")]
    [InlineData("10086", "1****")]
    [InlineData(null, null)]
    [InlineData("", "")]
    public void MaskPhone_符合预期(string? input, string? expected)
    {
        Assert.Equal(expected, MaskUtil.MaskPhone(input));
    }

    [Theory]
    [InlineData("110101199003071234", "110***********1234")]
    [InlineData(null, null)]
    public void MaskIdCard_符合预期(string? input, string? expected)
    {
        Assert.Equal(expected, MaskUtil.MaskIdCard(input));
    }

    [Theory]
    [InlineData("张三", "张*")]
    [InlineData("欧阳娜娜", "欧***")]
    [InlineData("李", "李")]
    [InlineData(null, null)]
    public void MaskName_符合预期(string? input, string? expected)
    {
        Assert.Equal(expected, MaskUtil.MaskName(input));
    }

    [Theory]
    [InlineData("zhangsan@example.com", "z*******@example.com")]
    [InlineData("ab@test.cn", "a*@test.cn")]
    [InlineData("no-at-sign", "no-at-sign")]
    [InlineData(null, null)]
    public void MaskEmail_符合预期(string? input, string? expected)
    {
        Assert.Equal(expected, MaskUtil.MaskEmail(input));
    }

    [Fact]
    public void Mask_自定义保留位数()
    {
        Assert.Equal("12*****89", MaskUtil.Mask("123456789", keepHead: 2, keepTail: 2));
    }

    [Fact]
    public void Mask_长度不足以保留头尾时保底保留首位()
    {
        // 长度 4，需要保留 3+4=7 位 → 保底只留首位
        Assert.Equal("1***", MaskUtil.Mask("1234", keepHead: 3, keepTail: 4));
    }

    [Fact]
    public void Mask_负保留位数抛异常()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MaskUtil.Mask("123", keepHead: -1));
    }
}
