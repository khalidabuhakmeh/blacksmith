using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Blacksmith.Core.Attributes;

namespace Blacksmith.Core
{
    public static class BlacksmithExtensionMethods
    {
        /// <summary>
        /// Convenience method around foreach
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="action"></param>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T element in source)
                action(element);
        }

        /// <summary>
        /// A safe way to get values from a name value collection. i.e. AppSettings
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="settings"></param>
        /// <param name="key"></param>
        /// <param name="default"></param>
        /// <returns></returns>
        public static T GetValueOrDefault<T>(this NameValueCollection settings, string key, T @default)
        {
            if (!settings.AllKeys.Any(x => x == key))
                return @default;

            var value = settings[key];
            return (T)Convert.ChangeType(value, typeof(T));
        }

        /// <summary>
        /// Can tell you what the preferred queue name is for a particular type. Helpful for debugging queue issues.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetQueueName(this Type type)
        {
            var config = new ConfigurationWrapper();
            
            if (string.IsNullOrEmpty(config.OptionalFixedQueueName))
            {
                var attributes = type.GetCustomAttributes(typeof(QueueNameAttribute), false);

                if (!attributes.Any())
                    return type.FullName;

                var attribute = attributes.Cast<QueueNameAttribute>().First();
                return attribute.Name;
            }
            else
            {
                return config.OptionalFixedQueueName;
            }
        }
    }

    
}
