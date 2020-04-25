using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.ComponentModel;

using UiPathTeam.SharedContext.Dependencies;
using UiPathTeam.SharedContext.Context;

namespace UiPathTeam.SharedContext.Activities
{
    [Designer(typeof(NamedPipeTriggerV2Designer))]
    [DisplayName("Named Pipe Trigger Standalone")]
    [Description("Named Pipe Trigger Standalone Activity (does not require MonitorEvents)")]
    public class NamedPipeTriggerV2 : TriggerScope<ContextContent>
    {
        [Category("Context")]
        [RequiredArgument]
        [Description("Name of the named pipe context that will store the information.")]
        public InArgument<string> ContextName { get; set; }

        [Category("Context")]
        [RequiredArgument]
        [DisplayName("Retries")]
        [Description("Number of retries of the opening of the file that the Scope activity will try before raising an exception.")]
        public InArgument<int> Retries { get; set; }

        private ContextClientNamedPipe _aContextClient;

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddArgument(new RuntimeArgument("ContextName", typeof(string), ArgumentDirection.In, true));
            base.CacheMetadata(metadata);
        }

        public NamedPipeTriggerV2()
        {
            this.Retries = 5;
        }

        protected override void StartMonitoring(NativeActivityContext context)
        {
            Dictionary<string, string> aArguments = new Dictionary<string, string>();

            aArguments["Retries"] = Retries.Get(context).ToString();

            _aContextClient = new ContextClientNamedPipe(ContextName.Get(context), aArguments, false);
            _aContextClient.CreateClient();
            _aContextClient.EventHandler += HandleEvent;
        }

        protected override void StopMonitoring(ActivityContext context)
        {
            try
            {
                if (_aContextClient != null)
                {
                    _aContextClient.MyDispose();
                    _aContextClient = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.Message);
            }
        }

    }
}
