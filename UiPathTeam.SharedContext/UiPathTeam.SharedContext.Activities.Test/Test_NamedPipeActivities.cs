using System;
using System.Linq;
using System.Activities;
using System.Activities.Statements;
using System.Activities.XamlIntegration;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Activities.UnitTesting;
using Microsoft.VisualBasic.Activities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiPathTeam.SharedContext.Context;

namespace UiPathTeam.SharedContext.Activities.Test
{
    [TestClass]
    public class Test_NamedPipeActivities
    {
        public const string Test_ContextName = "aTestContext";
        public const contextType Test_ContextType = contextType.NamedPipe;
        public const int Test_Retries = 5;

        public const string Test_SetVariableName = "aVariable123";
        public const string Test_SetVariableValue = "aValue456";
        public const string Test_SetVariableValue2 = "aValue999";

        protected ContextClient SetUpContextClient()
        {
            var aDictionary = new Dictionary<string, string>();
            aDictionary["Retries"] = Test_Retries.ToString();

            ContextClient aContext;
            aContext = new ContextClient(Test_ContextType, Test_ContextName, aDictionary, true);
            aContext.CreateClient();
            return aContext;
        }

        protected ContextServer SetUpContextServer()
        {
            var aDictionary = new Dictionary<string, string>();
            aDictionary["Retries"] = Test_Retries.ToString();

            ContextServer aContext;
            aContext = new ContextServer(Test_ContextType, Test_ContextName, aDictionary, true);
            aContext.CreateServer();
            return aContext;
        }

        [TestMethod]
        public void SCNamedPipeScopeWithSharedContextSetGetInside()
        {
            var sharedContextServerScope = new ServerScopeActivity
            {
                ContextName = Test_ContextName
            };

            var sharedContextScopeActivity = new ClientScopeActivity
            {
                ContextName = Test_ContextName,
                ContextType = Test_ContextType,
                ClearContext = true,
                Retries = Test_Retries
            };

            var setContextActivity = new SetVariableActivity
            {
                VariableName = Test_SetVariableName,
                VariableValue = Test_SetVariableValue
            };

            var getContextActivity = new GetVariableActivity
            {
                VariableName = Test_SetVariableName
            };

            sharedContextServerScope.Body.Handler = new Sequence()
            {
                Activities =
                {
                   sharedContextScopeActivity
                }
            };

            sharedContextScopeActivity.Body.Handler = new Sequence()
            {
                Activities =
                {
                   setContextActivity,
                   getContextActivity
                }
            };

            var output = WorkflowInvoker.Invoke(sharedContextServerScope);
        }

        [TestMethod]
        public void SCNamedPipeScopeNakedSetGet()
        {
            var aContextServer = this.SetUpContextServer();
            var aContextClient = this.SetUpContextClient();

            var setContextActivity = new SetVariableActivity
            {
                VariableName = Test_SetVariableName,
                VariableValue = Test_SetVariableValue,
                ContextClient = new InArgument<ContextClient>((ctx) => aContextClient)
            };

            var getContextActivity = new GetVariableActivity
            {
                VariableName = Test_SetVariableName,
                ContextClient = new InArgument<ContextClient>((ctx) => aContextClient)
            };

            WorkflowInvoker.Invoke(setContextActivity);
            var output = WorkflowInvoker.Invoke(getContextActivity);

            Assert.IsTrue(output["VariableValue"].ToString() == Test_SetVariableValue);

            aContextClient.MyDispose();
            aContextServer.MyDispose();
        }

