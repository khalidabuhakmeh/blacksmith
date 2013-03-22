using System;
using Newtonsoft.Json;

namespace Blacksmith.Core.Responses
{
    [Serializable]
    public class Subscriptions
    {
        public Subscriptions()
        {
            Subscribers = new Subscriber[0];
        }

        [JsonProperty("subscribers")]
        public Subscriber[] Subscribers { get; set; }
    }

    [Serializable]
    public class Subscriber
    {
        public Subscriber()
        {}

        public Subscriber(string url)
        {
            Url = url;
        }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    [Serializable]
    public class Subcsription
    {
        public Subcsription()
        {
            Subscribers = new Subscriber[0];
        }

        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("size")]
        public int Size { get; set; }
        [JsonProperty("total_messages")]
        public int TotalMessages { get; set; }
        [JsonProperty("project_id")]
        public string ProjectId { get; set; }
        [JsonProperty("retries")]
        public int Retries { get; set; }
        [JsonProperty("push_type")]
        public string PushType { get; set; }
        [JsonProperty("retries_delay")]
        public int RetriesDelay { get; set; }
        [JsonProperty("subscribers")]
        public Subscriber[] Subscribers { get; set; }
    }
}