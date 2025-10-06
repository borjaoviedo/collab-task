using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Tests.Common
{
    public static class ServiceTestHelpers
    {
        public static (ServiceProvider sp, AppDbContext db) BuildScope(string cs)
        {
            var sc = new ServiceCollection();
            sc.AddInfrastructure(cs);
            sc.AddApplication();

            var sp = sc.BuildServiceProvider();
            var db = sp.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
            return (sp, db);
        }

        public static byte[] Bytes(int n, byte fill = 0x5A) =>
            Enumerable.Repeat(fill, n).ToArray();
    }
}
