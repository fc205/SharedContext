using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace UiPathTeam.SharedContext.Context
{
    public enum contextType
    {
        NamedPipe,
        File
    }

    [Serializable]
    public class ContextContent : Object
    {
        public Dictionary<string, string> GlobalVariables;
        public Dictionary<string, List<ContextMessage>> Messages;

        public ContextContent()
        {
            this.GlobalVariables = new Dictionary<string, string>();
            this.Messages = new Dictionary<string, List<ContextMessage>>();
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
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
