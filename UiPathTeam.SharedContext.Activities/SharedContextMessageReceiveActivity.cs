using System;
using System.Activities;
using System.ComponentModel;
using UiPath.Robot.Activities.Api;

namespace UiPathTeam.SharedContext.Activities
{
    [DisplayName("Receive Latest Message")]
    public class SharedContextMessageReceiveActivity : CodeActivity
    {
        [Category("Input Optional")]
        [Description("Shared Context object to be used when not in a Shared Context Scope.")]
        public InArgument<ContextClient> ContextClient { get; set; }

        [Category("Output")]
        [DisplayName("Action")]
        [Description("The action to be performed")]
        public OutArgument<string> Action { get; set; }

        [Category("Output")]
        [DisplayName("Arguments")]
        [Description("The arguments of the Action (in Json format)")]
        public OutArgument<string> ArgumentsJson { get; set; }

        [Category("Output")]
        [DisplayName("From")]
        [Description("Origin Process of the Message.")]
        public OutArgument<string> From { get; set; }

        [Category("Output")]
        [DisplayName("Time sent")]
        [Description("Time sent.")]
        public OutArgument<DateTime> TimeSent { get; set; }

        [Category("Output")]
        [DisplayName("Message Queue empty")]
        [Description("Was this Message Queue empty?")]
        public OutArgument<bool> QueueEmpty { get; set; }

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

                this.QueueEmpty.Set(context, false);
            }
            else
            {
                this.QueueEmpty.Set(context, true);
            }
        }
    }
}
