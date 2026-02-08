using StockApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IStockService, YahooFinanceStockService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();

// Swagger UI (enable even in prod if you want)
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();