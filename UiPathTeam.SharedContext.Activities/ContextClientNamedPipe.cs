using System;
using System.Collections.Generic;
using NamedPipeWrapper;
using System.Threading;
using nsWin32Calls;

namespace UiPathTeam.SharedContext.Activities
{
    public class ContextClientNamedPipe : IContextClient
    {
        private NamedPipeClient<ContextContent> theClient;

        private int retriesMax;
        private int retriesUsed;

        private bool disposed = false;
        private bool initialised = false;

        private const string _mutexName = "Local\\ContextClientNamedPipe";

        private Mutex _dotNetMutex;

        public ContextClientNamedPipe(string iContextName, Dictionary<string, string> iArguments)
        {
            this.retriesUsed = 0;
            this.contextName = iContextName;
            this.arguments = iArguments;
            this.retriesMax = int.Parse(this.arguments["Retries"]);
            this.theClient = null;
            this._dotNetMutex = new Mutex(false, this.GetMutexName());
        }

        public override void CreateClient(bool iLock = true)
        {
            bool mutexTaken = false;

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

            if (!mutexTaken)
            {
                Console.WriteLine("[SharedContext Client] Could not open resource within the retries. > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
                throw new Exception("[SharedContext Client] Could not open resource within the retries.");
            }

            Console.WriteLine("[SharedContext Client] Connecting to Server. " + this.GetResource() + " > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));

            this.deserialisedContextContents = new ContextContent();

            this.theClient = new NamedPipeClient<ContextContent>(this.GetResource());
            this.theClient.ServerMessage += TheClient_ServerMessage;
            this.theClient.Error += TheClient_Error;
            this.theClient.Start();

            for (int i = 0; i < this.retriesMax; i++)
            {
                if(this.initialised)
                {
                    break;
                }
                Thread.Sleep(50);
            }

            if (!this.initialised)
            {
                throw new Exception("[SharedContext Client] Did not receive a message from the server!");
            }
        }

        public override void MyDispose()
        {
            if(!this.disposed)
            {
                Console.WriteLine("[SharedContext Client] Destroying. > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
                //                Win32Calls.ReleaseMutex(this.GetMutexName());

                this._dotNetMutex.ReleaseMutex();

                this.theClient.PushMessage(this.deserialisedContextContents);
                Thread.Sleep(30);
                this.theClient.Stop();
                this.disposed = true;
            }
        }

        ~ContextClientNamedPipe()
        {
            this.MyDispose();
        }

        private void TheClient_ServerMessage(NamedPipeConnection<ContextContent, ContextContent> connection, ContextContent message)
        {
            Console.WriteLine("[SharedContext Client] Message received . " + connection.Name + " > Message: " + message.ToString() + " > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
            this.deserialisedContextContents = message;
            if(!this.initialised)
            {
                Console.WriteLine("[SharedContext Client] Client initialised: " + connection.Name + " > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt"));
            }
            this.initialised = true;
        }

        private void TheClient_Error(Exception exception)
        {
            Console.WriteLine("[SharedContext Client] Exception > " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt") + " > " + exception.Message);
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
