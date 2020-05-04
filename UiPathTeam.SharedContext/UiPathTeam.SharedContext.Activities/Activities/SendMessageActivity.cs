using System;
using System.Activities;
using System.ComponentModel;
using UiPath.Robot.Activities.Api;
using UiPathTeam.SharedContext.Context;
using UiPathTeam.SharedContext.Activities.Properties;
using UiPath.Shared.Activities.Localization;

namespace UiPathTeam.SharedContext.Activities
{
    [LocalizedDisplayName(nameof(Resources.SendMessageActivity_DisplayName))]
    [LocalizedDescription(nameof(Resources.SendMessageActivity_Description))]
    public class SendMessageActivity : CodeActivity
    {
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.SendMessageActivity_To_DisplayName))]
        [LocalizedDescription(nameof(Resources.SendMessageActivity_To_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> To { get; set; }

        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.SendMessageActivity_Action_DisplayName))]
        [LocalizedDescription(nameof(Resources.SendMessageActivity_Action_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> Action { get; set; }

        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.SendMessageActivity_ArgumentsJson_DisplayName))]
        [LocalizedDescription(nameof(Resources.SendMessageActivity_ArgumentsJson_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> ArgumentsJson { get; set; }

        [LocalizedDisplayName(nameof(Resources.SendMessageActivity_ContextClient_DisplayName))]
        [LocalizedDescription(nameof(Resources.SendMessageActivity_ContextClient_Description))]
        [LocalizedCategory(nameof(Resources.Options_Category))]
        public InArgument<ContextClient> ContextClient { get; set; }

        [LocalizedDisplayName(nameof(Resources.SendMessageActivity_From_DisplayName))]
        [LocalizedDescription(nameof(Resources.SendMessageActivity_From_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> From { get; set; }

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

            string currentProcess;
            var executorRuntime = context.GetExtension<IExecutorRuntime>();
            if(executorRuntime != null)
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

            ContextMessage aNewContextMessage;
            aNewContextMessage.From = currentProcess;
            aNewContextMessage.Action = this.Action.Get(context);
            aNewContextMessage.ArgumentsJson = this.ArgumentsJson.Get(context);
            aNewContextMessage.To = this.To.Get(context);
            aNewContextMessage.DateSent = DateTime.Now;

            this.From.Set(context, currentProcess);

            contextClient.AddNewMessage(aNewContextMessage.To, aNewContextMessage);
        }
    }
}
