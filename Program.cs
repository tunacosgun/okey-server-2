using GameServer.Hubs;
using GameServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Hizmetler
builder.Services.AddSingleton<TableService>();               // mevcut table servisin
builder.Services.AddSignalR(o => o.EnableDetailedErrors = true);

// CORS (dev/test – prod'da domain kısıtla)
builder.Services.AddCors(o => o.AddPolicy("dev", p =>
    p.AllowAnyHeader()
     .AllowAnyMethod()
     .AllowAnyOrigin()
));

var app = builder.Build();

app.UseCors("dev");

app.MapGet("/health", () => Results.Ok("OK"));
app.MapHub<GameHub>("/hub");

app.Run();