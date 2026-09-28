using System.ComponentModel;

namespace VoltCartSupport.Tools;

public static class OrderTools
{
    [Description("Looks up order details by order ID. Returns order info including items, total, and shipping address.")]
    public static string LookupOrder(
        [Description("The order ID, for example ORD-1234")] string orderId)
    {
        var orders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ORD-1001"] = "Order ORD-1001: Wireless Headphones ($79.99), shipped to 123 Main St. Status: Delivered.",
            ["ORD-1002"] = "Order ORD-1002: Smart Speaker ($129.99) + USB-C Cable ($12.99), shipped to 456 Oak Ave. Status: In Transit.",
            ["ORD-1003"] = "Order ORD-1003: Laptop Stand ($49.99), shipped to 789 Pine Rd. Status: Processing."
        };

        return orders.TryGetValue(orderId, out var order)
            ? order
            : $"No order found with ID {orderId}. Please check the order ID and try again.";
    }

    [Description("Gets the current shipping status of an order.")]
    public static string GetOrderStatus(
        [Description("The order ID to check status for")] string orderId)
    {
        var statuses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ORD-1001"] = "Delivered on March 5, 2026.",
            ["ORD-1002"] = "In transit - expected delivery March 11, 2026.",
            ["ORD-1003"] = "Processing - not yet shipped."
        };

        return statuses.TryGetValue(orderId, out var status)
            ? $"Order {orderId}: {status}"
            : $"No order found with ID {orderId}.";
    }

    [Description("Cancels an order. Only works for orders that have not shipped yet. IMPORTANT: Always confirm with the customer before calling this tool.")]
    public static string CancelOrder(
        [Description("The order ID to cancel")] string orderId,
        [Description("Must be 'true' - the customer must explicitly confirm the cancellation")] string customerConfirmed)
    {
        if (!string.Equals(customerConfirmed, "true", StringComparison.OrdinalIgnoreCase))
        {
            return "Cancellation not confirmed. Please ask the customer to confirm they want to cancel this order.";
        }

        var cancellable = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ORD-1003" };

        if (cancellable.Contains(orderId))
        {
            return $"Order {orderId} has been cancelled. A confirmation email will be sent shortly.";
        }

        return $"Order {orderId} cannot be cancelled - it has already shipped or been delivered.";
    }
}
