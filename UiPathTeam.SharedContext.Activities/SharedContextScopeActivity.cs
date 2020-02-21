using System;
using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;
using System.Collections.Generic;

namespace UiPathTeam.SharedContext.Activities
{
    [Designer(typeof(SharedContextScopeActivityDesigner))]
    [DisplayName("Shared Context Scope")]
    [Description("Creates a connection to the shared context environment and locks it.")]
    public class SharedContextScopeActivity : NativeActivity
    {
        [Browsable(false)]
        public ActivityAction<ContextClient> Body { get; set; }

        [Category("Context")]
        [RequiredArgument]
        [Description("Name of the context that will store the information. It will be created if it doesn't exist and will be locked for exclussive use for this scope.")]
        public InArgument<string> Name { get; set; }

        [Category("Context")]
        [RequiredArgument]
        [Description("Type of context to use (Only File is available currently).")]
        public contextType Type { get; set; }

        [Category("Context")]
        [RequiredArgument]
        [DisplayName("Clear context")]
        [Description("Clear the context at the beginning of the scope")]
        public bool Clear { get; set; }

        [Category("Context")]
        [RequiredArgument]
        [DisplayName("Take lock")]
        [Description("Take a lock on the resource throughout the Scope activity")]
        public bool Lock { get; set; }

        [Category("File")]
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

        public SharedContextScopeActivity()
        {
            Body = new ActivityAction<ContextClient>
            {
                Argument = new DelegateInArgument<ContextClient>("ContextClient"),
                Handler = new Sequence { DisplayName = "Interact with the Context" }
            };
            this.Lock = true;
            this.Retries = 5;
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            if (Type != contextType.File && Retries == null)
            {
                metadata.AddValidationError("If Context Type is set to File, Retries is also required");
            }
        }

        protected override void Execute(NativeActivityContext context)
        {
            string aFolder = "";

            if (Retries.Get(context) <= 0) 
            {
                throw new Exception("Retries cannot be zero or negative.");
            }

            if(Folder != null)
            {
                aFolder = Folder.Get(context);
            }

            try
            {
                Dictionary<string, string>  aArguments = new Dictionary<string, string>();

                aArguments["Lock"] = this.Lock?"Y":"N";
                aArguments["Folder"] = aFolder;
                aArguments["Retries"] = Retries.Get(context).ToString();

                aContext = new ContextClient(Type, Name.Get(context), aArguments);

                if(this.Clear)
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
            catch (Exception)
            {
                CleanupContext();
                throw;
            }
        }

        private void CleanupContext()
        {
            if (aContext != null)
            {
                aContext.Dispose();
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
