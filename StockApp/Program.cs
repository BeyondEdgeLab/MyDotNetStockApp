using StockApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IStockService, YahooFinanceStockService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 🚫 DO NOT use HTTPS redirection on Railway
// app.UseHttpsRedirection();

app.UseAuthorization();

// Swagger (explicitly allowed)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "StockApp API v1");
    c.RoutePrefix = "swagger"; // default
});

app.MapControllers();
app.Run();