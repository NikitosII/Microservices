using EventBus.Extensions;
using MassTransit;
using Order.API.EventHandlers;

var builder = WebApplication.CreateBuilder(args);

// Add EventBus
builder.Services.AddEventBus(builder.Configuration);

// Configure consumers
builder.Services.AddMassTransitHostedService();

// Register event handlers
builder.Services.AddScoped<OrderCreatedEventHandler>();

var app = builder.Build();

// Configure MassTransit endpoints
var busControl = app.Services.GetRequiredService<IBusControl>();
busControl.Start();

app.Run();