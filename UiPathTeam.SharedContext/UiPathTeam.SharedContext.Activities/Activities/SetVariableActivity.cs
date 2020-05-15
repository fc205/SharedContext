using System;
using System.Activities;
using System.ComponentModel;
using UiPathTeam.SharedContext.Context;
using UiPathTeam.SharedContext.Activities.Properties;
using UiPath.Shared.Activities.Localization;

namespace UiPathTeam.SharedContext.Activities
{
    [LocalizedDisplayName(nameof(Resources.SetVariableActivity_DisplayName))]
    [LocalizedDescription(nameof(Resources.SetVariableActivity_Description))]
    public class SetVariableActivity : CodeActivity
    {
        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.SetVariableActivity_VariableName_DisplayName))]
        [LocalizedDescription(nameof(Resources.SetVariableActivity_VariableName_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> VariableName { get; set; }

        [RequiredArgument]
        [LocalizedDisplayName(nameof(Resources.SetVariableActivity_VariableValue_DisplayName))]
        [LocalizedDescription(nameof(Resources.SetVariableActivity_VariableValue_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> VariableValue { get; set; }

        [LocalizedDisplayName(nameof(Resources.SetVariableActivity_ContextClient_DisplayName))]
        [LocalizedDescription(nameof(Resources.SetVariableActivity_ContextClient_Description))]
        [LocalizedCategory(nameof(Resources.Options_Category))]
        public InArgument<ContextClient> ContextClient { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            ContextClient contextClient = null;

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

            contextClient.SetVariable(VariableName.Get(context), VariableValue.Get(context));
        }
    }
}