        public void NewThread()
        {
            Console.WriteLine("In thread > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
            var aContextServer = this.SetUpContextServer();

            var sharedContextScopeActivity = new ClientScopeActivity
            {
                ContextName = Test_ContextName,
                ContextType = Test_ContextType,
                ClearContext = true,
                Retries = Test_Retries
            };

            var setContextActivity = new SetVariableActivity
            {
                VariableName = Test_SetVariableName,
                VariableValue = Test_SetVariableValue
            };

            sharedContextScopeActivity.Body.Handler = new Sequence()
            {
                Activities =
                {
                   setContextActivity
                }
            };

            Console.WriteLine("In thread : about to execute > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
            WorkflowInvoker.Invoke(sharedContextScopeActivity);

            aContextServer.MyDispose();
        }

        [TestMethod]
        public void SCNamedPipeTryToOpenLockedContext()
        {
            Thread thread1 = new Thread(NewThread);
            thread1.Start();
            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine("In main. i=" + i.ToString() + " > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));

                if (i == 0 || i == 3)
                {
                    try
                    {
                        var sharedContextScopeActivity = new ClientScopeActivity
                        {
                            ContextName = Test_ContextName,
                            ContextType = Test_ContextType,
                            Retries = Test_Retries
                        };

                        var setContextActivity = new SetVariableActivity
                        {
                            VariableName = Test_SetVariableName,
                            VariableValue = Test_SetVariableValue
                        };

                        sharedContextScopeActivity.Body.Handler = new Sequence()
                        {
                            Activities =
                                {
                                   setContextActivity
                                }
                        };

                        var output = WorkflowInvoker.Invoke(sharedContextScopeActivity);

                        Console.WriteLine("File locked! > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
                        Thread.Sleep(200);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Could not lock File > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
                        Thread.Sleep(50);
                    }
                }
                Thread.Sleep(200);
            }
        }

        [TestMethod]
        public void SCNamedPipeTryToOpenTwoContextScopes()
        {
            var aContextServer = this.SetUpContextServer();

            var sharedContextScopeActivityInner = new ClientScopeActivity
            {
                ContextName = Test_ContextName,
                ContextType = Test_ContextType,
                ClearContext = true,
                Retries = Test_Retries
            };

            var sharedContextScopeActivityOuter = new ClientScopeActivity
            {
                ContextName = Test_ContextName,
                ContextType = Test_ContextType,
                ClearContext = true,
                Retries = Test_Retries
            };

            sharedContextScopeActivityOuter.Body.Handler = new Sequence()
            {
                Activities =
                    {
                        sharedContextScopeActivityInner
                    }
            };

            try
            {
                var output = WorkflowInvoker.Invoke(sharedContextScopeActivityOuter);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message == "[SharedContext Client] Could not open resource within the retries.");
            }
            aContextServer.MyDispose();
        }

        [TestMethod]
        public void SCNamedPipeScopeNakedSetGetTwice()
        {
            var aContextServer = this.SetUpContextServer();
            var aContextClient = this.SetUpContextClient();

            var setContextActivity1 = new SetVariableActivity
            {
                VariableName = Test_SetVariableName,
                VariableValue = Test_SetVariableValue,
                ContextClient = new InArgument<ContextClient>((ctx) => aContextClient)
            };

            var getContextActivity1 = new GetVariableActivity
            {
                VariableName = Test_SetVariableName,
                ContextClient = new InArgument<ContextClient>((ctx) => aContextClient)
            };

            var setContextActivity2 = new SetVariableActivity
            {
                VariableName = Test_SetVariableName,
                VariableValue = Test_SetVariableValue2,
                ContextClient = new InArgument<ContextClient>((ctx) => aContextClient)
            };

            var getContextActivity2 = new GetVariableActivity
            {
                VariableName = Test_SetVariableName,
                ContextClient = new InArgument<ContextClient>((ctx) => aContextClient)
            };

            WorkflowInvoker.Invoke(setContextActivity1);
            var output1 = WorkflowInvoker.Invoke(getContextActivity1);
            WorkflowInvoker.Invoke(setContextActivity2);
            var output2 = WorkflowInvoker.Invoke(getContextActivity2);

            Assert.IsTrue(output1["VariableValue"].ToString() == Test_SetVariableValue);
            Assert.IsTrue(output2["VariableValue"].ToString() == Test_SetVariableValue2);

            aContextClient.MyDispose();
            aContextServer.MyDispose();
        }

        [TestMethod]
        public void SCNamedPipeTrigger()
        {
            var aContextServer = this.SetUpContextServer();
            var aContextClient = this.SetUpContextClient();

            const int runs = 1;

            Variable<int> runCount = new Variable<int>
            {
                Name = nameof(runCount),
                Default = runs,
            };
            var sequence = new Sequence()
            {
                Variables = { runCount },
                Activities =
                {
                    new NamedPipeTriggerV2()
                    {
                        ContextName = Test_ContextName,
                        Debug = true,
                        Body = new ActivityAction<ContextContent>
                        {
                            Argument = new DelegateInArgument<ContextContent>(typeof(ContextContent).Name),
                            Handler = new Sequence {
                                Activities =
                                {
                                    new Assign<int>() {
                                        To = runCount,
                                        Value = new InArgument<int>((ctx) => runCount.Get(ctx) - 1),
                                    },
                                    new WriteLine()
                                    {
                                        Text = new InArgument<string>((ctx) => $"Should be logged.")
                                    }
                                }
                            }
                        },
                        ContinueMonitoring = new VisualBasicValue<bool>($"{nameof(runCount)} > 0"),
                    }
                }
            };

            var host = new WorkflowInvokerTest(sequence);
            var task = Task.Run(() => { host.TestActivity(TimeSpan.FromSeconds(10)); });

            // Trigger initialization takes time.
            Thread.Sleep(2000);

            var setContextActivity = new SetVariableActivity
            {
                VariableName = Test_SetVariableName,
                VariableValue = Test_SetVariableValue,
                ContextClient = new InArgument<ContextClient>((ctx) => aContextClient)
            };

            WorkflowInvoker.Invoke(setContextActivity);

            aContextClient.MyDispose();

            Thread.Sleep(100);

            task.Wait(1000);
            Assert.IsTrue(TaskStatus.RanToCompletion == task.Status);
            Assert.IsTrue(runs == host.TextLines.Where(l => l == "Should be logged.").Count());

            aContextServer.MyDispose();
        }

        /*
        [TestMethod]
        public void XamlActivitySetGet()
        {
            ActivityXamlServicesSettings settings = new ActivityXamlServicesSettings
            {
                CompileExpressions = true
            };

            Activity workflow = ActivityXamlServices.Load("TestSetGet.xaml", settings);

            var aDict = new Dictionary<string, Object>();
            aDict["iContext"] = Test_ContextName;
            aDict["iVariable"] = Test_SetVariableName;
            aDict["iValue"] = Test_SetVariableValue;

            var output = WorkflowInvoker.Invoke(workflow, aDict);

            Assert.IsTrue(output["oValue"].ToString() == Test_SetVariableValue);
        }
        */
    }
}
