using System.Text.Json;
using SmartCampusOS.SharedKernel.Events;

namespace SmartCampusOS.SharedKernel.Tests;

public class IntegrationEventTests
{
    private sealed class FakeEvent : IntegrationEvent
    {
        public FakeEvent(string sourceService, string? eventType = null, Guid? eventId = null, DateTimeOffset? occurredAt = null)
            : base(sourceService, eventType, eventId, occurredAt)
        {
        }

        public string BizId { get; set; } = "";

        public int Count { get; set; }
    }

    [Fact]
    public void 默认值自动填充()
    {
        var e = new FakeEvent(ServiceNames.Edu);

        Assert.NotEqual(Guid.Empty, e.EventId);
        Assert.Equal(nameof(FakeEvent), e.EventType);
        Assert.Equal(ServiceNames.Edu, e.SourceService);
        Assert.Equal(DateTimeOffset.UtcNow.UtcDateTime, e.OccurredAt.UtcDateTime, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void 显式值优先()
    {
        var eventId = Guid.NewGuid();
        var occurredAt = new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero);
        var e = new FakeEvent(ServiceNames.Notice, eventType: "ScorePublished", eventId, occurredAt);

        Assert.Equal(eventId, e.EventId);
        Assert.Equal("ScorePublished", e.EventType);
        Assert.Equal(occurredAt, e.OccurredAt);
    }

    [Fact]
    public void 空来源服务抛异常()
    {
        Assert.Throws<ArgumentException>(() => new FakeEvent("  "));
    }

    [Fact]
    public void 序列化结构对齐LLD事件规范()
    {
        var e = new FakeEvent(ServiceNames.Edu, eventType: "ScorePublished")
        {
            BizId = "biz-1",
            Count = 3,
        };

        string json = JsonSerializer.Serialize(e, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("eventId", out _));
        Assert.Equal("ScorePublished", doc.RootElement.GetProperty("eventType").GetString());
        Assert.Equal(ServiceNames.Edu, doc.RootElement.GetProperty("sourceService").GetString());
        Assert.True(doc.RootElement.TryGetProperty("occurredAt", out _));
        // payload（派生类属性）平铺输出
        Assert.Equal("biz-1", doc.RootElement.GetProperty("bizId").GetString());
        Assert.Equal(3, doc.RootElement.GetProperty("count").GetInt32());
    }

    [Fact]
    public void ServiceNames_与网关路由前缀一致()
    {
        Assert.Equal("identity", ServiceNames.Identity);
        Assert.Equal("edu", ServiceNames.Edu);
        Assert.Equal("dorm", ServiceNames.Dorm);
        Assert.Equal("notice", ServiceNames.Notice);
        Assert.Equal("data", ServiceNames.Data);
        Assert.Equal("push", ServiceNames.Push);
        Assert.Equal("file", ServiceNames.File);
    }
}
