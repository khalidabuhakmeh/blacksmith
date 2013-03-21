using System.Collections.Specialized;
using Blacksmith.Core;
using FluentAssertions;
using Xunit;

namespace Blacksmith.Tests
{
    public class BlacksmithExtensionMethodTests
    {
        [Fact]
        public void Can_get_value_from_settings()
        {
            var settings = new NameValueCollection { { "butter", "300" } };
            var result = settings.GetValueOrDefault("butter", 1);
            result.Should().Be(300);
        }

        [Fact]
        public void Can_get_default_if_settings_doesnt_exist()
        {
            var settings = new NameValueCollection();
            var result = settings.GetValueOrDefault("butter", 1);
            result.Should().Be(1);
        }

        [Fact]
        public void Can_iterate_over_a_collection()
        {
            var items = new[] { 1, 1, 1, 1, 1 };
            var sum = 0;
            items.ForEach(x => sum = sum + x );
            sum.Should().Be(5);
        }
    }
}
