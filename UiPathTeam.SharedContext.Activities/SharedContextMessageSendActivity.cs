﻿using System;
using System.Activities;
using System.ComponentModel;
using UiPath.Robot.Activities.Api;

namespace UiPathTeam.SharedContext.Activities
{
    [DisplayName("Send Message")]
    public class SharedContextMessageSendActivity : CodeActivity
    {
        [Category("Input")]
        [RequiredArgument]
        [DisplayName("To")]
        [Description("Destination Process for the Message.")]
        public InArgument<string> To { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [DisplayName("Action")]
        [Description("The action to be performed")]
        public InArgument<string> Action { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [DisplayName("Arguments")]
        [Description("The arguments of the Action (in Json format)")]
        public InArgument<string> ArgumentsJson { get; set; }

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
            aNewContextMessage.Action = this.Action.Get(context);
            aNewContextMessage.ArgumentsJson = this.ArgumentsJson.Get(context);
            aNewContextMessage.To = this.To.Get(context);
            aNewContextMessage.DateSent = DateTime.Now;

            contextClient.AddNewMessage(currentProcess, aNewContextMessage);
        }
    }
}
