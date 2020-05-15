using System;
using System.Activities;
using System.ComponentModel;
using UiPath.Robot.Activities.Api;
using UiPathTeam.SharedContext.Context;
using UiPathTeam.SharedContext.Activities.Properties;
using UiPath.Shared.Activities.Localization;


namespace UiPathTeam.SharedContext.Activities
{
    [LocalizedDisplayName(nameof(Resources.ReceiveMessageActivity_DisplayName))]
    [LocalizedDescription(nameof(Resources.ReceiveMessageActivity_Description))]
    public class ReceiveMessageActivity : CodeActivity
    {
        [LocalizedDisplayName(nameof(Resources.ReceiveMessageActivity_ContextClient_DisplayName))]
        [LocalizedDescription(nameof(Resources.ReceiveMessageActivity_ContextClient_Description))]
        [LocalizedCategory(nameof(Resources.Options_Category))]
        public InArgument<ContextClient> ContextClient { get; set; }

        [LocalizedDisplayName(nameof(Resources.ReceiveMessageActivity_Action_DisplayName))]
        [LocalizedDescription(nameof(Resources.ReceiveMessageActivity_Action_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> Action { get; set; }

        [LocalizedDisplayName(nameof(Resources.ReceiveMessageActivity_ArgumentsJson_DisplayName))]
        [LocalizedDescription(nameof(Resources.ReceiveMessageActivity_ArgumentsJson_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> ArgumentsJson { get; set; }

        [LocalizedDisplayName(nameof(Resources.ReceiveMessageActivity_From_DisplayName))]
        [LocalizedDescription(nameof(Resources.ReceiveMessageActivity_From_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> From { get; set; }

        [LocalizedDisplayName(nameof(Resources.ReceiveMessageActivity_TimeSent_DisplayName))]
        [LocalizedDescription(nameof(Resources.ReceiveMessageActivity_TimeSent_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<DateTime> TimeSent { get; set; }

        [LocalizedDisplayName(nameof(Resources.ReceiveMessageActivity_MessageQueueEmpty_DisplayName))]
        [LocalizedDescription(nameof(Resources.ReceiveMessageActivity_MessageQueueEmpty_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<bool> MessageQueueEmpty { get; set; }

        [LocalizedDisplayName(nameof(Resources.ReceiveMessageActivity_To_DisplayName))]
        [LocalizedDescription(nameof(Resources.ReceiveMessageActivity_To_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> To { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            ContextClient contextClient = null;

            if (ContextClient.Expression == null)
            {
                var ContextClientArgument = context.DataContext.GetProperties()["ContextClient"];
                contextClient = ContextClientArgument.GetValue(context.DataContext) as ContextClient;
                if (contextClient == null)
                {
                    throw (new ArgumentException("ContextClient was not provided and cannot be retrieved from the container."));
                }
            }
            else
            {
                contextClient = ContextClient.Get(context);
            }

            var executorRuntime = context.GetExtension<IExecutorRuntime>();
            string currentProcess;

            if (executorRuntime != null)
            {
                var jobInfo = executorRuntime.RunningJobInformation;

                currentProcess = jobInfo.ProcessName.ToString();

                if (currentProcess.Contains("_"))
                {
                    currentProcess = currentProcess.Split('_')[0];
                }
            }
            else
            {
                currentProcess = "DummyProcess";
            }

            ContextMessage aNewContextMessage = new ContextMessage();

            if (contextClient.GetNextMessage(currentProcess, ref aNewContextMessage))
            {
                this.Action.Set(context, aNewContextMessage.Action);
                this.ArgumentsJson.Set(context, aNewContextMessage.ArgumentsJson);
                this.From.Set(context, aNewContextMessage.From);
                this.TimeSent.Set(context, aNewContextMessage.DateSent);

                this.To.Set(context, currentProcess);

                this.MessageQueueEmpty.Set(context, false);
            }
            else
            {
                this.MessageQueueEmpty.Set(context, true);
            }
        }
    }
}
