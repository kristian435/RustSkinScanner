using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace RustSkinScanner.Model
{
    public class SovellusDbContext : DbContext
    {
        public DbSet<SkinItem> SkinItems { get; set; }
        public DbSet<ProfitDeal> ProfitDeals { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skins.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }
}
