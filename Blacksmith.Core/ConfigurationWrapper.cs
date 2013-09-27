using System.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Blacksmith.Core
{
    public class ConfigurationWrapper
    {
        /// <summary>
        /// Initializes the <see cref="ConfigurationWrapper"/> class.
        /// </summary>
        static ConfigurationWrapper()
        {
            // default configuration for the json serializer (for better non C# compatibility)
            JsonSettings = new JsonSerializerSettings {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            JsonSettings.Converters.Add(new ExpandoObjectConverter());
            JsonSettings.Converters.Add(new IsoDateTimeConverter());
            JsonSettings.Converters.Add(new StringEnumConverter());
        }

        public virtual string BlacksmithProjectId
        {
            get { return ConfigurationManager.AppSettings.GetValueOrDefault("blacksmith.projectId", ""); }
        }

        public virtual string BlacksmithToken
        {
            get { return ConfigurationManager.AppSettings.GetValueOrDefault("blacksmith.token", ""); }
        }


        public virtual string OptionalFixedQueueName
        {
            get { return ConfigurationManager.AppSettings.GetValueOrDefault("blacksmith.optional.fixed.queuename", ""); }
        }

        /// <summary>
        /// Gets or sets the json settings.
        /// </summary>
        /// <value>
        /// The json settings.
        /// </value>
        public static JsonSerializerSettings JsonSettings { get; set; }
    }
}