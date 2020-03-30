using System;
using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;
using UiPath.Core.Activities;
using NamedPipeWrapper;
using UiPath.Framework.Activities;

namespace UiPathTeam.SharedContext.Activities
{
    [Designer(typeof(NamedPipeTriggerDesigner))]
    [DisplayName("Named Pipe Trigger")]
    [Description("Named Pipe Trigger Activity")]
    public class NamedPipeTrigger : NativeActivity
    {
        [Browsable(false)]
        public ActivityAction<ContextClient> Body { get; set; }

        [Category("Context")]
        [RequiredArgument]
        [Description("Name of the named pipe context that will store the information.")]
        public InArgument<string> Name { get; set; }

        [Category("Context")]
        [RequiredArgument]
        [DisplayName("Retries")]
        [Description("Number of retries of the opening of the file that the Scope activity will try before raising an exception.")]
        public InArgument<int> Retries { get; set; }

        BookmarkResumptionHelper BookmarkResumptionHelper;
        private Bookmark RuntimeBookmark;

        protected override bool CanInduceIdle
        {
            get
            {
                return true; // we create bookmarks
            }
        }

        private NamedPipeClient<ContextContent> _client;

        public NamedPipeTrigger()
        {
            this.Retries = 5;
            this.Constraints.Add(CheckParentConstraint.GetCheckDirectParentConstraint<NamedPipeTrigger>("MonitorEvents", null, "Monitor Events"));
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.RequireExtension<BookmarkResumptionHelper>();
            metadata.AddDefaultExtensionProvider<BookmarkResumptionHelper>(() => new BookmarkResumptionHelper());
            base.CacheMetadata(metadata);
        }

        protected override void Execute(NativeActivityContext context)
        {
            RuntimeBookmark = context.Properties.Find("MonitorBookmark") as Bookmark;
            if (RuntimeBookmark == null) return;
            BookmarkResumptionHelper = context.GetExtension<BookmarkResumptionHelper>();
            StartMonitor(context);
            context.CreateBookmark();
        }

        protected void StartMonitor(NativeActivityContext context)
        {
            string contextName = Environment.UserDomainName + "_" +
                                   Environment.UserName + "_" +
                                   Name.Get(context);

            this._client = new NamedPipeClient<ContextContent>(contextName);

            this._client.ServerMessage += _client_ServerMessage; ;
            this._client.Error += _client_Error;

            this._client.Start();

            Console.WriteLine("[SharedContext Trigger] Started client. > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
        }

        private void _client_ServerMessage(NamedPipeConnection<ContextContent, ContextContent> connection, ContextContent message)
        {
            Console.WriteLine("[SharedContext Trigger] Received a message! > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));

            if (RuntimeBookmark != null)
            {
                var args = new object();
                BookmarkResumptionHelper.BeginResumeBookmark(RuntimeBookmark, args);
            }
        }

        private void _client_Error(Exception exception)
        {
            Console.WriteLine("[SharedContext Trigger] There is an error!! > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
            Console.WriteLine(exception.Message);
        }

        protected void StopMonitor(ActivityContext context)
        {
            try
            {
                if (this._client != null)
                {
                    this._client.Stop();
                    this._client = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.Message);
            }
        }

        protected override void Cancel(NativeActivityContext context)
        {
            StopMonitor(context);
            base.Cancel(context);
        }

        protected override void Abort(NativeActivityAbortContext context)
        {
            StopMonitor(context);
            base.Abort(context);
        }
    }
}
