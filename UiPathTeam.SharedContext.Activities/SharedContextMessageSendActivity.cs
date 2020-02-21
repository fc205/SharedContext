using System;
using System.Activities;
using System.ComponentModel;
using UiPath.Robot.Activities.Api;

namespace UiPathTeam.SharedContext.Activities
{
    [DisplayName("Add New Message for a Different Process")]
    public class SharedContextMessageSendActivity : CodeActivity
    {
        [Category("Input")]
        [RequiredArgument]
        [DisplayName("Destination Process")]
        [Description("Destination Process for the Message. The message will be placed in a queue for the Process to dequeue.")]
        public InArgument<string> DestinationProcess { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [DisplayName("MessageContent")]
        [Description("The contents of the Message to be passed to Destination Process")]
        public InArgument<string> MessageContent { get; set; }

        [Category("Input Optional")]
        [Description("Shared Context object to be used when not in a Shared Context Scope.")]
        public InArgument<ContextClient> ContextClient { get; set; }

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
            }
            else
            {
                currentProcess = "DummyProcess";
            }

            ContextMessage aNewContextMessage;
            aNewContextMessage.From = currentProcess;
            aNewContextMessage.Message = this.MessageContent.Get(context);
            aNewContextMessage.To = this.DestinationProcess.Get(context);
            aNewContextMessage.DateSent = DateTime.Now;

            contextClient.AddNewMessage(currentProcess, aNewContextMessage);
        }
    }
}
