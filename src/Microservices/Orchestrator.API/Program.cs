using Asp.Versioning;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Orchestrator.API.Data;
using Orchestrator.API.StateMachines;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddDbContext<OrchestratorContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrchestratorDB")));

builder.Services.AddMassTransit(x =>
{
    // Saga state machine — heart of the orchestration
    x.AddSagaStateMachine<OrderSagaStateMachine, OrderSagaState>()
        .EntityFrameworkRepository(r =>
        {
            // Optimistic concurrency prevents double-processing of parallel events
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
            r.AddDbContext<DbContext, OrchestratorContext>((_, cfg) =>
                cfg.UseNpgsql(builder.Configuration.GetConnectionString("OrchestratorDB")));
        });

    // Transactional outbox — commands published by the saga are written
    // atomically with the saga state update, then forwarded by a background worker
    x.AddEntityFrameworkOutbox<OrchestratorContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.SetKebabCaseEndpointNameFormatter();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]);
            h.Password(builder.Configuration["RabbitMQ:Password"]);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/api/health");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrchestratorContext>();
    db.Database.EnsureCreated();
}

app.Run();
