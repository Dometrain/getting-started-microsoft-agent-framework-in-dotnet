using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using VoltCartSupport.Tools;

namespace VoltCartSupport.Workflows;

public class RefundWorkflowRunner
{
    private readonly IChatClient _chatClient;

    public RefundWorkflowRunner(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<string> RunAsync(
        string customerMessage,
        bool customerConfirmed,
        CancellationToken cancellationToken = default)
    {
        ChatClientAgent invoiceAgent = new(
            _chatClient,
            """
            You are the refund intake step.
            Extract the order ID, call the invoice and payment-history tools, and explain what charge is being discussed.
            Keep the answer factual and short.
            """,
            "RefundInvoice",
            "Looks up the billing context for a refund request",
            [
                AIFunctionFactory.Create(BillingTools.GetInvoice),
                AIFunctionFactory.Create(BillingTools.GetPaymentHistory)
            ]);

        ChatClientAgent executionAgent = new(
            _chatClient,
            """
            You handle the irreversible refund step.
            Only call ProcessRefund when approval is granted.
            If approval is denied, explain that the refund is waiting for customer confirmation.
            """,
            "RefundExecution",
            "Executes the refund after approval",
            [
                new ApprovalRequiredAIFunction(
                    AIFunctionFactory.Create(BillingTools.ProcessRefund))
            ]);

        ChatClientAgent summaryAgent = new(
            _chatClient,
            """
            You are the billing wrap-up step.
            Summarize the outcome for the customer in one short paragraph.
            Mention the waiting-for-confirmation state if the refund was not approved.
            """,
            "RefundSummary");

        var workflow = AgentWorkflowBuilder.BuildSequential(
            [invoiceAgent, executionAgent, summaryAgent]);

        var transcript = new StringBuilder();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, customerMessage)
        };

        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, messages);

        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                case RequestInfoEvent requestEvent
                    when requestEvent.Request.TryGetDataAs(
                        out ToolApprovalRequestContent? approvalRequest):
                    await run.SendResponseAsync(
                        requestEvent.Request.CreateResponse(
                            approvalRequest.CreateResponse(approved: customerConfirmed)));

                    if (!customerConfirmed)
                    {
                        return "I found the order and can start the refund. Please confirm that you want me to submit it.";
                    }

                    break;

                case AgentResponseUpdateEvent update:
                    transcript.Append(update.Update.Text);
                    break;
            }
        }

        return transcript.ToString().Trim();
    }
}
