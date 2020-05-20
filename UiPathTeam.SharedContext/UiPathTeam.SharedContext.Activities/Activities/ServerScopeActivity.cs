using System;
using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using UiPathTeam.SharedContext.Context;
using UiPathTeam.SharedContext.Activities.Properties;
using UiPath.Shared.Activities.Localization;

namespace UiPathTeam.SharedContext.Activities
{
    [LocalizedDisplayName(nameof(Resources.ServerScopeActivity_DisplayName))]
    [LocalizedDescription(nameof(Resources.ServerScopeActivity_Description))]
    public class ServerScopeActivity : NativeActivity
    {
        private const string _mutexName = "Local\\ServerScopeActivity";

        [Browsable(false)]
        public ActivityAction<ContextServer> Body { get; set; }

        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.ServerScopeActivity_ContextName_DisplayName))]
        [LocalizedDescription(nameof(Resources.ServerScopeActivity_ContextName_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> ContextName { get; set; }

        private ContextServer aContext;
        private string _context;

        private Mutex _dotNetMutex;

        public ServerScopeActivity()
        {
            Body = new ActivityAction<ContextServer>
            {
                Argument = new DelegateInArgument<ContextServer>("ContextServer"),
                Handler = new Sequence { DisplayName = Resources.InteractWithContext }
            };
            this._dotNetMutex = new Mutex(false, this.GetMutexName());
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
        }

        protected override void Execute(NativeActivityContext context)
        {
            try
            {
                this._context = ContextName.Get(context);

                if (!this._dotNetMutex.WaitOne())
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
                CleanupContext();
                throw;
            }
        }

        private void CleanupContext(bool releaseMutex = true)
        {
            if (aContext != null)
            {
                aContext.MyDispose();
            }

            if(releaseMutex && this._dotNetMutex != null)
            {
                this._dotNetMutex.ReleaseMutex();
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

        private string GetMutexName()
        {
            return _mutexName + this._context;
        }
    }
}
