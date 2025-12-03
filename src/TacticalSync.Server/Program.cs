var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // (PascalCase)
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        // Handle circular references (recommended)
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// CORS for development - allow all origins for dev
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Server startup

Console.WriteLine("[SERVER] Starting TacticalSync Server on http://localhost:5000...\n");

app.Run("http://localhost:5000");
