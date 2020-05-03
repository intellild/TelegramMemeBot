using Microsoft.EntityFrameworkCore;
using Telegram.Synthetic.Dawn.Models;

namespace Telegram.Synthetic.Dawn
{
    public class MemeContext : DbContext
    {
        public DbSet<UserAlias> UserAliases;

        public MemeContext(DbContextOptions<MemeContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<UserAlias>().HasIndex(x => x.Alias);
            modelBuilder.Entity<UserAlias>().HasIndex(x => x.UserId);
            modelBuilder.Entity<Meme>().HasIndex(x => x.Alias);
            modelBuilder.Entity<MemeAlias>().HasIndex(x => x.MemeId);
            modelBuilder.Entity<MemeAlias>().HasIndex(x => x.UserId);
        }
    }
}