using Coupon.API.Data;
using Coupon.API.Services;
using Microsoft.EntityFrameworkCore;
using EventBus.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// Add DbContext
builder.Services.AddDbContext<CouponContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CouponDB")));

// Add services
builder.Services.AddScoped<ICouponService, CouponService>();

// Add EventBus
builder.Services.AddEventBus(builder.Configuration);

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
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/health");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CouponContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();