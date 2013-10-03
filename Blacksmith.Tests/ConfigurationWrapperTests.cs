using Blacksmith.Core;
using FluentAssertions;
using Xunit;

namespace Blacksmith.Tests
{
    public class ConfigurationWrapperTests
    {
        [Fact]
        public void ProjectID_Always_Returns_A_Non_Null()
        {
            var config = new ConfigurationWrapper();
            config.BlacksmithProjectId.Should().NotBe(null);
        }

        [Fact]
        public void Token_Always_Returns_A_Non_Null()
        {
            var config = new ConfigurationWrapper();
            config.BlacksmithToken.Should().NotBe(null);
        }

        [Fact]
        public void Optional_fixed_qeuename_should_not_return_null()
        {
            var config = new ConfigurationWrapper();
            config.OptionalFixedQueueName.Should().NotBe(null);
        }

        [Fact]
        public void Queue_name_should_not_be_null()
        {
            ConfigurationWrapper.QueueNameMappings.Should().NotBeNull();
        }
    }
}