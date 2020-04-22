using System;
using System.IO;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Activities.UnitTesting;

namespace UiPathTeam.SharedContext.Activities.Test
{
    [TestClass]
    public class Test_SharedContextNamedPipeActivities
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
            aContext = new ContextClient(Test_ContextType, Test_ContextName, aDictionary);
            aContext.CreateClient();
            aContext.ClearAll();
            return aContext;
        }

        protected ContextServer SetUpContextServer()
        {
            var aDictionary = new Dictionary<string, string>();
            aDictionary["Retries"] = Test_Retries.ToString();

            ContextServer aContext;
            aContext = new ContextServer(Test_ContextType, Test_ContextName, aDictionary);
            aContext.CreateServer();
            return aContext;
        }

        [TestMethod]
        public void SCNamedPipeScopeWithSharedContextSetGetInside()
        {
            var sharedContextServerScope = new SharedContextServerScope
            {
                Name = Test_ContextName
            };

            var sharedContextScopeActivity = new SharedContextScopeActivity
            {
                Name = Test_ContextName,
                Type = Test_ContextType,
                Clear = true,
                Retries = Test_Retries
            };

            var setContextActivity = new SharedContextVariableSetActivity
            {
                Name = Test_SetVariableName,
                Value = Test_SetVariableValue
            };

            var getContextActivity = new SharedContextVariableGetActivity
            {
                Name = Test_SetVariableName
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

            var setContextActivity = new SharedContextVariableSetActivity
            {
                Name = Test_SetVariableName,
                Value = Test_SetVariableValue,
                ContextClient = new InArgument<ContextClient>((ctx) => aContextClient)
            };

            var getContextActivity = new SharedContextVariableGetActivity
            {
                Name = Test_SetVariableName,
                ContextClient = new InArgument<ContextClient>((ctx) => aContextClient)
            };

            var sharedContextSequence = new Sequence()
            {
                Activities =
                {
                   setContextActivity,
                   getContextActivity
                }
            };

            WorkflowInvoker.Invoke(setContextActivity);
            var output = WorkflowInvoker.Invoke(getContextActivity);

            Assert.IsTrue(output["Value"].ToString() == Test_SetVariableValue);
        }

        public void NewThread()
        {
            Console.WriteLine("In thread > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
            var aContextServer = this.SetUpContextServer();

            var sharedContextScopeActivity = new SharedContextScopeActivity
            {
                Name = Test_ContextName,
                Type = Test_ContextType,
                Clear = true,
                Retries = Test_Retries
            };

            var setContextActivity = new SharedContextVariableSetActivity
            {
                Name = Test_SetVariableName,
                Value = Test_SetVariableValue
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
                        var sharedContextScopeActivity = new SharedContextScopeActivity
                        {
                            Name = Test_ContextName,
                            Type = Test_ContextType,
                            Retries = Test_Retries
                        };

                        var setContextActivity = new SharedContextVariableSetActivity
                        {
                            Name = Test_SetVariableName,
                            Value = Test_SetVariableValue
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

            var sharedContextScopeActivityInner = new SharedContextScopeActivity
            {
                Name = Test_ContextName,
                Type = Test_ContextType,
                Clear = true,
                Retries = Test_Retries
            };

            var sharedContextScopeActivityOuter = new SharedContextScopeActivity
            {
                Name = Test_ContextName,
                Type = Test_ContextType,
                Clear = true,
                Retries = Test_Retries
            };

            sharedContextScopeActivityOuter.Body.Handler = new Sequence()
            {
                Activities =
                    {
                        sharedContextScopeActivityInner
                    }
            };

            var output = WorkflowInvoker.Invoke(sharedContextScopeActivityOuter);
        }


        [TestMethod]
        public void SCNamedPipeScopeNakedSetGetTwice()
        {
            var aContextServer = this.SetUpContextServer();
            var aContextClient = this.SetUpContextClient();

            var setContextActivity1 = new SharedContextVariableSetActivity
            {
                Name = Test_SetVariableName,
                Value = Test_SetVariableValue,
                ContextClient = new InArgument<ContextClient>((ctx) => aContextClient)
            };

            var getContextActivity1 = new SharedContextVariableGetActivity
            {
                Name = Test_SetVariableName,
                ContextClient = new InArgument<ContextClient>((ctx) => aContextClient)
            };

            var setContextActivity2 = new SharedContextVariableSetActivity
            {
                Name = Test_SetVariableName,
                Value = Test_SetVariableValue2,
                ContextClient = new InArgument<ContextClient>((ctx) => aContextClient)
            };

            var getContextActivity2 = new SharedContextVariableGetActivity
            {
                Name = Test_SetVariableName,
                ContextClient = new InArgument<ContextClient>((ctx) => aContextClient)
            };

            WorkflowInvoker.Invoke(setContextActivity1);
            var output1 = WorkflowInvoker.Invoke(getContextActivity1);
            WorkflowInvoker.Invoke(setContextActivity2);
            var output2 = WorkflowInvoker.Invoke(getContextActivity2);

            Assert.IsTrue(output1["Value"].ToString() == Test_SetVariableValue);
            Assert.IsTrue(output2["Value"].ToString() == Test_SetVariableValue2);
        }

        public void NewThreadForTrigger()
        {
        }


        [TestMethod]
        public void SCNamedPipeTrigger()
        {
            var aContextServer = this.SetUpContextServer();

            Console.WriteLine("About to start Monitoring! > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));

            var trigger = new NamedPipeTrigger()
            {
                Name = Test_ContextName
            };

            var aSequence = new Sequence
            {
                Activities =
                {
                    new WriteLine()
                    {
                        Text = new InArgument<string>((ctx) => $"Trigger triggered!!")
                    }
                }
            };

            var monitorActv = new UiPath.Core.Activities.MonitorEvents()
            {
                RepeatForever = false
            };
            monitorActv.Triggers.Add(trigger);
            monitorActv.Handler = new ActivityAction<object>
            {
                Handler = aSequence,
                Argument = new DelegateInArgument<object>("args")
            };

            var host = new WorkflowInvokerTest(new Sequence() { Activities = { monitorActv } } );
            var task = Task.Run(() => { host.TestActivity(TimeSpan.FromSeconds(15)); });

            // Trigger initialization takes time.
            Thread.Sleep(1000);

            Console.WriteLine("In thread > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));

            var sharedContextScopeActivity = new SharedContextScopeActivity
            {
                Name = Test_ContextName,
                Type = Test_ContextType,
                Clear = false,
                Retries = Test_Retries
            };

            var setContextActivity = new SharedContextVariableSetActivity
            {
                Name = Test_SetVariableName,
                Value = Test_SetVariableValue
            };

            sharedContextScopeActivity.Body.Handler = new Sequence()
            {
                Activities =
                {
                   setContextActivity
                }
            };

            Thread.Sleep(500);
            Console.WriteLine("In thread : about to execute > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));

            WorkflowInvoker.Invoke(sharedContextScopeActivity);

            Thread.Sleep(500);
            Console.WriteLine("In thread : executed > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));

            task.Wait(500);
            // Assert.IsTrue(TaskStatus.RanToCompletion == task.Status);
            Assert.IsTrue(host.TextLines.Length == 1);
            Assert.IsTrue(host.TextLines[0] == $"Trigger triggered!!");

            aContextServer.MyDispose();
        }
    }
}
