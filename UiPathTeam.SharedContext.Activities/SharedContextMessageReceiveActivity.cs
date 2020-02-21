using System;
using System.Activities;
using System.ComponentModel;
using UiPath.Robot.Activities.Api;

namespace UiPathTeam.SharedContext.Activities
{
    [DisplayName("Gets the oldest Message in the queue for this Process")]
    public class SharedContextMessageReceiveActivity : CodeActivity
    {
        [Category("Input Optional")]
        [Description("Shared Context object to be used when not in a Shared Context Scope.")]
        public InArgument<ContextClient> ContextClient { get; set; }

        [Category("Output")]
        [DisplayName("Message Content")]
        [Description("The contents of the Message received")]
        public OutArgument<string> MessageContent { get; set; }

        [Category("Output")]
        [DisplayName("Origin Process")]
        [Description("Origin Process of the Message.")]
        public OutArgument<string> OriginProcess { get; set; }

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
                this.MessageContent.Set(context, aNewContextMessage.Message);
                this.OriginProcess.Set(context, aNewContextMessage.From);
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
