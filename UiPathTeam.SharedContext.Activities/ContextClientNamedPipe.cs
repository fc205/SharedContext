using System;
using System.Collections.Generic;
using NamedPipeWrapper;
using System.Threading;

namespace UiPathTeam.SharedContext.Activities
{
    public class ContextClientNamedPipe : IContextClient
    {
        private NamedPipeClient<ContextContent> theClient;

        private int retriesMax;
        private int retriesUsed;

        private bool disposed = false;

        public ContextClientNamedPipe(string iContextName, Dictionary<string, string> iArguments)
        {
            this.retriesUsed = 0;
            this.contextName = iContextName;
            this.arguments = iArguments;
            this.retriesMax = int.Parse(this.arguments["Retries"]);
            this.theClient = null;
        }

        public override void CreateClient(bool iLock = true)
        {
            this.deserialisedContextContents = new ContextContent();
            this.theClient = new NamedPipeClient<ContextContent>(this.GetResource());
            this.theClient.ServerMessage += TheClient_ServerMessage;
            this.theClient.Error += TheClient_Error;
            this.theClient.Start();

            if(iLock)
            {
                this.deserialisedContextContents.TakeLock = true;
                this.theClient.PushMessage(this.deserialisedContextContents);
            }
        }

        public override void MyDispose()
        {
            if(!this.disposed)
            {
                this.theClient.PushMessage(this.deserialisedContextContents);
                Thread.Sleep(20);
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
            this.deserialisedContextContents = message;
            // Mostly ignore...
            // This is for the Trigger only
        }

        private void TheClient_Error(Exception exception)
        {
            if(this.retriesUsed < this.retriesMax)
            {
                Console.WriteLine("[SharedContext] NamePipe conflict with " +
                    this.contextName +
                    " @ " +
                    DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt") +
                    ". I will retry shortly.");

                string nowString = DateTime.Now.ToFileTime().ToString();
                var aRandom = new Random(int.Parse(nowString.Substring(nowString.Length - 9, 9)));
                Thread.Sleep(aRandom.Next(0, 100 * this.retriesUsed));

                this.deserialisedContextContents.TakeLock = true;
                this.theClient.PushMessage(this.deserialisedContextContents);

                this.retriesUsed++;
            }
            else
            {
                Console.WriteLine("[SharedContext] Could not open FileStream within the retries.");
            }
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
