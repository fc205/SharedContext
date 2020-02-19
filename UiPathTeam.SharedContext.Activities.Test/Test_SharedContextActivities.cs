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

        [TestMethod]
        public void SharedContextScopeWithSharedContextSetInside()
        {
            string aFileName = Path.Combine(Path.GetTempPath(), Test_ContextName + ".txt");

            if(File.Exists(aFileName))
            {
                File.Delete(aFileName);
            }

            var sharedContextScopeActivity = new SharedContextScopeActivity
            {
                Name = Test_ContextName,
                Type = Test_ContextType,
                Retries = Test_Retries
            };

            var setContextActivity = new SharedContextSetActivity
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
        }

        [TestMethod]
        public void SharedContextScopeWithClearInside()
        {
            string aFileName = Path.Combine(Path.GetTempPath(), Test_ContextName + ".txt");

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
            Assert.IsTrue(File.ReadAllText(aFileName) == "{}");
        }

        [TestMethod]
        public void SharedContextScopeWithSharedContextGetInside()
        {
            string aFileName = Path.Combine(Path.GetTempPath(), Test_ContextName + ".txt");

            if (File.Exists(aFileName))
            {
                File.Delete(aFileName);
            }

            var sharedContextScopeActivity = new SharedContextScopeActivity
            {
                Name = Test_ContextName,
                Type = Test_ContextType,
                Retries = Test_Retries
            };

            var getContextActivity = new SharedContextGetActivity
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
        }

        [TestMethod]
        public void SharedContextScopeWithSharedContextGetInsideException()
        {
            string aFileName = Path.Combine(Path.GetTempPath(), Test_ContextName + ".txt");

            if (File.Exists(aFileName))
            {
                File.Delete(aFileName);
            }

            var sharedContextScopeActivity = new SharedContextScopeActivity
            {
                Name = Test_ContextName,
                Type = Test_ContextType,
                Retries = Test_Retries
            };

            var getContextActivity = new SharedContextGetActivity
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

        public static void NewThread()
        {
            Console.WriteLine("In thread > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
            string aFileName = Path.Combine(Path.GetTempPath(), Test_ContextName + ".txt");

            var sharedContextScopeActivity = new SharedContextScopeActivity
            {
                Name = Test_ContextName,
                Type = Test_ContextType,
                Retries = Test_Retries
            };

            var setContextActivity = new SharedContextSetActivity
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
            string aFileName = Path.Combine(Path.GetTempPath(), Test_ContextName + ".txt");
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

        [TestMethod]
        public void SharedContextGet()
        {
            string aFileName = Path.Combine(Path.GetTempPath(), Test_ContextName + ".txt");
            var aDictionary = new Dictionary<string, string>();
            aDictionary["Retries"] = Test_Retries.ToString();
            aDictionary["Lock"] = "Y";
            var contextClient = new ContextClient(Test_ContextType, Test_ContextName, aDictionary);

            var getContextActivity = new SharedContextGetActivity
            {
                Name = Test_SetVariableName,
                ContextClient = new InArgument<ContextClient>((ctx) => contextClient)
            };

            var output = WorkflowInvoker.Invoke(getContextActivity);

            contextClient.Dispose();

            Assert.IsFalse(string.IsNullOrEmpty(Convert.ToString(output["Value"])));
            Assert.IsTrue(Convert.ToString(output["Value"]) == Test_SetVariableValue);
            if (File.Exists(aFileName))
            {
                File.Delete(aFileName);
            }
            Assert.IsFalse(File.Exists(aFileName));
        }
    }
}
