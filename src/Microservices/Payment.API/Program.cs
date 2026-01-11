using Payment.API.Data;
using Payment.API.Services;
using EventBus.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
builder.Services.AddDbContext<PaymentContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PaymentDB")));

// Add HttpClient for calling Order API
builder.Services.AddHttpClient("OrderApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["OrderApi:BaseUrl"]);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Add services
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Add EventBus
builder.Services.AddEventBus(builder.Configuration);

// Add Authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Identity:Authority"];
        options.RequireHttpsMetadata = false;
        options.Audience = "payment.api";
    });

// Add Authorization
builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();