using System.Configuration;

namespace Blacksmith.Core
{
    public class ConfigurationWrapper
    {
        public virtual string blacksmithprojectId
        {
            get { return ConfigurationManager.AppSettings.GetValueOrDefault("blacksmith.projectId", ""); }
        }

        public virtual string blacksmithtoken
        {
            get { return ConfigurationManager.AppSettings.GetValueOrDefault("blacksmith.token", ""); }
        }


        public virtual string OptionalFixedQueueName
        {
            get { return ConfigurationManager.AppSettings.GetValueOrDefault("blacksmith.optional.fixed.queuename", ""); }
        }
    }
}