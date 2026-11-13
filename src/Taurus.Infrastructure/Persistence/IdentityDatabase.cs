using Microsoft.EntityFrameworkCore;
using Taurus.Core.Auditing;
using Taurus.Core.Authentication.Policies;
using Taurus.Core.Credentials;
using Taurus.Core.Groups;
using Taurus.Core.Sessions;
using Taurus.Core.Users;
using Taurus.Core.Users.Attributes;
using Taurus.Infrastructure.Persistence.Schemas;

namespace Taurus.Infrastructure.Persistence;

public class IdentityDatabase(DbContextOptions options)
    : DbContext(options)
{
    private const int IdentifierLength = 128;
    private const int DigestLength = 64;
    private const string RootFilter = "\"Root\" = true";

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<CredentialEntity> Credentials => Set<CredentialEntity>();
    public DbSet<SessionEntity> Sessions => Set<SessionEntity>();
    public DbSet<UserLogEntity> UserLogs => Set<UserLogEntity>();
    public DbSet<UserAttributeEntity> Attributes => Set<UserAttributeEntity>();
    public DbSet<SchemaSnapshot> Schemas => Set<SchemaSnapshot>();
    public DbSet<AdminAuthenticationPolicyEntity> AdminPolicies =>
        Set<AdminAuthenticationPolicyEntity>();
    public DbSet<UserAuthenticationPolicyEntity> UserPolicies =>
        Set<UserAuthenticationPolicyEntity>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<UserEntity>().HasKey(x => new { x.TenantId, x.Id });
        model.Entity<UserEntity>().HasIndex(x => new { x.TenantId, x.Username })
            .IsUnique();
        model.Entity<UserEntity>().Property(x => x.Version).IsConcurrencyToken();
        model.Entity<UserEntity>().HasIndex(x => x.TenantId).IsUnique()
            .HasFilter(RootFilter);
        model.Entity<CredentialEntity>().HasKey(x => new { x.TenantId, x.Id });
        model.Entity<CredentialEntity>().HasIndex(x => new { x.TenantId, x.UserId });
        model.Entity<CredentialEntity>().HasOne<UserEntity>().WithMany()
            .HasForeignKey(x => new { x.TenantId, x.UserId });
        model.Entity<SessionEntity>().HasKey(x => new { x.TenantId, x.TokenDigest });
        model.Entity<SessionEntity>().HasIndex(x => new { x.TenantId, x.Id })
            .IsUnique();
        model.Entity<SessionEntity>().Property(x => x.TokenDigest)
            .HasMaxLength(DigestLength);
        model.Entity<SessionEntity>().HasOne<UserEntity>().WithMany()
            .HasForeignKey(x => new { x.TenantId, x.UserId });
        model.Entity<GroupEntity>().HasKey(x => new { x.TenantId, x.Id });
        model.Entity<GroupEntity>().HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique();
        model.Entity<GroupMembershipEntity>()
            .HasKey(x => new { x.TenantId, x.GroupId, x.UserId });
        model.Entity<GroupMembershipEntity>().HasOne<UserEntity>().WithMany()
            .HasForeignKey(x => new { x.TenantId, x.UserId });
        model.Entity<GroupMembershipEntity>().HasOne<GroupEntity>().WithMany()
            .HasForeignKey(x => new { x.TenantId, x.GroupId });
        model.Entity<UserAttributeEntity>()
            .HasKey(x => new { x.TenantId, x.UserId, x.Name, x.Position });
        model.Entity<UserAttributeEntity>().HasOne<UserEntity>().WithMany()
            .HasForeignKey(x => new { x.TenantId, x.UserId });
        model.Entity<UserLogEntity>().Property(x => x.Action).HasConversion<string>();
        model.Entity<UserLogEntity>().Property(x => x.Severity)
            .HasDefaultValue(UserLogSeverity.High).ValueGeneratedNever();
        model.Entity<UserLogEntity>().HasKey(x => new { x.TenantId, x.Id });
        model.Entity<UserLogEntity>().HasIndex(x => new { x.TenantId, x.Timestamp });
        model.Entity<SchemaSnapshot>().HasKey(x => x.TenantId);
        model.Entity<SchemaSnapshot>().Property(x => x.Version).IsConcurrencyToken();
        model.Entity<AdminAuthenticationPolicyEntity>().HasKey(x => x.TenantId);
        model.Entity<UserAuthenticationPolicyEntity>()
            .HasKey(x => new { x.TenantId, x.UserId });
        model.Entity<UserAuthenticationPolicyEntity>().HasOne<UserEntity>().WithMany()
            .HasForeignKey(x => new { x.TenantId, x.UserId });

        foreach (var entity in model.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.Name == nameof(UserEntity.TenantId))
                    property.SetMaxLength(IdentifierLength);

                if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetValueConverter(
                        new Microsoft.EntityFrameworkCore.Storage.ValueConversion
                            .ValueConverter<DateTimeOffset, long>(
                                value => value.UtcTicks,
                                value => new DateTimeOffset(value, TimeSpan.Zero)));
                }
            }
        }
    }
}
