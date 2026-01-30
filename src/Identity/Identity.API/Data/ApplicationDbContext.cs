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
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IOptions<OperationalStoreOptions> operationalStoreOptions)
            : base(options, operationalStoreOptions)
        {
        }

        public DbSet<Tokens> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Customize ASP.NET Identity table names and properties
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Users");

                entity.Property(e => e.FirstName)
                    .HasMaxLength(100);

                entity.Property(e => e.LastName)
                    .HasMaxLength(100);

                entity.Property(e => e.CreatedTime)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdateTime);

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.HasIndex(e => e.Email)
                    .IsUnique();

                entity.HasIndex(e => e.UserName)
                    .IsUnique();
            });

            builder.Entity<IdentityRole>(entity =>
            {
                entity.ToTable("Roles");
            });

            builder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("UserRoles");

                entity.HasKey(ur => new { ur.UserId, ur.RoleId });

                entity.HasOne<IdentityRole>()
                    .WithMany()
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();

                entity.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();
            });

            builder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable("UserClaims");

                entity.Property(uc => uc.ClaimType)
                    .HasMaxLength(256);

                entity.Property(uc => uc.ClaimValue)
                    .HasMaxLength(256);
            });

            builder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.ToTable("UserLogins");

                entity.HasKey(ul => new { ul.LoginProvider, ul.ProviderKey });
            });

            builder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.ToTable("RoleClaims");

                entity.Property(rc => rc.ClaimType)
                    .HasMaxLength(256);

                entity.Property(rc => rc.ClaimValue)
                    .HasMaxLength(256);
            });

            builder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.ToTable("UserTokens");

                entity.HasKey(ut => new { ut.UserId, ut.LoginProvider, ut.Name });
            });

            // RefreshToken configuration
            builder.Entity<Tokens>(entity =>
            {
                entity.ToTable("RefreshTokens");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Token)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedByIp)
                    .HasMaxLength(50);

                entity.Property(e => e.RevokedByIp)
                    .HasMaxLength(50);

                entity.Property(e => e.ReplacedByToken)
                    .HasMaxLength(500);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}






