using System;
using System.IO;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading;

namespace UiPathTeam.SharedContext.Activities.Test
{
    [TestClass]
    public class Test_SharedContextActivities
    {
        public const string Test_ContextName = "aTestContext";
        public const contextType Test_ContextType = contextType.File;
        public const int Test_Retries = 5;

        public const string Test_SetVariableName = "aVariable123";
        public const string Test_SetVariableValue = "aValue456";

        public const string Test_SendDestination = "DummyProcess";
        public const string Test_SendMessage = "KillRoy was here!";

        protected string ClearEnvironmentForTest(bool preserveFile = false)
        {
            string aFileName = Environment.UserDomainName + "_" + Environment.UserName + "_" + Test_ContextName + ".txt";
            string aFilePath = Path.Combine(Path.GetTempPath(), aFileName);

            if (File.Exists(aFilePath) && !preserveFile)
            {
                File.Delete(aFilePath);
            }
            return aFilePath;
        }

        protected ContextClient SetUpContextClient()
        {
            var aDictionary = new Dictionary<string, string>();
            aDictionary["Retries"] = Test_Retries.ToString();
            aDictionary["Lock"] = "Y";
            return new ContextClient(Test_ContextType, Test_ContextName, aDictionary);
        }

        [TestMethod]
        public void SharedContextSetVariable()
        {
            string aFileName = ClearEnvironmentForTest();
            ContextClient contextClient = SetUpContextClient();

            var setContextActivity = new SharedContextVariableSetActivity
            {
                Name = Test_SetVariableName,
                Value = Test_SetVariableValue,
                ContextClient = new InArgument<ContextClient>((ctx) => contextClient)
            };

            var output = WorkflowInvoker.Invoke(setContextActivity);

            contextClient.Dispose();

            string fileContents = File.ReadAllText(aFileName);
            Assert.IsTrue(fileContents == "{\"GlobalVariables\":{\"aVariable123\":\"aValue456\"},\"Messages\":{}}");
        }

        [TestMethod]
        public void SharedContextGetVariable()
        {
            string aFileName = ClearEnvironmentForTest();
            ContextClient contextClient = SetUpContextClient();

            var setContextActivity = new SharedContextVariableSetActivity
            {
                Name = Test_SetVariableName,
                Value = Test_SetVariableValue,
                ContextClient = new InArgument<ContextClient>((ctx) => contextClient)
            };

            var getContextActivity = new SharedContextVariableGetActivity
            {
                Name = Test_SetVariableName,
                ContextClient = new InArgument<ContextClient>((ctx) => contextClient)
            };

            WorkflowInvoker.Invoke(setContextActivity);
            var output = WorkflowInvoker.Invoke(getContextActivity);

            contextClient.Dispose();

            string fileContents = File.ReadAllText(aFileName);
            Assert.IsTrue(fileContents == "{\"GlobalVariables\":{\"aVariable123\":\"aValue456\"},\"Messages\":{}}");
            Assert.IsTrue(output["Value"].ToString() == Test_SetVariableValue);
        }

        [TestMethod]
        public void SharedContextSendMessage()
        {
            string aFileName = ClearEnvironmentForTest();
            ContextClient contextClient = SetUpContextClient();

            var sendMessageContextActivity = new SharedContextMessageSendActivity
            {
                DestinationProcess = Test_SendDestination,
                MessageContent = Test_SendMessage,
                ContextClient = new InArgument<ContextClient>((ctx) => contextClient)
            };

            var output = WorkflowInvoker.Invoke(sendMessageContextActivity);

            contextClient.Dispose();

            string fileContents = File.ReadAllText(aFileName);
            Assert.IsTrue(fileContents.Contains("{\"GlobalVariables\":{},\"Messages\":{\"DummyProcess\":[{\"From\":\"DummyProcess\",\"To\":\"DummyProcess\",\"Message\":\"KillRoy was here!\",\"DateSent\":\""));
        }

