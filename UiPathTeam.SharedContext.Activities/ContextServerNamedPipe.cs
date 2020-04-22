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

        private static Mutex myMutex = new Mutex(false, "mySemaphore");

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
            Console.WriteLine("[SharedContext Server] Starting... " + this.GetResource() + " > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));

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
                Console.WriteLine("[SharedContext Server] Destroying. > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));

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
            Console.WriteLine("[SharedContext Server] Message received . " + connection.Name + " > Message: " + message.ToString() + " > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));

            if (ContextServerNamedPipe.myMutex.WaitOne(10000))
            {
                Console.WriteLine("[SharedContext Server] Mutex taken > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
                if (message.TakeLock)
                {
                    Console.WriteLine("[SharedContext Server] Message wants to take lock > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
                    if (this.Lock != -1 && this.Lock != connection.Id)
                    {
                        // This is LOCKED
                        ContextServerNamedPipe.myMutex.ReleaseMutex();
                        Console.WriteLine("[SharedContext Server] Mutex released > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
                        throw new Exception("[SharedContext Server] Already locked! > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
                    }
                    else
                    {
                        // No lock
                        Console.WriteLine("[SharedContext Server] No lock. Taking it now > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
                        this.Lock = connection.Id;
                    }
                }

                if (this.Lock != -1 && this.Lock == connection.Id && message.Commit)
                {
                    Console.WriteLine("[SharedContext Server] Using message as new content > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
                    this.deserialisedContextContents = message;
                    this.deserialisedContextContents.TakeLock = false;
                    this.deserialisedContextContents.Commit = false;
                }

                Console.WriteLine("[SharedContext Server] Sending message to client " + connection.Name + " > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
                this.theServer.PushMessage(this.deserialisedContextContents);

                ContextServerNamedPipe.myMutex.ReleaseMutex();
                Console.WriteLine("[SharedContext Server] Mutex released > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
            }
            Console.WriteLine("[SharedContext Server] Message received . " + connection.Name + " > Outcome: " + this.deserialisedContextContents.ToString() + " > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
        }

        private void TheServer_ClientConnected(NamedPipeConnection<ContextContent, ContextContent> connection)
        {
            // Do nothing...
            Console.WriteLine("[SharedContext Server] Client connected. " + connection.Name + " > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
        }

        private void TheServer_ClientDisconnected(NamedPipeConnection<ContextContent, ContextContent> connection)
        {
            Console.WriteLine("[SharedContext Server] Client disconnected. " + connection.Name + " > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));

            if (this.Lock != -1 && this.Lock == connection.Id)
            {
                // This is LOCKED by ME! Release the lock
                this.Lock = -1;
                this.deserialisedContextContents.TakeLock = false;
            }
        }

        private void TheServer_Error(Exception exception)
        {
            Console.WriteLine("[SharedContext Server] There is an error!! > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
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
