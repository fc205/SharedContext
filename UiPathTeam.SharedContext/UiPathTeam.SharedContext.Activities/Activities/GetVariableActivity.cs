using System;
using System.Activities;
using System.ComponentModel;
using UiPathTeam.SharedContext.Context;
using UiPathTeam.SharedContext.Activities.Properties;
using UiPath.Shared.Activities.Localization;

namespace UiPathTeam.SharedContext.Activities
{
    [LocalizedDisplayName(nameof(Resources.GetVariableActivity_DisplayName))]
    [LocalizedDescription(nameof(Resources.GetVariableActivity_Description))]
    public class GetVariableActivity : CodeActivity
    {
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.GetVariableActivity_VariableName_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetVariableActivity_VariableName_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> VariableName { get; set; }

        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.GetVariableActivity_RaiseException_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetVariableActivity_RaiseException_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public bool RaiseException { get; set; }

        [LocalizedDisplayName(nameof(Resources.GetVariableActivity_ContextClient_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetVariableActivity_ContextClient_Description))]
        [LocalizedCategory(nameof(Resources.Options_Category))]
        public InArgument<ContextClient> ContextClient { get; set; }

        [LocalizedDisplayName(nameof(Resources.GetVariableActivity_VariableValue_DisplayName))]
        [LocalizedDescription(nameof(Resources.GetVariableActivity_VariableValue_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> VariableValue { get; set; }

        public GetVariableActivity()
        {
            this.RaiseException = true;
        }

        protected override void Execute(CodeActivityContext context)
        {
            ContextClient contextClient = null;
            string aValue;

            if (ContextClient.Expression == null)
            {
                var ContextClientArgument = context.DataContext.GetProperties()["ContextClient"];
                contextClient = ContextClientArgument.GetValue(context.DataContext) as ContextClient;
                if (contextClient == null)
                {
                    throw (new ArgumentException("ContextClient was not provided and cannot be retrieved from the container."));
                }
            }
            else
            {
                contextClient = ContextClient.Get(context);
            }

            aValue = contextClient.GetVariable(VariableName.Get(context), this.RaiseException);

            VariableValue.Set(context, aValue);
        }
    }
}
