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
        File
    }

    public abstract class IContextClient
    {
        protected Dictionary<string, string> arguments;
        protected string contextName;

        abstract public void MyDispose();
        abstract public void SetVariable(string iVariableName, string iVariableValue);
        abstract public string GetVariable(string iVariableName, bool iRaiseException);
        abstract public bool GetNextMessage(string iProcessName, ref ContextMessage oContextMessage);
        abstract public void AddNewMessage(string iProcessName, ContextMessage iMessage);
        abstract public void ClearAll();
        abstract public string GetResource();
    }

    public class ContextContent : Object
    {
        public Dictionary<string, string> GlobalVariables;
        public Dictionary<string, List<ContextMessage>> Messages;

        public ContextContent()
        {
            this.GlobalVariables = new Dictionary<string, string>();
            this.Messages = new Dictionary<string, List<ContextMessage>>();
        }
    }

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
                default:
                    throw new Exception("Unknown Context Type");
            }
        }

        public void MyDispose()
        {
            this.myContextClient.MyDispose();
        }

        public void SetVariable(string iVariableName, string iVariableValue)
        {
            this.myContextClient.SetVariable(iVariableName, iVariableValue);
        }

        public string GetVariable(string iVariableName, bool iRaiseException)
        {
            return this.myContextClient.GetVariable(iVariableName, iRaiseException);
        }

        public bool GetNextMessage(string iProcessName, ref ContextMessage oContextMessage)
        {
            return this.myContextClient.GetNextMessage(iProcessName, ref oContextMessage);
        }

        public void AddNewMessage(string iProcessName, ContextMessage iMessage)
        {
            this.myContextClient.AddNewMessage(iProcessName, iMessage);
        }

        public void ClearAll()
        {
            this.myContextClient.ClearAll();
        }

        public string GetResource()
        {
            return this.myContextClient.GetResource();
        }

    }
}
