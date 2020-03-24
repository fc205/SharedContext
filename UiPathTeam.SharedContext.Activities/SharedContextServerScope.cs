using System;
using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;
using System.Collections.Generic;

namespace UiPathTeam.SharedContext.Activities
{
    [Designer(typeof(SharedContextServerScopeDesigner))]
    [DisplayName("Shared Context Server Scope")]
    [Description("Creates a Named Pipe server for the shared context environment.")]
    public class SharedContextServerScope : NativeActivity
    {
        [Browsable(false)]
        public ActivityAction<ContextServer> Body { get; set; }

        [Category("Context")]
        [RequiredArgument]
        [Description("Name of the context that will store the information. It will be created if it doesn't exist and will be locked for exclussive use for this scope.")]
        public InArgument<string> Name { get; set; }

        private ContextServer aContext;

        public SharedContextServerScope()
        {
            Body = new ActivityAction<ContextServer>
            {
                Argument = new DelegateInArgument<ContextServer>("ContextServer"),
                Handler = new Sequence { DisplayName = "Interact with the Context" }
            };
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
        }

        protected override void Execute(NativeActivityContext context)
        {
            try
            {
                Dictionary<string, string>  aArguments = new Dictionary<string, string>();

                aArguments["Retries"] = "5";

                aContext = new ContextServer(contextType.NamedPipe, Name.Get(context), aArguments);
                aContext.CreateServer();

                if (Body != null)
                {
                    //scheduling the execution of the child activities
                    // and passing the value of the delegate argument
                    context.ScheduleAction(Body, aContext, OnCompleted, OnFaulted);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                CleanupContext();
                throw;
            }
        }

        private void CleanupContext()
        {
            if (aContext != null)
            {
                aContext.MyDispose();
            }
        }
        private void OnFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            faultContext.CancelChildren();
            CleanupContext();
        }

        private void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {
            CleanupContext();
        }
    }
}
