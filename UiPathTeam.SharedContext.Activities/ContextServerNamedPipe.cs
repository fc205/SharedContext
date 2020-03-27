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

        private static Semaphore mySemaphore = new Semaphore(0, 1);

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
            Console.WriteLine("[SharedContext Server] Starting... " + this.GetResource());

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
                Console.WriteLine("[SharedContext Server] Destroying.");

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
            Console.WriteLine("[SharedContext Server] Message received . " + connection.Name + " > Message: " + message.ToString());

            if (ContextServerNamedPipe.mySemaphore.WaitOne(10000))
            {
                if (message.TakeLock)
                {
                    if (this.Lock != -1 && this.Lock != connection.Id)
                    {
                        // This is LOCKED
                        ContextServerNamedPipe.mySemaphore.Release();
                        throw new Exception("[SharedContext Server] Already locked!");
                    }
                    else
                    {
                        // No lock
                        this.Lock = connection.Id;
                    }
                }

                if (this.Lock != -1 && this.Lock == connection.Id && message.Commit)
                {
                    this.deserialisedContextContents = message;
                    this.deserialisedContextContents.Commit = false;
                }

                this.theServer.PushMessage(this.deserialisedContextContents);
                ContextServerNamedPipe.mySemaphore.Release();
            }
            Console.WriteLine("[SharedContext Server] Message received . " + connection.Name + " > Outcome: " + this.deserialisedContextContents.ToString());
        }

        private void TheServer_ClientConnected(NamedPipeConnection<ContextContent, ContextContent> connection)
        {
            // Do nothing
            Console.WriteLine("[SharedContext Server] Client connected. " + connection.Name);
        }

        private void TheServer_ClientDisconnected(NamedPipeConnection<ContextContent, ContextContent> connection)
        {
            Console.WriteLine("[SharedContext Server] Client disconnected. " + connection.Name);

            if (this.Lock != -1 && this.Lock == connection.Id)
            {
                // This is LOCKED by ME! Release the lock
                this.Lock = -1;
            }
        }

        private void TheServer_Error(Exception exception)
        {
            Console.WriteLine("[SharedContext Server] There is an error!!");
            Console.WriteLine(exception.Message);
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
