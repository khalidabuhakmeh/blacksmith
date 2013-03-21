using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Blacksmith.Core.Responses
{
    [Serializable]
    [JsonObject]
    public class Message
    {
        public Message()
        { }

        public Message(Message message)
        {
            Id = message.Id;
            Body = message.Body;
            Timeout = message.Timeout;
            ExpiresIn = message.Timeout;
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [DefaultValue(0)]
        [JsonProperty("timeout")]
        public long Timeout { get; set; }

        [DefaultValue(0)]
        [JsonProperty("delay")]
        public long Delay { get; set; }

        [JsonProperty("expires_in")]
        [DefaultValue(0)]
        public long ExpiresIn { get; set; }
    }

    public class Message<T> : Message
    {
        public Message(Message message)
            :base(message)
        {}

        [JsonIgnore]
        public T Target
        {
            get { return JsonConvert.DeserializeObject<T>(Body); }
        }
    }
}