using System.ComponentModel;
using System.Text.Json.Serialization;

namespace VoltCartSupport.Models;

public record TriageDecision(
    [property: Description("The target agent to route to: 'orders', 'billing', or 'technical'")]
    [property: JsonPropertyName("targetAgent")]
    string TargetAgent,

    [property: Description("Confidence score from 0.0 to 1.0 indicating how certain the routing is")]
    [property: JsonPropertyName("confidence")]
    double Confidence,

    [property: Description("A brief one-sentence summary of the customer's issue")]
    [property: JsonPropertyName("summary")]
    string Summary,

    [property: Description("The customer's sentiment: 'frustrated', 'neutral', or 'positive'")]
    [property: JsonPropertyName("customerSentiment")]
    string CustomerSentiment);
