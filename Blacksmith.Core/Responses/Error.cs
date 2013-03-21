using System;
using Newtonsoft.Json;

namespace Blacksmith.Core.Responses
{
    [Serializable]
    public class Error
    {
        [JsonProperty("msg")]
        public string Message { get; set; }
    }
}
