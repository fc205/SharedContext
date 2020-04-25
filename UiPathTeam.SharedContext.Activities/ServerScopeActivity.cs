using System;
using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;
using System.Collections.Generic;
using UiPathTeam.SharedContext.Dependencies;
using UiPathTeam.SharedContext.Context;

namespace UiPathTeam.SharedContext.Activities
{
    [Designer(typeof(ServerScopeActivityDesigner))]
    [DisplayName("Shared Context Server Scope")]
    [Description("Creates a Named Pipe server for the shared context environment.")]
    public class ServerScopeActivity : NativeActivity
    {
        private const string _mutexName = "Local\\ServerScopeActivity";

        [Browsable(false)]
        public ActivityAction<ContextServer> Body { get; set; }

        [Category("Context")]
        [RequiredArgument]
        [Description("Name of the context that will store the information. It will be created if it doesn't exist and will be locked for exclussive use for this scope.")]
        public InArgument<string> Name { get; set; }

        private ContextServer aContext;
        private string _context;

        public ServerScopeActivity()
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
                this._context = Name.Get(context);

                if (!Win32Calls.TakeMutex(this.GetMutexName()))
                {
                    throw new Exception("There is already a Shared Context Server running!");
                }

                Dictionary<string, string>  aArguments = new Dictionary<string, string>();

                aArguments["Retries"] = "5";

                aContext = new ContextServer(contextType.NamedPipe, this._context, aArguments);
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
                CleanupContext(false);
                throw;
            }
        }

        private void CleanupContext(bool releaseMutex)
        {
            if (aContext != null)
            {
                aContext.MyDispose();
            }

            if(releaseMutex)
            {
                Win32Calls.ReleaseMutex(this.GetMutexName());
            }
        }
        private void OnFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            faultContext.CancelChildren();
            CleanupContext(true);
        }

        private void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {
            CleanupContext(true);
        }

        private string GetMutexName()
        {
            return _mutexName + this._context;
        }
    }
}
