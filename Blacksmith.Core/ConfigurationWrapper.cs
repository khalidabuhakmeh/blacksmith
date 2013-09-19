using System.Configuration;

namespace Blacksmith.Core
{
    public class ConfigurationWrapper
    {
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
    }
}