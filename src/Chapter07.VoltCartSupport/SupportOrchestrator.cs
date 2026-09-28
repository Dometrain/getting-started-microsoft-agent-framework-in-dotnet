using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using VoltCartSupport.Models;
using VoltCartSupport.Security;
using VoltCartSupport.Workflows;

namespace VoltCartSupport;

public class SupportOrchestrator
{
    private readonly Dictionary<string, AIAgent> _agents;
    private readonly AIAgent _triageAgent;
    private readonly RefundWorkflowRunner _refundWorkflow;
    private readonly ActivitySource _activitySource;

    public SupportOrchestrator(
        AIAgent triageAgent,
        Dictionary<string, AIAgent> specialists,
        RefundWorkflowRunner refundWorkflow,
        ActivitySource activitySource)
    {
        _triageAgent = triageAgent;
        _agents = specialists;
        _refundWorkflow = refundWorkflow;
        _activitySource = activitySource;
    }

    public async Task<string> HandleMessageAsync(
        string customerMessage,
        bool customerConfirmed = false)
    {
        using var activity = _activitySource.StartActivity("HandleCustomerMessage");
        activity?.SetTag("customer.message_length", customerMessage.Length);

        var (isSuspicious, matchedPattern) = InjectionDetector.Check(customerMessage);
        if (isSuspicious)
        {
            activity?.SetTag("security.injection_detected", true);
            activity?.SetTag("security.matched_pattern", matchedPattern);
            return "I'm here to help with VoltCart support questions. Could you describe a product or order issue I can assist with?";
        }

        using var triageActivity = _activitySource.StartActivity("Triage");
        var triageResponse = await _triageAgent.RunAsync<TriageDecision>(customerMessage);
        var decision = triageResponse.Result;
        triageActivity?.SetTag("triage.completed", true);

        if (decision is null)
        {
            activity?.SetTag("triage.fallback", true);
            return "I wasn't able to determine how to help. Could you rephrase your question?";
        }

        activity?.SetTag("triage.target_agent", decision.TargetAgent);
        activity?.SetTag("triage.confidence", decision.Confidence);
        activity?.SetTag("triage.sentiment", decision.CustomerSentiment);

        string response;
        if (decision.TargetAgent.Equals("billing", StringComparison.OrdinalIgnoreCase) &&
            LooksLikeRefundRequest(customerMessage))
        {
            using var workflowActivity = _activitySource.StartActivity("RefundWorkflow");
            workflowActivity?.SetTag("workflow.type", "refund");
            workflowActivity?.SetTag("workflow.customer_confirmed", customerConfirmed);
            response = await _refundWorkflow.RunAsync(customerMessage, customerConfirmed);
        }
        else
        {
            if (!_agents.TryGetValue(decision.TargetAgent, out var specialist))
            {
                return $"I'd like to help with your {decision.Summary}, but I'm unable to connect you to the right team right now. Please try again.";
            }

            using var specialistActivity = _activitySource.StartActivity("SpecialistResponse");
            specialistActivity?.SetTag("specialist.agent", decision.TargetAgent);
            specialistActivity?.SetTag("specialist.issue_summary", decision.Summary);

            response = (await specialist.RunAsync(customerMessage)).Text;
        }

        var (isClean, filteredResponse) = ContentFilter.Filter(response);
        if (!isClean)
        {
            activity?.SetTag("security.output_filtered", true);
        }

        return filteredResponse;
    }

    public async IAsyncEnumerable<string> HandleStreamingAsync(
        string customerMessage,
        bool customerConfirmed = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("StreamingSupport");
        activity?.SetTag("customer.message_length", customerMessage.Length);

        var (isSuspicious, matchedPattern) = InjectionDetector.Check(customerMessage);
        if (isSuspicious)
        {
            activity?.SetTag("security.injection_detected", true);
            activity?.SetTag("security.matched_pattern", matchedPattern);
            yield return "I'm here to help with VoltCart support questions. Could you describe a product or order issue I can assist with?";
            yield break;
        }

        var triageResponse = await _triageAgent.RunAsync<TriageDecision>(customerMessage);
        var decision = triageResponse.Result;

        if (decision is null)
        {
            yield return "I wasn't able to route your request. Could you rephrase?";
            yield break;
        }

        activity?.SetTag("triage.target", decision.TargetAgent);
        activity?.SetTag("triage.confidence", decision.Confidence);

        if (decision.TargetAgent.Equals("billing", StringComparison.OrdinalIgnoreCase) &&
            LooksLikeRefundRequest(customerMessage))
        {
            var workflowResponse = await _refundWorkflow.RunAsync(
                customerMessage,
                customerConfirmed);

            var (_, filteredWorkflowResponse) = ContentFilter.Filter(workflowResponse);
            yield return filteredWorkflowResponse;
            yield break;
        }

        if (!_agents.TryGetValue(decision.TargetAgent, out var specialist))
        {
            yield return "I wasn't able to route your request. Could you rephrase?";
            yield break;
        }

        await foreach (var token in specialist.RunStreamingAsync(customerMessage)
                           .WithCancellation(cancellationToken))
        {
            yield return token.Text;
        }
    }

    private static bool LooksLikeRefundRequest(string message) =>
        message.Contains("refund", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("money back", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("broken", StringComparison.OrdinalIgnoreCase);
}
