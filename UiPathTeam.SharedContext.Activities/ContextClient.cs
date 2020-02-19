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

    public abstract class IContextClient : IDisposable
    {
        protected Dictionary<string, string> arguments;
        protected string contextName;

        abstract public void Dispose();
        abstract public void Set(string iVariableName, string iVariableValue);
        abstract public string Get(string iVariableName, bool iRaiseException);
        abstract public void ClearAll();
        abstract public string GetResource();
    }

    public class ContextClient : IDisposable
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

        public void Dispose()
        {
            if (this.myContextClient != null)
            {
                this.myContextClient.Dispose();
            }
        }

        ~ContextClient()
        {
            if (this.myContextClient != null)
            {
                Dispose();
            }
        }

        public void Set(string iVariableName, string iVariableValue)
        {
            this.myContextClient.Set(iVariableName, iVariableValue);
        }

        public string Get(string iVariableName, bool iRaiseException)
        {
            return this.myContextClient.Get(iVariableName, iRaiseException);
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
