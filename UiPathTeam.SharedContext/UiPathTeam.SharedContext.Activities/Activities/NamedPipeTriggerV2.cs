using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.ComponentModel;
using UiPathTeam.SharedContext.Activities.Properties;
using UiPath.Shared.Activities.Localization;
using UiPathTeam.SharedContext.Context;
using UiPathTeam.SharedContext.Activities.Dependencies;

namespace UiPathTeam.SharedContext.Activities
{
    [LocalizedDisplayName(nameof(Resources.NamedPipeTriggerV2_DisplayName))]
    [LocalizedDescription(nameof(Resources.NamedPipeTriggerV2_Description))]
    public class NamedPipeTriggerV2 : TriggerScope<ContextContent>
    {
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.NamedPipeTriggerV2_ContextName_DisplayName))]
        [LocalizedDescription(nameof(Resources.NamedPipeTriggerV2_ContextName_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> ContextName { get; set; }

        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.NamedPipeTriggerV2_Retries_DisplayName))]
        [LocalizedDescription(nameof(Resources.NamedPipeTriggerV2_Retries_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<int> Retries { get; set; }

        private ContextClientNamedPipe _aContextClient;

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddArgument(new RuntimeArgument("Name", typeof(string), ArgumentDirection.In, true));
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
