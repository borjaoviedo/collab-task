using Xunit.Abstractions;
using Xunit.Sdk;

namespace TestHelpers.Common.Testing
{
    // -------------------- Attributes --------------------

    /// <summary>
    /// Marks a test as a unit test (<c>TestType = Unit</c>).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    [TraitDiscoverer(UnitTestTraitDiscoverer.DiscovererTypeName, UnitTestTraitDiscoverer.AssemblyName)]
    public sealed class UnitTestAttribute : Attribute, ITraitAttribute { }

    /// <summary>
    /// Marks a test as an integration test (<c>TestType = Integration</c>).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    [TraitDiscoverer(IntegrationTestTraitDiscoverer.DiscovererTypeName, IntegrationTestTraitDiscoverer.AssemblyName)]
    public sealed class IntegrationTestAttribute : Attribute, ITraitAttribute { }

    /// <summary>
    /// Marks a test as an SQL Server test (<c>TestType = SqlServerContainer</c>).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    [TraitDiscoverer(SqlServerContainerTestTraitDiscoverer.DiscovererTypeName, SqlServerContainerTestTraitDiscoverer.AssemblyName)]
    public sealed class SqlServerContainerTestAttribute : Attribute, ITraitAttribute { }


    // -------------------- Discoverers --------------------

    /// <summary>
    /// Maps <see cref="UnitTestAttribute"/> to trait <c>TestType = Unit</c>.
    /// </summary>
    public sealed class UnitTestTraitDiscoverer : ITraitDiscoverer
    {
        public const string DiscovererTypeName = "TestHelpers.Common.Testing.UnitTestTraitDiscoverer";
        public const string AssemblyName = "TestHelpers";

        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            yield return new KeyValuePair<string, string>("TestType", "Unit");
        }
    }

    /// <summary>
    /// Maps <see cref="IntegrationTestAttribute"/> to trait <c>TestType = Integration</c>.
    /// </summary>
    public sealed class IntegrationTestTraitDiscoverer : ITraitDiscoverer
    {
        public const string DiscovererTypeName = "TestHelpers.Common.Testing.IntegrationTestTraitDiscoverer";
        public const string AssemblyName = "TestHelpers";

        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            yield return new KeyValuePair<string, string>("TestType", "Integration");
        }
    }

    /// <summary>
    /// Maps <see cref="SqlServerContainerTestAttribute"/> to trait <c>TestType = SqlServerContainer</c>.
    /// </summary>
    public sealed class SqlServerContainerTestTraitDiscoverer : ITraitDiscoverer
    {
        public const string DiscovererTypeName = "TestHelpers.Common.Testing.SqlServerContainerTestTraitDiscoverer";
        public const string AssemblyName = "TestHelpers";

        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            yield return new KeyValuePair<string, string>("TestType", "SqlServerContainer");
        }
    }
}
