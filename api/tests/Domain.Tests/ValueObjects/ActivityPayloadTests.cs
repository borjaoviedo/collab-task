using Domain.ValueObjects;
using FluentAssertions;
using System.Text.Json;

namespace Domain.Tests.ValueObjects
{
    public sealed class ActivityPayloadTests
    {
        [Fact]
        public void Create_WithValidJson_ReturnsValueObject()
        {
            var json = " { \"a\": 1 } ";
            var payload = ActivityPayload.Create(json);

            payload.Should().NotBeNull();
            payload.Value.Should().Be("{ \"a\": 1 }");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_EmptyOrWhitespace_Throws(string input)
        {
            Action act = () => ActivityPayload.Create(input);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Create_InvalidJson_Throws()
        {
            var bad = "{ a: 1 ";
            Action act = () => ActivityPayload.Create(bad);
            act.Should().Throw<ArgumentException>().WithInnerException<JsonException>();
        }

        [Fact]
        public void Equality_SameString_IsEqual()
        {
            var a = ActivityPayload.Create("{\"x\":2}");
            var b = ActivityPayload.Create("{\"x\":2}");

            a.Should().Be(b);
            (a == b).Should().BeTrue();
            a.Equals((object)b).Should().BeTrue();
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Equality_DifferentString_IsNotEqual()
        {
            var a = ActivityPayload.Create("{\"x\":2}");
            var b = ActivityPayload.Create("{\"x\":3}");

            a.Should().NotBe(b);
            (a != b).Should().BeTrue();
        }

        [Fact]
        public void Implicit_ToString_Works()
        {
            var a = ActivityPayload.Create("{\"k\":\"v\"}");
            string s = a;
            s.Should().Be("{\"k\":\"v\"}");
            a.ToString().Should().Be(s);
        }
    }
}
