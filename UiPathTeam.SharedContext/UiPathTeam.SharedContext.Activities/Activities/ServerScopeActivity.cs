using System;
using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using UiPathTeam.SharedContext.Context;
using UiPathTeam.SharedContext.Activities.Properties;
using UiPath.Shared.Activities.Localization;
using System.Text;

namespace UiPathTeam.SharedContext.Activities
{
    [LocalizedDisplayName(nameof(Resources.ServerScopeActivity_DisplayName))]
    [LocalizedDescription(nameof(Resources.ServerScopeActivity_Description))]
    public class ServerScopeActivity : NativeActivity
    {
        [Browsable(false)]
        public ActivityAction<ContextServer> Body { get; set; }

        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.Debug_DisplayName))]
        [LocalizedDescription(nameof(Resources.Debug_Description))]
        [LocalizedCategory(nameof(Resources.Misc_Category))]
        public bool Debug { get; set; }

        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.ServerScopeActivity_ContextName_DisplayName))]
        [LocalizedDescription(nameof(Resources.ServerScopeActivity_ContextName_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> ContextName { get; set; }

        private ContextServer aContext;
        private string _context;

        public ServerScopeActivity()
        {
            Body = new ActivityAction<ContextServer>
            {
                Argument = new DelegateInArgument<ContextServer>("ContextServer"),
                Handler = new Sequence { DisplayName = Resources.InteractWithContext }
            };
            this.Debug = false;
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

                Dictionary<string, string>  aArguments = new Dictionary<string, string>();

                aArguments["Retries"] = "5";

                aContext = new ContextServer(contextType.NamedPipe, this._context, aArguments, this.Debug);
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
