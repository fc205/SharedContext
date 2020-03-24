using System;
using System.Collections.Generic;
using NamedPipeWrapper;
using System.Threading;

namespace UiPathTeam.SharedContext.Activities
{
    public class ContextServerNamedPipe : IContextServer
    {
        private NamedPipeServer<ContextContent> theServer;
        private int Lock;

        private int retriesMax;
        private int retriesUsed;

        private bool disposed = false;

        public ContextServerNamedPipe(string iContextName, Dictionary<string, string> iArguments)
        {
            this.retriesUsed = 0;
            this.Lock = -1;
            this.contextName = iContextName;
            this.arguments = iArguments;
            this.retriesMax = int.Parse(this.arguments["Retries"]);
            this.theServer = null;
        }

        public override void CreateServer()
        {
            this.deserialisedContextContents = new ContextContent();
            this.theServer = new NamedPipeServer<ContextContent>(this.GetResource());
            this.theServer.ClientMessage += TheServer_ClientMessage;
            this.theServer.ClientDisconnected += TheServer_ClientDisconnected;
            this.theServer.Error += TheServer_Error;
            this.theServer.Start();
        }

        public override void MyDispose()
        {
            if(!this.disposed)
            {
                this.theServer.Stop();
                this.disposed = true;
            }
        }

        ~ContextServerNamedPipe()
        {
            this.MyDispose();
        }

        private void TheServer_ClientMessage(NamedPipeConnection<ContextContent, ContextContent> connection, ContextContent message)
        {
            if(message.TakeLock)
            {
                if (this.Lock != -1 && this.Lock != connection.Id)
                {
                    // This is LOCKED
                    throw new Exception("[SharedContext] Already locked!");
                }
                else
                {
                    // No lock
                    this.Lock = connection.Id;
                }
            }
            else
            {
                this.deserialisedContextContents = message;
                this.theServer.PushMessage(this.deserialisedContextContents);
            }
        }

        private void TheServer_ClientDisconnected(NamedPipeConnection<ContextContent, ContextContent> connection)
        {
            if (this.Lock != -1 && this.Lock == connection.Id)
            {
                // This is LOCKED by ME!
                this.Lock = -1;
            }
        }

        private void TheServer_Error(Exception exception)
        {
            Console.Error.WriteLine("[SharedContext] There is an error!!");
            Console.Error.WriteLine(exception.Message);
        }

        //////////////////////////////////////////////////

        public override string GetResource()
        {
            return this.GetPipeName();
        }

        private string GetPipeName()
        {
            string anOutputFileName = Environment.UserDomainName + "_" +
                                   Environment.UserName + "_" +
                                   this.contextName;
            return anOutputFileName;
        }

    }
}
