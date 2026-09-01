using GarageFaultAssistant.Api.Api.AnalyseFault;
using GarageFaultAssistant.Api.Application.Common.DependencyInjection;
using GarageFaultAssistant.Api.Infrastructure.DependencyInjection;
using GarageFaultAssistant.Api.Infrastructure.ExceptionHandling;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
        options.AddPolicy("ViteDev", policy =>
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod()));
}

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseCors("ViteDev");
}

app.MapGet("/health", () => Results.Ok());
app.MapAnalyseFault();

app.Run();

public partial class Program;
