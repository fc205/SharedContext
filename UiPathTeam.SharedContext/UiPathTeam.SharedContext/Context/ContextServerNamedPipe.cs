using System;
using System.Collections.Generic;
using NamedPipeWrapper;
using System.Threading;

namespace UiPathTeam.SharedContext.Context
{
    public class ContextServerNamedPipe : IContextServer
    {
        private NamedPipeServer<ContextContent> theServer;

        private bool disposed = false;

        public ContextServerNamedPipe(string iContextName, Dictionary<string, string> iArguments, bool iDebug)
        {
            this.contextName = iContextName;
            this.arguments = iArguments;
            this.theServer = null;
            this.Debug = iDebug;
        }

        public override void CreateServer()
        {
            this.Log("[SharedContext Server] Starting... " + this.GetResource() + " > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));

            this.deserialisedContextContents = new ContextContent();

            this.theServer = new NamedPipeServer<ContextContent>(this.GetResource());
            this.theServer.ClientMessage += TheServer_ClientMessage;
            this.theServer.ClientConnected += TheServer_ClientConnected;
            this.theServer.ClientDisconnected += TheServer_ClientDisconnected;
            this.theServer.Error += TheServer_Error;
            this.theServer.Start();
        }

        public override void MyDispose()
        {
            if(!this.disposed)
            {
                this.Log("[SharedContext Server] Destroying. > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));

                bool connectionsActive = true;
                for(int i = 0; i < 10; i++)
                {
                    if(this.theServer._connections.Count != 0)
                    {
                        Thread.Sleep(5);
                    }
                    else
                    {
                        connectionsActive = false;
                        break;
                    }
                }

                if (connectionsActive)
                {
                    this.Log("[SharedContext Server] Destroying. Connections still active > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
                }

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
            this.Log("[SharedContext Server] Message received . " + connection.Name + " > Message: " + message.ToString() + " > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));

            if(this.deserialisedContextContents.ToString() != message.ToString())
            {
                this.Log("[SharedContext Server] Using message as new content > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
                this.deserialisedContextContents = message;

                this.Log("[SharedContext Server] Sending message to all clients > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
                this.theServer.PushMessage(this.deserialisedContextContents);
            }
            else
            {
                this.Log("[SharedContext Server] No need to notify others. The context is the same as before. > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
            }
        }

        private void TheServer_ClientConnected(NamedPipeConnection<ContextContent, ContextContent> connection)
        {
            // Send current context
            this.Log("[SharedContext Server] Client connected. " + connection.Name + " > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
            this.Log("[SharedContext Server] Sending context: " + this.deserialisedContextContents.ToString() + " > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
            connection.PushMessage(this.deserialisedContextContents);
        }

        private void TheServer_ClientDisconnected(NamedPipeConnection<ContextContent, ContextContent> connection)
        {
            this.Log("[SharedContext Server] Client disconnected. " + connection.Name + " > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
        }

        private void TheServer_Error(Exception exception)
        {
            this.Log("[SharedContext Server] There is an error!! > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
            this.Log(exception.Message);
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
