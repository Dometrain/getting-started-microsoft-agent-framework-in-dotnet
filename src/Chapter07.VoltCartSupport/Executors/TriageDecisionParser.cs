using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using VoltCartSupport.Models;

namespace VoltCartSupport.Executors;

public sealed class TriageDecisionExecutor()
    : Executor<List<ChatMessage>, TriageDecision>(id: "parse-triage")
{
    public override ValueTask<TriageDecision> HandleAsync(
        List<ChatMessage> messages,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        // Triage was configured to respond with JSON matching TriageDecision.
        var json = messages.LastOrDefault(m => m.Role == ChatRole.Assistant)?.Text
                   ?? throw new InvalidOperationException(
                       "Parser received no assistant response from the triage agent.");

        var decision = JsonSerializer.Deserialize<TriageDecision>(json)
                       ?? throw new InvalidOperationException(
                           $"Triage response was not valid JSON: {json}");

        return ValueTask.FromResult(decision);
    }
}