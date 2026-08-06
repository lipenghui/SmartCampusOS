var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "SmartCampusOS FileService")
    .ExcludeFromDescription();

app.Run();

public partial class Program;
