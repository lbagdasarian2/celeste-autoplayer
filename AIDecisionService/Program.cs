using AIDecisionService.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure to listen on port 5001
builder.WebHost.UseUrls("http://localhost:5001");

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the AI decision service (use fully qualified name to avoid namespace collision)
builder.Services.AddScoped<AIDecisionService.Services.AIDecisionService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("AI Decision Service starting on http://localhost:5001");
Console.WriteLine("API: POST http://localhost:5001/api/decision/decide");
Console.WriteLine("Health: GET http://localhost:5001/api/decision/health");

app.Run();
