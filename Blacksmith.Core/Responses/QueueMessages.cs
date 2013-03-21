using System;
using Newtonsoft.Json;

namespace Blacksmith.Core.Responses
{
    [Serializable]
    public class QueueMessages
    {
        [JsonProperty("messages")]
        public Message[] Messages { get; set; }
    }

   
}