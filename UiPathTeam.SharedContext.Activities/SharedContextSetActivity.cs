﻿using System;
using System.Activities;
using System.ComponentModel;

namespace UiPathTeam.SharedContext.Activities
{
    [DisplayName("Set Variable in Context")]
    public class SharedContextSetActivity : CodeActivity
    {
        [Category("Input")]
        [RequiredArgument]
        [Description("Variable name")]
        public InArgument<string> Name { get; set; }
        [Category("Input")]
        [RequiredArgument]
        [Description("Variable value (string).")]
        public InArgument<string> Value { get; set; }

        [Category("Input Optional")]
        [Description("Shared Context object to be used when not in a Shared Context Scope.")]
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
                    throw (new ArgumentException("FileClient was not provided and cannot be retrieved from the container."));
                }
            }
            else
            {
                contextClient = ContextClient.Get(context);
            }

            contextClient.Set(Name.Get(context), Value.Get(context));
        }
    }
}
