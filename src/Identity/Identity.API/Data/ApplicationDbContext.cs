using Duende.IdentityServer.EntityFramework.Options;
using Duende.IdentityServer.Models;
using Identity.API.Models;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Identity.API.Data
{
    public class ApplicationDbContext : ApiAuthorizationDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,
            IOptions<OperationalStoreOptions> options1) : base(options, options1)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
             {
                 entity.ToTable("Users");
                 entity.Property(x => x.FirstName).HasMaxLength(30);
                 entity.Property(x => x.LastName).HasMaxLength(50);
                 entity.Property(x => x.CreatedTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
             });

            builder.Entity<IdentityRole>(x =>
            {
                x.ToTable("Roles");
            });

            builder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("UserRoles");
            });

            builder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable("UserClaims");
            });

            builder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.ToTable("UserLogins");
            });

            builder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.ToTable("RoleClaims");
            });

            builder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.ToTable("UserTokens");
            });
        }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
    }
}

    



