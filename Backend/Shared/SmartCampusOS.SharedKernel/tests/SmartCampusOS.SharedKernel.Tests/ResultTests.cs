using SmartCampusOS.SharedKernel.Results;

namespace SmartCampusOS.SharedKernel.Tests;

public class ResultTests
{
    [Fact]
    public void Ok_成功状态无错误码()
    {
        Result result = Result.Ok();

        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Fail_携带错误码与消息()
    {
        Result result = Result.Fail(ErrorCodes.EduScheduleConflict, "排课冲突");

        Assert.False(result.IsSuccess);
        Assert.Equal("EDU-2001", result.ErrorCode);
        Assert.Equal("排课冲突", result.ErrorMessage);
    }

    [Fact]
    public void ThrowIfFailed_失败时抛出()
    {
        Result result = Result.Fail(ErrorCodes.CommonValidationFailed, "参数错误");
        Assert.Throws<InvalidOperationException>(result.ThrowIfFailed);
    }

    [Fact]
    public void ThrowIfFailed_成功时不抛出()
    {
        Result.Ok().ThrowIfFailed(); // 不应抛异常
    }
}

public class ResultOfTTests
{
    [Fact]
    public void Ok_携带载荷()
    {
        Result<int> result = Result<int>.Ok(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Equal(42, result.GetValueOrThrow());
    }

    [Fact]
    public void Fail_无载荷携带错误码()
    {
        Result<int> result = Result<int>.Fail(ErrorCodes.EduSelectionFull, "选课名额已满");

        Assert.False(result.IsSuccess);
        Assert.Equal(default, result.Value);
        Assert.Equal("EDU-2002", result.ErrorCode);
        Assert.Equal("选课名额已满", result.ErrorMessage);
    }

    [Fact]
    public void GetValueOrThrow_失败时抛出()
    {
        Result<int> result = Result<int>.Fail(ErrorCodes.CommonNotFound, "数据不存在");
        Assert.Throws<InvalidOperationException>(() => result.GetValueOrThrow());
    }

    [Fact]
    public void GetValueOrDefault_失败时返回兜底值()
    {
        Result<int> result = Result<int>.Fail(ErrorCodes.CommonNotFound, "数据不存在");
        Assert.Equal(-1, result.GetValueOrDefault(-1));
    }

    [Fact]
    public void 隐式转换_值可直接作成功结果()
    {
        Result<string> result = "hello";
        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Match_分别处理成功失败()
    {
        string ok = Result<int>.Ok(7).Match(v => $"值:{v}", (_, _) => "失败");
        string fail = Result<int>.Fail(ErrorCodes.CommonNotFound, "缺失").Match(_ => "成功", (c, m) => $"{c}:{m}");

        Assert.Equal("值:7", ok);
        Assert.Equal("COMMON-2003:缺失", fail);
    }
}

public class ApiResponseTests
{
    [Fact]
    public void Ok_code为0且携带data()
    {
        var response = ApiResponse<string>.Ok("hi");

        Assert.Equal("0", response.Code);
        Assert.Equal("ok", response.Message);
        Assert.Equal("hi", response.Data);
        Assert.True(response.IsSuccess);
    }

    [Fact]
    public void Fail_code为业务错误码()
    {
        var response = ApiResponse.Fail(ErrorCodes.AuthForbidden, "无权限");

        Assert.Equal("AUTH-1003", response.Code);
        Assert.Equal("无权限", response.Message);
        Assert.False(response.IsSuccess);
    }

    [Fact]
    public void From_成功结果透传载荷()
    {
        var response = ApiResponse<string>.From(Result<string>.Ok("data"));

        Assert.Equal("0", response.Code);
        Assert.Equal("data", response.Data);
    }

    [Fact]
    public void From_失败结果透传错误码()
    {
        var response = ApiResponse<string>.From(Result<string>.Fail(ErrorCodes.DormBedUnavailable, "床位不可用"));

        Assert.Equal("DORM-3001", response.Code);
        Assert.Equal("床位不可用", response.Message);
        Assert.Null(response.Data);
    }

    [Fact]
    public void 序列化为统一响应结构()
    {
        var response = ApiResponse<int>.Ok(5);
        string json = System.Text.Json.JsonSerializer.Serialize(
            response,
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

        Assert.Contains("\"code\":\"0\"", json);
        Assert.Contains("\"message\":\"ok\"", json);
        Assert.Contains("\"data\":5", json);
    }
}

public class PagedResultTests
{
    [Fact]
    public void From_计算总页数与下一页()
    {
        var result = PagedResult<int>.From([1, 2, 3], total: 25, page: 2, pageSize: 10);

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(25, result.Total);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNext);
    }

    [Fact]
    public void Empty_空页()
    {
        var result = PagedResult<int>.Empty(page: 1, pageSize: 10);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
        Assert.False(result.HasNext);
    }
}
