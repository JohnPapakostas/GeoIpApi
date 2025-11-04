using GeoIpApi.Data;
using GeoIpApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<IGeoIpClient, GeoIpClient>(client =>
{
    client.BaseAddress = new Uri("https://freegeoip.app/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Configure DbContext with SQL Server
builder.Services.AddDbContext<GeoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register background task queue and worker
builder.Services.AddSingleton<IBgTaskQueue, BgTaskQueue>();
builder.Services.AddHostedService<BatchWorker>();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
