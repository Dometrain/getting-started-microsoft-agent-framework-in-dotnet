using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using VoltCartSupport.Models;

namespace VoltCartSupport.Executors;

[YieldsOutput(typeof(ChatMessage))]
internal sealed class SpecialistExecutor(string id, AIAgent agent)
    : Executor<TriageDecision, ChatMessage>(id: id)
{
    public override async ValueTask<ChatMessage> HandleAsync(
        TriageDecision decision,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var response = await agent.RunAsync(
            decision.Summary,
            cancellationToken: cancellationToken);

        var assistantMessage =
            response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant
                                                 && !string.IsNullOrWhiteSpace(m.Text))
            ?? new ChatMessage(ChatRole.Assistant, response.Text ?? string.Empty);

        // Chat panel (workflow-level output)
        await context.YieldOutputAsync(assistantMessage, cancellationToken);

        // Node panel (executor_completed payload) + sent to downstream edges (none here)
        return assistantMessage;
    }
}