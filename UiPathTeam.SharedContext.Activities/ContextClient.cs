using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPathTeam.SharedContext.Activities
{
    public enum contextType
    {
        File,
        NamedPipe
    }

    public abstract class IContextClient
    {
        protected Dictionary<string, string> arguments;

        public string contextName;
        public ContextContent deserialisedContextContents;
        public string originalContextContents;

        abstract public void CreateServer();
        abstract public void CreateClient(bool iLock = true);
        abstract public void MyDispose();
        abstract public string GetResource();
    }

    [Serializable]
    public class ContextContent : Object
    {
        public Dictionary<string, string> GlobalVariables;
        public Dictionary<string, List<ContextMessage>> Messages;
        public bool TakeLock;

        public ContextContent()
        {
            this.TakeLock = false;
            this.GlobalVariables = new Dictionary<string, string>();
            this.Messages = new Dictionary<string, List<ContextMessage>>();
        }
    }

    [Serializable]
    public struct ContextMessage
    {
        public string From;
        public string To;
        public string Action;
        public string ArgumentsJson;
        public DateTime DateSent;
    }

    public class ContextClient
    {
        private IContextClient myContextClient;

        public ContextClient(contextType iType, string iContextName, Dictionary<string, string> iArguments)
        {
            this.myContextClient = null;
            switch (iType)
            {
                case contextType.File:
                    this.myContextClient = new ContextClientFile(iContextName, iArguments);
                    return;
                case contextType.NamedPipe:
                    this.myContextClient = new ContextClientNamedPipe(iContextName, iArguments);
                    return;
                default:
                    throw new Exception("[SharedContext] Unknown Context Type");
            }
        }

        public void CreateServer()
        {
            this.myContextClient.CreateServer();
        }

        public void CreateClient(bool iLock = true)
        {
            this.myContextClient.CreateClient(iLock);
        }

        public void MyDispose()
        {
            this.myContextClient.MyDispose();
        }

        public string GetResource()
        {
            return this.myContextClient.GetResource();
        }

        public void SetVariable(string iVariableName, string iVariableValue)
        {
            this.myContextClient.deserialisedContextContents.GlobalVariables[iVariableName] = iVariableValue;
        }

        public string GetVariable(string iVariableName, bool iRaiseException)
        {
            if (this.myContextClient.deserialisedContextContents.GlobalVariables.ContainsKey(iVariableName))
            {
                return this.myContextClient.deserialisedContextContents.GlobalVariables[iVariableName];
            }

            string errorMessage = "[SharedContext] Variable Name " + iVariableName + " does not exist in context " + this.myContextClient.contextName;

            if (iRaiseException)
            {
                throw new Exception(errorMessage);
            }
            Console.Error.WriteLine(errorMessage);
            return "";
        }

        public bool GetNextMessage(string iProcessName, ref ContextMessage oContextMessage)
        {
            if (!this.myContextClient.deserialisedContextContents.Messages.ContainsKey(iProcessName))
            {
                // Process has never interacted with SharedContext
                this.myContextClient.deserialisedContextContents.Messages[iProcessName] = new List<ContextMessage>();
            }

            if (this.myContextClient.deserialisedContextContents.Messages[iProcessName].Count == 0)
            {
                return false;
            }
            oContextMessage = this.myContextClient.deserialisedContextContents.Messages[iProcessName].First<ContextMessage>();
            this.myContextClient.deserialisedContextContents.Messages[iProcessName].RemoveAt(0);
            return true;
        }

        public void AddNewMessage(string iProcessName, ContextMessage iMessage)
        {
            if (!this.myContextClient.deserialisedContextContents.Messages.ContainsKey(iProcessName))
            {
                this.myContextClient.deserialisedContextContents.Messages[iProcessName] = new List<ContextMessage>();
            }
            this.myContextClient.deserialisedContextContents.Messages[iProcessName].Add(iMessage);
        }

        public void ClearAll()
        {
            this.myContextClient.deserialisedContextContents.GlobalVariables.Clear();
            this.myContextClient.deserialisedContextContents.Messages.Clear();
        }
    }
}
