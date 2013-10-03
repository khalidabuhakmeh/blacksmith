using Blacksmith.Core;
using FluentAssertions;
using Xunit;

namespace Blacksmith.Tests
{
    public class QueueNameExternalConfigurationTests
    {
        [Fact]
        public void Types_should_reflect_configuration_queue_mapping()
        {
            var messageName = typeof (SomeMessage).GetQueueName();

            messageName.ShouldBeEquivalentTo(typeof(SomeMessage).FullName);

            const string queueName = "MyCustomQueue";
            ConfigurationWrapper.QueueNameMappings.Add(typeof(SomeMessage), queueName);

            messageName = typeof (SomeMessage).GetQueueName();
            messageName.ShouldAllBeEquivalentTo(queueName);
        }

        public class SomeMessage
        {
             
        }
    }
}