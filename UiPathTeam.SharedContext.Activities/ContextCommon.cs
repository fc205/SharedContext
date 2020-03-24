using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPathTeam.SharedContext.Activities
{
    public enum contextType
    {
        File,
        NamedPipe
    }

    [Serializable]
    public class ContextContent : Object
    {
        public Dictionary<string, string> GlobalVariables;
        public Dictionary<string, List<ContextMessage>> Messages;
        public bool TakeLock;

        public ContextContent()
        {
            this.TakeLock = false;
            this.GlobalVariables = new Dictionary<string, string>();
            this.Messages = new Dictionary<string, List<ContextMessage>>();
        }
    }

    [Serializable]
    public struct ContextMessage
    {
        public string From;
        public string To;
        public string Action;
        public string ArgumentsJson;
        public DateTime DateSent;
    }
}
