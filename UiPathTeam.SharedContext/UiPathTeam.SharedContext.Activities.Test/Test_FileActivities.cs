using System;
using System.IO;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiPathTeam.SharedContext.Context;
using System.Threading;

namespace UiPathTeam.SharedContext.Activities.Test
{
    [TestClass]
    public class Test_FileActivities
    {
        public const string Test_ContextName = "aTestContext";
        public const contextType Test_ContextType = contextType.File;
        public const int Test_Retries = 5;

        public const string Test_SetVariableName = "aVariable123";
        public const string Test_SetVariableValue = "aValue456";

        public const string Test_SendOrigin = "DummyProcess";
        public const string Test_SendDestination = "DummyProcess";
        public const string Test_SendMessage_Action = "Do-Something";
        public const string Test_SendMessage_Arguments = "{\"some_argument\":\"aValue\"}";

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

            ContextClient aContext;
            aContext = new ContextClient(Test_ContextType, Test_ContextName, aDictionary);
            aContext.CreateClient();
            aContext.ClearAll();
            return aContext;
        }

        [TestMethod]
        public void SCFileSetVariable()
        {
            string aFileName = ClearEnvironmentForTest();
            ContextClient contextClient = SetUpContextClient();

            var setContextActivity = new SetVariableActivity
            {
                VariableName = Test_SetVariableName,
                VariableValue = Test_SetVariableValue,
                ContextClient = new InArgument<ContextClient>((ctx) => contextClient)
            };

            var output = WorkflowInvoker.Invoke(setContextActivity);

            contextClient.MyDispose();

            string fileContents = File.ReadAllText(aFileName);
            Assert.IsTrue(fileContents == "{\"GlobalVariables\":{\"aVariable123\":\"aValue456\"},\"Messages\":{}}");
        }

        [TestMethod]
        public void SCFileGetVariable()
        {
            string aFileName = ClearEnvironmentForTest();
            ContextClient contextClient = SetUpContextClient();

            var setContextActivity = new SetVariableActivity
            {
                VariableName = Test_SetVariableName,
                VariableValue = Test_SetVariableValue,
                ContextClient = new InArgument<ContextClient>((ctx) => contextClient)
            };

            var getContextActivity = new GetVariableActivity
            {
                VariableName = Test_SetVariableName,
                ContextClient = new InArgument<ContextClient>((ctx) => contextClient)
            };

            WorkflowInvoker.Invoke(setContextActivity);
            var output = WorkflowInvoker.Invoke(getContextActivity);

            contextClient.MyDispose();

            string fileContents = File.ReadAllText(aFileName);
            Assert.IsTrue(fileContents == "{\"GlobalVariables\":{\"aVariable123\":\"aValue456\"},\"Messages\":{}}");
            Assert.IsTrue(output["VariableValue"].ToString() == Test_SetVariableValue);
        }

        [TestMethod]
        public void SCFileSendMessage()
        {
            string aFileName = ClearEnvironmentForTest();
            ContextClient contextClient = SetUpContextClient();

            var sendMessageContextActivity = new SendMessageActivity
            {
                To = Test_SendDestination,
                Action = Test_SendMessage_Action,
                ArgumentsJson = Test_SendMessage_Arguments,
                ContextClient = new InArgument<ContextClient>((ctx) => contextClient)
            };

            var output = WorkflowInvoker.Invoke(sendMessageContextActivity);

            contextClient.MyDispose();

            string fileContents = File.ReadAllText(aFileName);
            Assert.IsTrue(fileContents.Contains("{\"GlobalVariables\":{},\"Messages\":{\"DummyProcess\":[{\"From\":\"DummyProcess\",\"To\":\"DummyProcess\",\"Action\":\"Do-Something\",\"ArgumentsJson\":\"{\\\"some_argument\\\":\\\"aValue\\\"}\",\"DateSent\":"));
        }

