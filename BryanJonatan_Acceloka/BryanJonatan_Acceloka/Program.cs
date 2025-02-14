using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using BryanJonatan_Acceloka.Model;
using Serilog;
using System.Net;
using System.Text.Json;
using BryanJonatan_Acceloka;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
builder.Logging.AddSerilog(new LoggerConfiguration().MinimumLevel.Information()
    .WriteTo.File($"logs/Log-{DateTime.Now:yyyyMMdd}.txt")
    .CreateLogger());
// Add services to the container.
builder.Services.AddControllers();


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));


var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var errorFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (errorFeature != null)
        {
            var problemDetails = new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = "An unexpected error occurred!",
                status = context.Response.StatusCode,
                detail = errorFeature.Error.Message,
                instance = context.Request.Path
            };

            var errorJson = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(errorJson);
        }
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();