        [TestMethod]
        public void SharedContextReceiveMessage()
        {
            string aFileName = ClearEnvironmentForTest();
            ContextClient contextClient = SetUpContextClient();

            var sendMessageContextActivity = new SharedContextMessageSendActivity
            {
                DestinationProcess = Test_SendDestination,
                MessageContent = Test_SendMessage,
                ContextClient = new InArgument<ContextClient>((ctx) => contextClient)
            };

            var receiveMessageContextActivity = new SharedContextMessageReceiveActivity
            {
                ContextClient = new InArgument<ContextClient>((ctx) => contextClient)
            };

            WorkflowInvoker.Invoke(sendMessageContextActivity);
            var output = WorkflowInvoker.Invoke(receiveMessageContextActivity);

            contextClient.Dispose();

            string fileContents = File.ReadAllText(aFileName);
            Assert.IsTrue(output["MessageContent"].ToString() == Test_SendMessage);
            Assert.IsTrue(output["OriginProcess"].ToString() == Test_SendDestination);
            Assert.IsTrue(((DateTime)output["TimeSent"]).ToString("YYYY-MM-DD") == DateTime.Now.ToString("YYYY-MM-DD"));
            Assert.IsTrue(!(bool)output["QueueEmpty"]);
            Assert.IsTrue(fileContents == "{\"GlobalVariables\":{},\"Messages\":{\"DummyProcess\":[]}}");
        }

        [TestMethod]
        public void SharedContextScopeWithSharedContextSetInside()
        {
            string aFileName = this.ClearEnvironmentForTest();

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

            Assert.IsTrue(File.Exists(aFileName));
            string fileContents = File.ReadAllText(aFileName);
            Assert.IsTrue(fileContents == "{\"GlobalVariables\":{\"aVariable123\":\"aValue456\"},\"Messages\":{}}");
        }

        [TestMethod]
        public void SharedContextScopeWithClearInside()
        {
            string aFileName = this.ClearEnvironmentForTest();

            var sharedContextScopeActivity = new SharedContextScopeActivity
            {
                Name = Test_ContextName,
                Type = Test_ContextType,
                Retries = Test_Retries,
                Clear = true
            };

            sharedContextScopeActivity.Body.Handler = new Sequence()
            {
                Activities =
                {
                }
            };

            var output = WorkflowInvoker.Invoke(sharedContextScopeActivity);

            Assert.IsTrue(output["FilePath"].ToString() == aFileName);
            Assert.IsTrue(File.ReadAllText(aFileName) == "{\"GlobalVariables\":{},\"Messages\":{}}");
        }

        [TestMethod]
        public void SharedContextScopeWithSharedContextGetInside()
        {
            string aFileName = this.ClearEnvironmentForTest();

            var sharedContextScopeActivity = new SharedContextScopeActivity
            {
                Name = Test_ContextName,
                Type = Test_ContextType,
                Retries = Test_Retries
            };

            var getContextActivity = new SharedContextVariableGetActivity
            {
                Name = Test_SetVariableName,
                RaiseException = false
            };

            sharedContextScopeActivity.Body.Handler = new Sequence()
            {
                Activities =
                {
                   getContextActivity
                }
            };

            var output = WorkflowInvoker.Invoke(sharedContextScopeActivity);
            Assert.IsTrue(output["FilePath"].ToString() == aFileName);
        }

        [TestMethod]
        public void SharedContextScopeWithSharedContextGetInsideException()
        {
            string aFileName = this.ClearEnvironmentForTest();

            var sharedContextScopeActivity = new SharedContextScopeActivity
            {
                Name = Test_ContextName,
                Type = Test_ContextType,
                Retries = Test_Retries
            };

            var getContextActivity = new SharedContextVariableGetActivity
            {
                Name = Test_SetVariableName,
                RaiseException = true
            };

            sharedContextScopeActivity.Body.Handler = new Sequence()
            {
                Activities =
                {
                   getContextActivity
                }
            };

            try
            {
                var output = WorkflowInvoker.Invoke(sharedContextScopeActivity);
            }
            catch(Exception e)
            {
                Assert.IsTrue(e.Message == "Variable Name " + Test_SetVariableName + " does not exist in context " + Test_ContextName);
            }
        }

        public void NewThread()
        {
            Console.WriteLine("In thread > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
            string aFileName = ClearEnvironmentForTest(true);

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

            Console.WriteLine("In thread : about to execute > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
            var output = WorkflowInvoker.Invoke(sharedContextScopeActivity);
        }

        [TestMethod]
        public void SharedContextTryToOpenLockedFile()
        {
            string aFileName = this.ClearEnvironmentForTest();

            Thread thread1 = new Thread(NewThread);
            thread1.Start();
            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine("In main. i=" + i.ToString() + " > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));

                if (i == 0 || i == 3)
                {
                    try
                    {
                        using (FileStream fileStream = new FileStream(aFileName,
                                    FileMode.OpenOrCreate,
                                    FileAccess.ReadWrite,
                                    FileShare.None
                                    ))
                        {
                            Console.WriteLine("File locked! > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
                            Thread.Sleep(200);
                        }
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("Could not lock File > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
                        Thread.Sleep(50);
                    }
                }
                Thread.Sleep(200);
            }
        }
    }
}
