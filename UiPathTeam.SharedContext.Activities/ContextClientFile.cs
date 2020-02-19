using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPathTeam.SharedContext.Activities
{
    public class ContextClientFile : IContextClient
    {
        private string fileName;

        private FileStream fileStream;

        private Dictionary<string, string> deserialisedFileContents;
        private string originalFileContents;

        public ContextClientFile(string iContextName, Dictionary<string, string> iArguments)
        {
            this.fileName = "";
            this.contextName = iContextName;
            this.arguments = iArguments;

            int retriesAttempted = 0;
            int retriesMax = int.Parse(this.arguments["Retries"]);
            if (!this.arguments.ContainsKey("Lock"))
            {
                this.arguments["Lock"] = "N";
            }
            bool doLock = this.arguments["Lock"]=="Y"?true:false;

            while (retriesAttempted < retriesMax)
            {
                try
                {
                    if(doLock)
                    {
                        this.fileStream = new FileStream(this.GetFileName(),
                                                        FileMode.OpenOrCreate,
                                                        FileAccess.ReadWrite,
                                                        FileShare.None
                                                        );
                    }
                    else
                    {
                        this.fileStream = new FileStream(this.GetFileName(),
                                                        FileMode.OpenOrCreate,
                                                        FileAccess.ReadWrite,
                                                        FileShare.ReadWrite
                                                        );
                    }
                }
                catch (Exception e)
                {
                    this.fileStream = null;
                }

                if (this.fileStream != null) break;

                Console.WriteLine("FileStream conflict with " +
                                    this.GetFileName() +
                                    " @ " +
                                    DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt") +
                                    ". I will retry shortly.");

                string nowString = DateTime.Now.ToFileTime().ToString();
                var aRandom = new System.Random(int.Parse(nowString.Substring(nowString.Length - 9, 9)));
                System.Threading.Thread.Sleep(aRandom.Next(0, 500));

                retriesAttempted++;
            }

            if (this.fileStream != null)
            {
                // Read the contents and put them in the memory variable

                StreamReader sr = new StreamReader(this.fileStream);
                this.originalFileContents = sr.ReadToEnd();

                try
                {
                    this.deserialisedFileContents = JsonConvert.DeserializeObject<Dictionary<string, string>>(this.originalFileContents);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error deserializing the file. Emptying the file. > " + 
                                                DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fffff tt") + 
                                                " > " + 
                                                e.Message);
                    this.deserialisedFileContents = new Dictionary<string, string>();
                }

                if (this.deserialisedFileContents == null)
                {
                    this.deserialisedFileContents = new Dictionary<string, string>();
                }

            }
            else
            {
                throw new Exception("Could not open FileStream within the retries.");
            }
        }

        public override void Dispose()
        {
            string newFileContents = "";

            newFileContents = JsonConvert.SerializeObject(this.deserialisedFileContents);

            try
            {

                if (newFileContents != this.originalFileContents)
                {
                    if (this.fileStream != null)
                    {
                        this.fileStream.Position = 0;
                        Byte[] newFileContentsBytes = Encoding.UTF8.GetBytes(newFileContents);
                        this.fileStream.SetLength(newFileContentsBytes.Length);
                        this.fileStream.Write(newFileContentsBytes, 0, newFileContentsBytes.Length);
                        this.fileStream.Flush();
                    }
                }

                if (this.fileStream != null)
                {
                    this.fileStream.Close();
                    this.fileStream.Dispose();
                }
            }
            catch(Exception e)
            {

            }
        }

        ~ContextClientFile()
        {
            if (this.fileStream != null)
            {
                this.Dispose();
            }
        }

        //////////////////////////////////////////////////

        public override void Set(string iVariableName, string iVariableValue)
        {
            this.deserialisedFileContents[iVariableName] = iVariableValue;
        }

        public override string Get(string iVariableName, bool iRaiseException)
        {
            if(this.deserialisedFileContents.ContainsKey(iVariableName))
            {
                return this.deserialisedFileContents[iVariableName];
            }

            string errorMessage = "Variable Name " + iVariableName + " does not exist in context " + this.contextName;

            if (iRaiseException)
            {
                throw new Exception(errorMessage);
            }
            Console.Error.WriteLine(errorMessage);
            return "";
        }

        public override void ClearAll()
        {
            this.deserialisedFileContents = new Dictionary<string, string>();
        }

        public override string GetResource()
        {
            return this.GetFileName();
        }

        //////////////////////////////////////////////////

        public string GetFileName(int iNumber = 0)
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
                if(iNumber <= 0)
                {
                    anOutputFileName = Path.Combine(aFolder, this.contextName + ".txt");
                }
                else
                {
                    anOutputFileName = Path.Combine(aFolder, this.contextName + iNumber.ToString() + ".txt");
                }
            }
            else
            {
                anOutputFileName = this.fileName;
            }
            return anOutputFileName;
        }
    }
}
