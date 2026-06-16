using MidR.Abstractions;
using System;
using Xunit;

namespace MidR.UnitTests
{
    public class RoutingKeyTests
    {
        [Fact]
        public void Equals_SameValue_AreEqual()
        {
            RoutingKey a = "orders";
            RoutingKey b = new RoutingKey("orders");

            Assert.True(a == b);
            Assert.True(a.Equals(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Equals_DifferentValue_AreNotEqual()
        {
            RoutingKey a = "orders";
            RoutingKey b = "payments";

            Assert.True(a != b);
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_IsCaseSensitive()
        {
            RoutingKey a = "Orders";
            RoutingKey b = "orders";

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void ImplicitConversion_FromString_PreservesValue()
        {
            RoutingKey key = "orders";

            Assert.Equal("orders", key.Value);
            Assert.Equal("orders", key.ToString());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_NullOrWhitespace_Throws(string? value)
        {
            Assert.Throws<ArgumentException>(() => new RoutingKey(value!));
        }
    }
}
