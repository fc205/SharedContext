using System;
using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;
using System.Collections.Generic;
using UiPathTeam.SharedContext.Context;

namespace UiPathTeam.SharedContext.Activities
{
    [Designer(typeof(ClientScopeActivityDesigner))]
    [DisplayName("Shared Context Scope")]
    [Description("Creates a connection to the shared context environment and locks it.")]
    public class ClientScopeActivity : NativeActivity
    {
        [Browsable(false)]
        public ActivityAction<ContextClient> Body { get; set; }

        [Category("Context")]
        [RequiredArgument]
        [Description("Name of the context that will store the information. It will be created if it doesn't exist and will be locked for exclussive use for this scope.")]
        public InArgument<string> Name { get; set; }

        [Category("Context")]
        [RequiredArgument]
        [Description("Type of context to use (File and Named Pipes are available currently).")]
        public contextType Type { get; set; }

        [Category("Context")]
        [RequiredArgument]
        [DisplayName("Clear context")]
        [Description("Clear the context at the beginning of the scope")]
        public bool Clear { get; set; }

        [Category("Context")]
        [RequiredArgument]
        [DisplayName("Retries")]
        [Description("Number of retries of the opening of the file that the Scope activity will try before raising an exception.")]
        public InArgument<int> Retries { get; set; }

        [Category("File")]
        [DisplayName("Folder")]
        [Description("Path to the folder where the file will be stored. Will default to System.IO.Path.GetTempPath")]
        public InArgument<string> Folder { get; set; }

        [Category("File")]
        [DisplayName("Output File Path")]
        [Description("Path to the file where the context will be stored")]
        public OutArgument<string> FilePath { get; set; }

        private ContextClient aContext;
        private string _context;

        public ClientScopeActivity()
        {
            Body = new ActivityAction<ContextClient>
            {
                Argument = new DelegateInArgument<ContextClient>("ContextClient"),
                Handler = new Sequence { DisplayName = "Interact with the Context" }
            };
            this.Retries = 5;
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
        }

        protected override void Execute(NativeActivityContext context)
        {
            this._context = Name.Get(context);
            string aFolder = "";

            if (Retries == null || Retries.Get(context) <= 0) 
            {
                throw new Exception("Retries cannot be zero or negative.");
            }

            if(Folder != null)
            {
                aFolder = Folder.Get(context);
            }

            Dictionary<string, string> aArguments = new Dictionary<string, string>();

            aArguments["Folder"] = aFolder;
            aArguments["Retries"] = Retries.Get(context).ToString();

            try
            {
                aContext = new ContextClient(Type, this._context, aArguments);

                aContext.CreateClient();
                if (Type == contextType.File && this.Clear)
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
                Console.WriteLine("[SharedContext Scope] There is an error!!");
                Console.WriteLine(exception.Message);
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
            FilePath.Set(context, aContext.GetResource());
            CleanupContext();
        }
    }
}
