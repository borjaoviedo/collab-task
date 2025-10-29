using Infrastructure;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace TestHelpers.Persistence
{
    public static class DbHelper
    {
        public static (ServiceProvider sp, AppDbContext db) BuildDb(string fullCs)
        {
            var sc = new ServiceCollection();
            sc.AddInfrastructure(fullCs);
            var sp = sc.BuildServiceProvider();
            return (sp, sp.GetRequiredService<AppDbContext>());
        }
    }
}
