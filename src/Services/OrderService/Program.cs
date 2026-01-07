using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using OrderService;
using OrderService.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var endpoint = cfg["Cosmos:Endpoint"]!;
    var key = cfg["Cosmos:Key"]!;
    return new CosmosClient(endpoint, key, new CosmosClientOptions
    {
        ApplicationName = "OrderService"
    });
});

builder.Services.AddSingleton<IOrderRepository, CosmosOrderRepository>();
builder.Services.AddSingleton<IOrderValidator, OrderValidator>();

builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var cs = cfg["ServiceBus:ConnectionString"]!;
    return new ServiceBusClient(cs);
});

builder.Services.AddSingleton<IManagerQueuePublisher, ManagerQueuePublisher>();

var app = builder.Build();

// Ensure Cosmos DB/container exist
using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
    await repo.EnsureCreatedAsync(CancellationToken.None);
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/api/orders", async (
    CreateOrderRequest req,
    IOrderValidator validator,
    IOrderRepository repo,
    IManagerQueuePublisher publisher,
    CancellationToken ct) =>
{
    var (ok, error) = validator.Validate(req);
    if (!ok) return Results.BadRequest(new { error });

    var total = validator.CalculateTotal(req);

    // var orderId = Ulid.NewUlid().ToString(); // unique id
    var orderId = Guid.NewGuid().ToString();
    var now = DateTimeOffset.UtcNow;

    var order = new Order
    {
        // Id = $"{req.TenantId}:{orderId}",     // Cosmos id
        Id = orderId,
        // OrderId = req.OrderId,
        TenantId = req.TenantId,
        CustomerId = req.CustomerId,
        DeliveryAddress = req.DeliveryAddress,
        Items = req.Items,
        TotalAmount = total,
        OrderStatus = "Placed",
        CreatedAt = now,
        UpdatedAt = now
    };

    // 1) Persist order
    var created = await repo.CreateAsync(order, ct);

    // 2) Enqueue for manager review
    await publisher.PublishOrderSubmittedAsync(
        new OrderSubmittedMessage(created.Id, created.TenantId, created.CreatedAt),
        ct);

    // 3) Return to client
    return Results.Created($"/api/orders/{created.TenantId}/{created.Id}",
        new CreateOrderResponse(
            created.Id,
            created.TenantId,
            created.CustomerId,
            created.OrderStatus,
            created.TotalAmount,
            created.CreatedAt
        ));
});

app.MapGet("/api/orders/{TenantId}/{orderId}", async (
    string tenantId,
    string orderId,
    IOrderRepository repo,
    CancellationToken ct) =>
{
    var order = await repo.GetAsync(tenantId, orderId, ct);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

app.MapGet("/api/orders/by-customer/{TenantId}/{customerId}", async (
    string tenantId,
    string customerId,
    int? take,
    IOrderRepository repo,
    CancellationToken ct) =>
{
    var pageSize = Math.Clamp(take ?? 25, 1, 200);
    var orders = await repo.GetByCustomerAsync(tenantId, customerId, pageSize, ct);
    return Results.Ok(orders);
});

app.Run();
