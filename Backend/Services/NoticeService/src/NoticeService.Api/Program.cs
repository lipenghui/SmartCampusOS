var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "SmartCampusOS NoticeService")
    .ExcludeFromDescription();

app.Run();

public partial class Program;
