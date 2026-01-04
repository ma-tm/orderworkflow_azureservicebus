using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using OrderService.Models;

namespace OrderService
{    
    public interface IOrderRepository
    {
        Task<Order> CreateAsync(Order order, CancellationToken ct);
        Task<Order?> GetAsync(string tenantId, string orderId, CancellationToken ct);
        Task<IReadOnlyList<Order>> GetByCustomerAsync(string customerId, int take, CancellationToken ct);
        Task EnsureCreatedAsync(CancellationToken ct);
    }

    public sealed class CosmosOrderRepository : IOrderRepository
    {
        private readonly CosmosClient _client;
        private readonly string _dbId;
        private readonly string _containerId;
        private Database? _db;
        private Container? _container;

        public CosmosOrderRepository(CosmosClient client, IConfiguration cfg)
        {
            _client = client;
            _dbId = cfg["Cosmos:DatabaseId"]!;
            _containerId = cfg["Cosmos:OrdersContainerId"]!;
        }

        public async Task EnsureCreatedAsync(CancellationToken ct)
        {
            _db = await _client.CreateDatabaseIfNotExistsAsync(_dbId, cancellationToken: ct);
            _container = await _db.CreateContainerIfNotExistsAsync(
                id: _containerId,
                partitionKeyPath: "/tenantId",
                throughput: 400,
                cancellationToken: ct
            );
        }

        private Container Container => _container ?? throw new InvalidOperationException("Cosmos container not initialized.");

        public async Task<Order> CreateAsync(Order order, CancellationToken ct)
        {
            // Partition key is tenantId
            var resp = await Container.CreateItemAsync(order, new PartitionKey(order.TenantId), cancellationToken: ct);
            return resp.Resource;
        }

        public async Task<Order?> GetAsync(string tenantId, string orderId, CancellationToken ct)
        {
            // Use a query by orderId within tenantId partition.
            var q = Container.GetItemLinqQueryable<Order>(allowSynchronousQueryExecution: false)
                .Where(o => o.TenantId == tenantId && o.OrderId == orderId)
                .Take(1)
                .ToFeedIterator();

            while (q.HasMoreResults)
            {
                var page = await q.ReadNextAsync(ct);
                return page.Resource.FirstOrDefault();
            }

            return null;
        }

        public async Task<IReadOnlyList<Order>> GetByCustomerAsync(string customerId, int take, CancellationToken ct)
        {
            var results = new List<Order>();

            var q = Container.GetItemLinqQueryable<Order>(allowSynchronousQueryExecution: false)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(take)
                .ToFeedIterator();

            while (q.HasMoreResults)
            {
                var page = await q.ReadNextAsync(ct);
                results.AddRange(page);
            }

            return results;
        }
    }
}
