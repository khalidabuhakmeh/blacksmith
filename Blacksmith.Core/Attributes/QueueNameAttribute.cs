using System;

namespace Blacksmith.Core.Attributes
{
    public class QueueNameAttribute : Attribute
    {
        public string Name { get; set; }

        public QueueNameAttribute(string name)
        {
            Name = name;
        }
    }
}
