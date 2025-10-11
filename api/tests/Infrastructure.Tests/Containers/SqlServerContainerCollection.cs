
namespace Infrastructure.Tests.Containers
{
    [CollectionDefinition("SqlServerContainer")]
    public sealed class SqlServerContainerCollection : ICollectionFixture<MsSqlContainerFixture> { }
}
