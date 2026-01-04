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
    var cs = cfg["Cosmos:ConnectionString"]!;
    return new CosmosClient(cs, new CosmosClientOptions
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

    var orderId = Ulid.NewUlid().ToString(); // unique id
    var now = DateTimeOffset.UtcNow;

    var order = new Order
    {
        // Id = $"{req.TenantId}:{orderId}",     // Cosmos id
        OrderId = orderId,
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
        new OrderSubmittedMessage(created.OrderId, created.TenantId, created.CreatedAt),
        ct);

    // 3) Return to client
    return Results.Created($"/api/orders/{created.TenantId}/{created.OrderId}",
        new CreateOrderResponse(
            created.OrderId,
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

app.MapGet("/api/orders/by-customer/{customerId}", async (
    string customerId,
    int? take,
    IOrderRepository repo,
    CancellationToken ct) =>
{
    var pageSize = Math.Clamp(take ?? 25, 1, 200);
    var orders = await repo.GetByCustomerAsync(customerId, pageSize, ct);
    return Results.Ok(orders);
});

app.Run();
