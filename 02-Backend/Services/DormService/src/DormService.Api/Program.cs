var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "SmartCampusOS DormService")
    .ExcludeFromDescription();

app.Run();

public partial class Program;