        [TestMethod]
        public void SCFileReceiveMessage()
        {
            string aFileName = ClearEnvironmentForTest();
            ContextClient contextClient = SetUpContextClient();

            var sendMessageContextActivity = new SendMessageActivity
            {
                To = Test_SendDestination,
                Action = Test_SendMessage_Action,
                ArgumentsJson = Test_SendMessage_Arguments,
                ContextClient = new InArgument<ContextClient>((ctx) => contextClient)
            };

            var receiveMessageContextActivity = new ReceiveMessageActivity
            {
                ContextClient = new InArgument<ContextClient>((ctx) => contextClient)
            };

            WorkflowInvoker.Invoke(sendMessageContextActivity);
            var output = WorkflowInvoker.Invoke(receiveMessageContextActivity);

            contextClient.MyDispose();

            string fileContents = File.ReadAllText(aFileName);
            Assert.IsTrue(output["Action"].ToString() == Test_SendMessage_Action);
            Assert.IsTrue(output["ArgumentsJson"].ToString() == Test_SendMessage_Arguments);
            Assert.IsTrue(output["To"].ToString() == Test_SendOrigin);
            Assert.IsTrue(output["From"].ToString() == Test_SendOrigin);
            Assert.IsTrue(((DateTime)output["TimeSent"]).ToString("YYYY-MM-DD") == DateTime.Now.ToString("YYYY-MM-DD"));
            Assert.IsTrue(!(bool)output["MessageQueueEmpty"]);
            Assert.IsTrue(fileContents == "{\"GlobalVariables\":{},\"Messages\":{\"DummyProcess\":[]}}");
        }

        [TestMethod]
        public void SCFileReceiveMessageEmpty()
        {
            string aFileName = ClearEnvironmentForTest();
            ContextClient contextClient = SetUpContextClient();

            var receiveMessageContextActivity = new ReceiveMessageActivity
            {
                ContextClient = new InArgument<ContextClient>((ctx) => contextClient)
            };

            var output = WorkflowInvoker.Invoke(receiveMessageContextActivity);

            contextClient.MyDispose();

            string fileContents = File.ReadAllText(aFileName);

            Assert.IsTrue((bool)output["MessageQueueEmpty"]);
            Assert.IsTrue(fileContents == "{\"GlobalVariables\":{},\"Messages\":{\"DummyProcess\":[]}}");
        }

        [TestMethod]
        public void SCFileScopeWithSharedContextSetInside()
        {
            string aFileName = this.ClearEnvironmentForTest();

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

            Assert.IsTrue(File.Exists(aFileName));
            string fileContents = File.ReadAllText(aFileName);
            Assert.IsTrue(fileContents == "{\"GlobalVariables\":{\"aVariable123\":\"aValue456\"},\"Messages\":{}}");
        }

        [TestMethod]
        public void SCFileScopeWithClearInside()
        {
            string aFileName = this.ClearEnvironmentForTest();

            var sharedContextScopeActivity = new ClientScopeActivity
            {
                ContextName = Test_ContextName,
                ContextType = Test_ContextType,
                Retries = Test_Retries,
                ClearContext = true
            };

            sharedContextScopeActivity.Body.Handler = new Sequence()
            {
                Activities =
                {
                }
            };

            var output = WorkflowInvoker.Invoke(sharedContextScopeActivity);

            Assert.IsTrue(output["OutputContextFile"].ToString() == aFileName);
            Assert.IsTrue(File.ReadAllText(aFileName) == "{\"GlobalVariables\":{},\"Messages\":{}}");
        }

        [TestMethod]
        public void SCFileScopeWithSharedContextGetInside()
        {
            string aFileName = this.ClearEnvironmentForTest();

            var sharedContextScopeActivity = new ClientScopeActivity
            {
                ContextName = Test_ContextName,
                ContextType = Test_ContextType,
                Retries = Test_Retries
            };

            var getContextActivity = new GetVariableActivity
            {
                VariableName = Test_SetVariableName,
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
            Assert.IsTrue(output["OutputContextFile"].ToString() == aFileName);
        }

        [TestMethod]
        public void SCFileScopeWithSharedContextGetInsideException()
        {
            string aFileName = this.ClearEnvironmentForTest();

            var sharedContextScopeActivity = new ClientScopeActivity
            {
                ContextName = Test_ContextName,
                ContextType = Test_ContextType,
                Retries = Test_Retries
            };

            var getContextActivity = new GetVariableActivity
            {
                VariableName = Test_SetVariableName,
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
                Assert.IsTrue(e.Message == "[SharedContext Client] Variable Name " + Test_SetVariableName + " does not exist in context " + Test_ContextName);
            }
        }

        public void NewThread()
        {
            Console.WriteLine("In thread > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
            string aFileName = ClearEnvironmentForTest(true);

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

            Console.WriteLine("In thread : about to execute > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
            var output = WorkflowInvoker.Invoke(sharedContextScopeActivity);
        }

        [TestMethod]
        public void SCFileTryToOpenLockedFile()
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
                    catch(Exception)
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
