using System.ComponentModel;

namespace VoltCartSupport.Tools;

public static class BillingTools
{
    [Description("Retrieves the invoice for a specific order.")]
    public static string GetInvoice(
        [Description("The order ID to get the invoice for")] string orderId)
    {
        var invoices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ORD-1001"] = "Invoice INV-5001: Wireless Headphones - $79.99, tax $6.40, total $86.39. Paid via Visa ending 4242.",
            ["ORD-1002"] = "Invoice INV-5002: Smart Speaker $129.99 + USB-C Cable $12.99 - subtotal $142.98, tax $11.44, total $154.42. Paid via PayPal.",
            ["ORD-1003"] = "Invoice INV-5003: Laptop Stand - $49.99, tax $4.00, total $53.99. Paid via Visa ending 1234."
        };

        return invoices.TryGetValue(orderId, out var invoice)
            ? invoice
            : $"No invoice found for order {orderId}.";
    }

    [Description("Processes a refund for an order after approval has been granted.")]
    public static string ProcessRefund(
        [Description("The order ID to refund")] string orderId,
        [Description("The reason for the refund")] string reason)
    {
        return $"Refund initiated for order {orderId}. Reason: {reason}. The refund will appear on your statement within 5-10 business days.";
    }

    [Description("Gets the payment history for the current customer.")]
    public static string GetPaymentHistory()
    {
        return """
            Recent payments:
            - March 1: $86.39 (ORD-1001, Visa ending 4242)
            - March 3: $154.42 (ORD-1002, PayPal)
            - March 7: $53.99 (ORD-1003, Visa ending 1234)
            """;
    }
}
