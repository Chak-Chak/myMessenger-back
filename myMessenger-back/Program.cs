using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using myMessenger_back.Models;
using AutoWrapper;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
/*IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();*/
//string connection = config.GetConnectionString("MessengerDB");
builder.Services.AddDbContext<ApplicationContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("MessengerDb")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseApiResponseAndExceptionWrapper(new AutoWrapperOptions { ShowIsErrorFlagForSuccessfulResponse = true });

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
