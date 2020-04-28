using System;
using System.Collections.Generic;
using NamedPipeWrapper;
using System.Threading;
// using UiPathTeam.SharedContext.Dependencies;

namespace UiPathTeam.SharedContext.Context
{
    public class ContextClientNamedPipe : IContextClient
    {
        private NamedPipeClient<ContextContent> theClient;

        private int retriesMax;
        private int retriesUsed;

        private bool disposed = false;
        private bool expectingLastMessage = false;
        private bool lastMessageReceived = false;
        private bool initialised = false;
        private bool _lock = false;

        private const string _mutexName = "Local\\ContextClientNamedPipe";

        private Mutex _dotNetMutex;

        public ContextClientNamedPipe(string iContextName, Dictionary<string, string> iArguments, bool iLock = true)
        {
            this.retriesUsed = 0;
            this._lock = iLock;
            this.contextName = iContextName;
            this.arguments = iArguments;
            this.retriesMax = int.Parse(this.arguments["Retries"]);
            this.theClient = null;
            if(iLock)
            {
                this._dotNetMutex = new Mutex(false, this.GetMutexName());
            }
        }

        public override void CreateClient(bool iLock = true)
        {
            bool mutexTaken = false;

            if(!this._lock)
            {
                retriesUsed = retriesMax;
            }

            while (this.retriesUsed < this.retriesMax)
            {
//                if (Win32Calls.TakeMutex(this.GetMutexName()))
                if(this._dotNetMutex.WaitOne())
                {
                    this.retriesUsed = this.retriesMax + 1;
                    mutexTaken = true;
                }
                else
                {
                    Console.WriteLine("[SharedContext Client] Mutex conflict with " +
                        this.GetMutexName() +
                        " @ " +
                        DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt") +
                        ". I will retry shortly.");

                    string nowString = DateTime.Now.ToFileTime().ToString();
                    var aRandom = new Random(int.Parse(nowString.Substring(nowString.Length - 9, 9)));

                    Thread.Sleep(aRandom.Next(0, 100 * this.retriesUsed));

                    this.retriesUsed++;
                }
            }

            if (!mutexTaken && this._lock)
            {
                Console.WriteLine("[SharedContext " + (this._lock ? "Client" : "Trigger") + "] Could not open resource within the retries. > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
                throw new Exception("[SharedContext " + (this._lock ? "Client" : "Trigger") + "] Could not open resource within the retries.");
            }

            Console.WriteLine("[SharedContext " + (this._lock ? "Client" : "Trigger") + "] Connecting to Server. " + this.GetResource() + " > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));

            this.deserialisedContextContents = new ContextContent();

            this.theClient = new NamedPipeClient<ContextContent>(this.GetResource());
            this.theClient.ServerMessage += TheClient_ServerMessage;
            this.theClient.Error += TheClient_Error;
            this.theClient.AutoReconnect = false;
            this.theClient.Start();

            if(!this._lock)
            {
                this.retriesMax = 0;
            }

            for (int i = 0; i < this.retriesMax; i++)
            {
                if(this.initialised)
                {
                    break;
                }
                Thread.Sleep(50);
            }

            if (!this.initialised && this._lock)
            {
                throw new Exception("[SharedContext " + (this._lock ? "Client" : "Trigger") + "] Did not receive a message from the server!");
            }
        }

        public override void MyDispose()
        {
            if(!this.disposed)
            {
                Console.WriteLine("[SharedContext " + (this._lock?"Client":"Trigger") + "] Destroying. > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
                if(this._lock)
                {
                    this.theClient.PushMessage(this.deserialisedContextContents);

                    this.expectingLastMessage = true;
                    for (int i = 0; i < 10; i++)
                    {
                        if (!this.lastMessageReceived)
                        {
                            Thread.Sleep(5);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (!this.lastMessageReceived)
                    {
                        Console.WriteLine("[SharedContext Client] Destroying. The last Server message hasn't been received > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
                    }

                    // Win32Calls.ReleaseMutex(this.GetMutexName());

                    this._dotNetMutex.ReleaseMutex();
                }

                this.theClient.Stop();
                this.theClient = null;
                this.disposed = true;
            }
        }

        ~ContextClientNamedPipe()
        {
            this.MyDispose();
        }

        public event Action<object, ContextContent> EventHandler;

        private void TheClient_ServerMessage(NamedPipeConnection<ContextContent, ContextContent> connection, ContextContent message)
        {

            if (!this.initialised)
            {
                Console.WriteLine("[SharedContext Client] " + (this._lock ? "Client" : "Trigger") + " initialised: " + connection.Name + " > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
                this.initialised = true;
                if(!this._lock)
                {
                    return;
                }
            }

            if (this.expectingLastMessage)
            {
                this.lastMessageReceived = true;
            }

            Console.WriteLine("[SharedContext " + (this._lock ? "Client" : "Trigger") + "] Message received . " + connection.Name + " > Message: " + message.ToString() + " > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
            this.deserialisedContextContents = message;

            if(EventHandler != null)
            {
                EventHandler.Invoke((object)connection, message);
            }
        }

        private void TheClient_Error(Exception exception)
        {
            Console.WriteLine("[SharedContext " + (this._lock ? "Client" : "Trigger") + "] Exception > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt") + " > " + exception.Message);
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

        private string GetMutexName()
        {
            return _mutexName + this.GetPipeName();
        }
    }
}
