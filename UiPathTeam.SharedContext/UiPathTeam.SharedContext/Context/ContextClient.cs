﻿using System;
using System.IO;
using System.Collections.Generic;

using System.Linq;

namespace UiPathTeam.SharedContext.Context
{
    public abstract class IContextClient : Logger
    {
        protected Dictionary<string, string> arguments;

        public string contextName;

        public ContextContent deserialisedContextContents;

        abstract public void CreateClient(bool iLock = true);
        abstract public void MyDispose();
        abstract public string GetResource();
    }

    public class ContextClient : Logger
    {
        private IContextClient myContextClient;

        public ContextClient(contextType iType, string iContextName, Dictionary<string, string> iArguments, bool iDebug)
        {
            this.myContextClient = null;
            this.Debug = iDebug;

            switch (iType)
            {
                case contextType.File:
                    this.myContextClient = new ContextClientFile(iContextName, iArguments, iDebug);
                    return;
                case contextType.NamedPipe:
                    this.myContextClient = new ContextClientNamedPipe(iContextName, iArguments, iDebug);
                    return;
                default:
                    throw new Exception("[SharedContext] Unknown Context Type");
            }
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

            string errorMessage = "[SharedContext Client] Variable Name " + iVariableName + " does not exist in context " + this.myContextClient.contextName;

            if (iRaiseException)
            {
                throw new Exception(errorMessage);
            }
            else
            {
                this.Error(errorMessage);
            }
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
