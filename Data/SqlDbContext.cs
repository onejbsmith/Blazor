using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace BlazorTrader.Data
{
    public class SqlDbContext : DbContext
    {
        public SqlDbContext(DbContextOptions<SqlDbContext> options)
           : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TDAOptionQuote>()
                .HasKey(c => new { c.symbol, c.quoteTimeInLong });
        }
        public DbSet<TDAOptionQuote> TDAOptionQuotes { get; set; }
    }
}
