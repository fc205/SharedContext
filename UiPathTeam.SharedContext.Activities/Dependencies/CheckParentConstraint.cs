using System.Activities;
using System.Activities.Statements;
using System.Activities.Validation;
using System.Collections.Generic;
using System.Linq;

namespace UiPathTeam.SharedContext.Dependencies
{
    public class CheckParentConstraint
    {
        public static Constraint GetCheckParentConstraint<ActivityType>(string parentTypeName, string validationMessage = null) where ActivityType : Activity
        {
            validationMessage = validationMessage ?? ValidationMessage(parentTypeName);

            DelegateInArgument<ValidationContext> context = new DelegateInArgument<ValidationContext>();
            var element = new DelegateInArgument<ActivityType>();
            var parent = new DelegateInArgument<Activity>();
            var result = new Variable<bool>();
            Variable<IEnumerable<Activity>> parentList = new Variable<IEnumerable<Activity>>();

            return new Constraint<ActivityType>
            {
                Body = new ActivityAction<ActivityType, ValidationContext>
                {
                    Argument1 = element,
                    Argument2 = context,
                    Handler = new Sequence
                    {
                        Variables =
                        {
                            result,
                            parentList
                        },
                        Activities =
                        {
                            new Assign<IEnumerable<Activity>>
                            {
                                To = parentList,
                                Value = new GetParentChain
                                {
                                    ValidationContext = context
                                }
                            },
                            new ForEach<Activity>
                            {
                                Values = parentList,
                                Body = new ActivityAction<Activity>
                                {
                                    Argument = parent,
                                    Handler = new If
                                    {
                                        Condition = new InArgument<bool>(ctx => parent.Get(ctx).GetType().Name.Equals(parentTypeName)),
                                        Then = new Assign<bool>
                                        {
                                            Value = true,
                                            To = result
                                        }
                                    }
                                }
                            },
                            new AssertValidation
                            {
                                Assertion = new InArgument<bool>(result),
                                Message = new InArgument<string> (validationMessage),
                            }
                        }
                    }
                }
            };
        }

        public static Constraint GetCheckDirectParentConstraint<ActivityType>(string parentTypeName, string validationMessage = null, string parentTypeNameResource = null) where ActivityType : Activity
        {
            validationMessage = validationMessage ?? ValidationMessage(parentTypeNameResource);

            DelegateInArgument<ValidationContext> context = new DelegateInArgument<ValidationContext>();
            Variable<bool> result = new Variable<bool>();
            var element = new DelegateInArgument<ActivityType>();
            Variable<IEnumerable<Activity>> parentList = new Variable<IEnumerable<Activity>>();

            return new Constraint<ActivityType>
            {
                Body = new ActivityAction<ActivityType, ValidationContext>
                {
                    Argument1 = element,
                    Argument2 = context,
                    Handler = new Sequence
                    {
                        Variables =
                        {
                            result,
                            parentList
                        },
                        Activities =
                        {
                            new Assign<IEnumerable<Activity>>
                            {
                                To = parentList,
                                Value = new GetParentChain
                                {
                                    ValidationContext = context
                                }
                            },
                            new If
                            {
                                Condition = new InArgument<bool>(ctx => parentList.Get(ctx).Count() > 0 && parentList.Get(ctx).ElementAt(0).GetType().Name.Equals(parentTypeName)),
                                Then = new Assign<bool>
                                {
                                    Value = true,
                                    To = result
                                }
                            },
                            new AssertValidation
                            {
                                Assertion = new InArgument<bool>(result),
                                Message = new InArgument<string> (validationMessage),
                            }
                        }
                    }
                }
            };
        }

        private static string ValidationMessage(string parentTypeName)
        {
            return string.Format("Activity is valid only inside {0}", parentTypeName);
        }
    }
}
