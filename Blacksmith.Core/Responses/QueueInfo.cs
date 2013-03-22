using Newtonsoft.Json;

namespace Blacksmith.Core.Responses
{
    public class QueueInfo
    {
        [JsonProperty("size")]
        public int Size { get; set; }
    }
}
