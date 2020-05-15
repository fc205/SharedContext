using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPathTeam.SharedContext.Context
{
    public abstract class IContextServer
    {
        protected Dictionary<string, string> arguments;

        public string contextName;
        public ContextContent deserialisedContextContents;

        abstract public void CreateServer();
        abstract public void MyDispose();
        abstract public string GetResource();
    }

    public class ContextServer
    {
        private IContextServer myContextServer;

        public ContextServer(contextType iType, string iContextName, Dictionary<string, string> iArguments)
        {
            this.myContextServer = null;

            switch (iType)
            {
                case contextType.NamedPipe:
                    this.myContextServer = new ContextServerNamedPipe(iContextName, iArguments);
                    return;
                default:
                    throw new Exception("[SharedContext] Unknown Context Type");
            }
        }

        public void CreateServer()
        {
            this.myContextServer.CreateServer();
        }

        public void MyDispose()
        {
            this.myContextServer.MyDispose();
        }

        public string GetResource()
        {
            return this.myContextServer.GetResource();
        }
    }
}
