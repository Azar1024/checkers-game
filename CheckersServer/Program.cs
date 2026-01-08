using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true);
    });
});

var app = builder.Build();

app.UseCors();

// Привязываем сервер к 0.0.0.0:5000 для доступа из локальной сети
app.Urls.Add("http://0.0.0.0:5000");

app.MapHub<GameHub>("/gamehub");

app.Run();