using System.ComponentModel;
using Newtonsoft.Json;

namespace Blacksmith.Core.Responses
{
    public class QueueUpdate
    {
        public QueueUpdate()
        {
            Subscribers = new Subscriber[0];
        }

        [JsonProperty("subscribers")]
        public Subscriber[] Subscribers { get; set; }

        [DefaultValue(3)]
        [JsonProperty("retries", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Retries { get; set; }

        [DefaultValue(60)]
        [JsonProperty("retries_delay", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int RetriesDelay { get; set; }

        [DefaultValue("multicast")]
        [JsonProperty("push_type", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PushType { get; set; }
    }

    public class QueueSettings
    {
        public QueueSettings()
        {
            Subscribers = new Subscriber[0];
        }

        [JsonProperty("subscribers")]
        public Subscriber[] Subscribers { get; set; }

        [DefaultValue(3)]
        [JsonProperty("retries")]
        public int Retries { get; set; }

        [DefaultValue("multicast")]
        [JsonProperty("push_type")]
        public string PushType { get; set; }

        [DefaultValue(60)]
        [JsonProperty("retries_delay")]
        public int RetriesDelay { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("total_messages")]
        public int TotalMessages { get; set; }
    }
}
