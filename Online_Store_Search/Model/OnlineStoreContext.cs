using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Online_Store_Search.Model
{

    public class OnlineStoreContext : DbContext
    {
        public OnlineStoreContext() : base(GetOptions("OnlineStoreContext"))
        {
        }

        private static DbContextOptions GetOptions(string connectionString)
        {
            return SqlServerDbContextOptionsExtensions.UseSqlServer(new DbContextOptionsBuilder(), connectionString).Options;
        }

        public DbSet<Car> Cars { get; set; }
        public DbSet<Laptop> Laptops { get; set; }
        public DbSet<TV> TVs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(Properties.Settings.Default.OnlineStoreContext);
        }
    }




}
