using System;
using System.Activities;
using System.ComponentModel;

namespace UiPathTeam.SharedContext.Activities
{
    [DisplayName("Get Variable from Context")]
    public class SharedContextGetActivity : CodeActivity
    {
        [Category("Input")]
        [RequiredArgument]
        [Description("Variable name")]
        public InArgument<string> Name { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [DisplayName("Raise exception")]
        [Description("Whether the activity will raise an Exception in case the Variable doesn't exist in the context")]
        public bool RaiseException { get; set; }

        [Category("Input Optional")]
        [Description("Shared Context object to be used when not in a Shared Context Scope.")]
        public InArgument<ContextClient> ContextClient { get; set; }

        [Category("Output")]
        [Description("The Value of the variable.")]
        public OutArgument<string> Value { get; set; }

        public SharedContextGetActivity()
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
                    throw (new ArgumentException("FileClient was not provided and cannot be retrieved from the container."));
                }
            }
            else
            {
                contextClient = ContextClient.Get(context);
            }

            aValue = contextClient.Get(Name.Get(context), this.RaiseException);

            Value.Set(context, aValue);
        }
    }
}
