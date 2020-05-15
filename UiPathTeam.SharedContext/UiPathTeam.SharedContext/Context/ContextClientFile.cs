using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;

namespace UiPathTeam.SharedContext.Context { 

    public class ContextClientFile : IContextClient
    {
        private string fileName;
        private FileStream fileStream;
        private string originalContextContents;

        private bool disposed = false;

        public ContextClientFile(string iContextName, Dictionary<string, string> iArguments)
        {
            this.fileName = "";
            this.contextName = iContextName;
            this.arguments = iArguments;
        }

        public override void CreateClient(bool iLock = true)
        {
            int retriesAttempted = 0;
            int retriesMax = int.Parse(this.arguments["Retries"]);

            while (retriesAttempted < retriesMax)
            {
                try
                {
                    Console.WriteLine("[SharedContext] Opening FileStream. Taking a lock on the file");
                    this.fileStream = new FileStream(this.GetFileName(),
                                                        FileMode.OpenOrCreate,
                                                        FileAccess.ReadWrite,
                                                        FileShare.None
                                                        );
                }
                catch (Exception e)
                {
                    this.fileStream = null;
                }

                if (this.fileStream != null) break;

                Console.WriteLine("[SharedContext] FileStream conflict with " +
                                    this.GetFileName() +
                                    " @ " +
                                    DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt") +
                                    ". I will retry shortly.");

                string nowString = DateTime.Now.ToFileTime().ToString();
                var aRandom = new System.Random(int.Parse(nowString.Substring(nowString.Length - 9, 9)));
                System.Threading.Thread.Sleep(aRandom.Next(0, 100 * retriesAttempted));

                retriesAttempted++;
            }

            if (this.fileStream != null)
            {
                // Read the contents and put them in the memory variable

                StreamReader sr = new StreamReader(this.fileStream);
                this.originalContextContents = sr.ReadToEnd();

                Console.WriteLine("[SharedContext] Read File Contents: " + this.originalContextContents);


                try
                {
                    this.deserialisedContextContents = JsonConvert.DeserializeObject<ContextContent>(this.originalContextContents);
                }
                catch (Exception e)
                {
                    Console.WriteLine("[SharedContext] Error deserializing the file. Emptying the file. > " +
                                                DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt") +
                                                " > " +
                                                e.Message);
                    this.deserialisedContextContents = null;
                }

                if (this.deserialisedContextContents == null)
                {
                    this.deserialisedContextContents = new ContextContent();
                }

            }
            else
            {
                throw new Exception("[SharedContext] Could not open FileStream within the retries.");
            }
        }

        public override void MyDispose()
        {
            if (!this.disposed)
            {
                string newFileContents = "";

                newFileContents = JsonConvert.SerializeObject(this.deserialisedContextContents);

                Console.WriteLine("[SharedContext] End File Contents before write: " + newFileContents);

                try
                {

                    if (newFileContents != this.originalContextContents)
                    {
                        if (this.fileStream != null)
                        {
                            this.fileStream.Position = 0;
                            Byte[] newFileContentsBytes = Encoding.UTF8.GetBytes(newFileContents);
                            this.fileStream.SetLength(newFileContentsBytes.Length);
                            this.fileStream.Write(newFileContentsBytes, 0, newFileContentsBytes.Length);
                            this.fileStream.Flush();
                            Console.WriteLine("[SharedContext] Writing new contents to file");
                        }
                    }
                    else
                    {
                        Console.WriteLine("[SharedContext] Same contents, not writing");
                    }

                    if (this.fileStream != null)
                    {
                        Console.WriteLine("[SharedContext] Closing file");
                        this.fileStream.Close();
                        this.fileStream.Dispose();
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("[SharedContext] Something went wrong on " + this.GetHashCode().ToString());
                    Console.WriteLine("[SharedContext] Error: " + e.Message);
                    Console.WriteLine("[SharedContext] Stack Trace: " + e.StackTrace);
                }

                this.disposed = true;
            }
        }

        ~ContextClientFile()
        {

                this.MyDispose();

        }

        //////////////////////////////////////////////////

        public override string GetResource()
        {
            return this.GetFileName();
        }

        //////////////////////////////////////////////////

        private string GetFileName()
        {
            string anOutputFileName = "";
            string aFolder = "";

            if (this.arguments.ContainsKey("Folder"))
            {
                aFolder = this.arguments["Folder"];
            }

            if (string.IsNullOrEmpty(aFolder))
            {
                aFolder = Path.GetTempPath();
            }

            if (this.fileName == "")
            {
                string aFileName = Environment.UserDomainName + "_" + 
                                   Environment.UserName + "_" + 
                                   this.contextName + ".txt";
                anOutputFileName = Path.Combine(aFolder, aFileName);
            }
            else
            {
                anOutputFileName = this.fileName;
            }
            return anOutputFileName;
        }
    }
}
