using System;
using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;
using System.Collections.Generic;
using UiPathTeam.SharedContext.Context;
using UiPathTeam.SharedContext.Activities.Properties;
using UiPath.Shared.Activities.Localization;

namespace UiPathTeam.SharedContext.Activities
{
    [LocalizedDisplayName(nameof(Resources.ClientScopeActivity_DisplayName))]
    [LocalizedDescription(nameof(Resources.ClientScopeActivity_Description))]
    public class ClientScopeActivity : NativeActivity
    {
        #region Properties

        [Browsable(false)]
        public ActivityAction<ContextClient> Body { get; set; }

        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.Debug_DisplayName))]
        [LocalizedDescription(nameof(Resources.Debug_Description))]
        [LocalizedCategory(nameof(Resources.Misc_Category))]
        public bool Debug { get; set; }

        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.ClientScopeActivity_ContextName_DisplayName))]
        [LocalizedDescription(nameof(Resources.ClientScopeActivity_ContextName_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> ContextName { get; set; }

        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.ClientScopeActivity_ContextType_DisplayName))]
        [LocalizedDescription(nameof(Resources.ClientScopeActivity_ContextType_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public contextType ContextType { get; set; }

        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.ClientScopeActivity_ClearContext_DisplayName))]
        [LocalizedDescription(nameof(Resources.ClientScopeActivity_ClearContext_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public bool ClearContext { get; set; }

        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.ClientScopeActivity_Retries_DisplayName))]
        [LocalizedDescription(nameof(Resources.ClientScopeActivity_Retries_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<int> Retries { get; set; }

        [LocalizedDisplayName(nameof(Resources.ClientScopeActivity_InputFolder_DisplayName))]
        [LocalizedDescription(nameof(Resources.ClientScopeActivity_InputFolder_Description))]
        [LocalizedCategory(nameof(Resources.File_Category))]
        public InArgument<string> InputFolder { get; set; }

        [LocalizedDisplayName(nameof(Resources.ClientScopeActivity_OutputContextFile_DisplayName))]
        [LocalizedDescription(nameof(Resources.ClientScopeActivity_OutputContextFile_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> OutputContextFile { get; set; }

        private ContextClient aContext;
        private string _context;

        #endregion


        public ClientScopeActivity()
        {
            Body = new ActivityAction<ContextClient>
            {
                Argument = new DelegateInArgument<ContextClient>("ContextClient"),
                Handler = new Sequence { DisplayName = Resources.InteractWithContext }
            };
            this.Retries = 5;
            this.ClearContext = false;
            this.Debug = false;
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
        }

        protected override void Execute(NativeActivityContext context)
        {
            this._context = ContextName.Get(context);
            string aFolder = "";

            if (Retries == null || Retries.Get(context) <= 0) 
            {
                throw new Exception("Retries cannot be zero or negative.");
            }

            if(InputFolder != null)
            {
                aFolder = InputFolder.Get(context);
            }

            Dictionary<string, string> aArguments = new Dictionary<string, string>();

            aArguments["Folder"] = aFolder;
            aArguments["Retries"] = Retries.Get(context).ToString();

            try
            {
                aContext = new ContextClient(ContextType, this._context, aArguments, this.Debug);

                aContext.CreateClient();
                if (ContextType == contextType.File && this.ClearContext)
                {
                    aContext.ClearAll();
                }

                if (Body != null)
                {
                    //scheduling the execution of the child activities
                    // and passing the value of the delegate argument
                    context.ScheduleAction(Body, aContext, OnCompleted, OnFaulted);
                }
            }
            catch (Exception exception)
            {
                if(this.Debug)
                {
                    Console.WriteLine("[SharedContext Scope] There is an error!!");
                    Console.WriteLine(exception.Message);
                }
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
            OutputContextFile.Set(context, aContext.GetResource());
            CleanupContext();
        }
    }
}
