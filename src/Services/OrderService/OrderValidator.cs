using OrderService.Models;

namespace OrderService
{
    public interface IOrderValidator
    {
        (bool ok, string? error) Validate(CreateOrderRequest req);
        decimal CalculateTotal(CreateOrderRequest req);
    }

    public sealed class OrderValidator : IOrderValidator
    {
        public (bool ok, string? error) Validate(CreateOrderRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.TenantId)) return (false, "TenantId is required.");

            if (string.IsNullOrWhiteSpace(req.CustomerId)) return (false, "CustomerId is required.");

            if (req.DeliveryAddress is null) return (false, "DeliveryAddress is required.");

            if (req.Items is null || req.Items.Count == 0) return (false, "Cart is empty.");
            if (req.Items.Any(i => string.IsNullOrWhiteSpace(i.Sku))) return (false, "Item SKU is required.");
            if (req.Items.Any(i => i.Quantity <= 0)) return (false, "Item quantity must be >= 1.");
            if (req.Items.Any(i => i.UnitPrice < 0)) return (false, "Item price must be >= 0.");

            return (true, null);
        }

        public decimal CalculateTotal(CreateOrderRequest req)
            => req.Items.Sum(i => i.UnitPrice * i.Quantity);
    }

}
