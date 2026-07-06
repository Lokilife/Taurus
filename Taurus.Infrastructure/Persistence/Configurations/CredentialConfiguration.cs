namespace Taurus.Infrastructure.Persistence.Configurations;

public sealed class CredentialConfiguration : IEntityTypeConfiguration<CredentialEntity>
{
    public void Configure(EntityTypeBuilder<CredentialEntity> builder)
    {
        builder.ToTable("Credentials");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId).IsRequired();
        builder.Property(c => c.Type).IsRequired();
        builder.Property(c => c.Value).IsRequired().HasMaxLength(255);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        builder.HasOne(c => c.User)
            .WithMany(u => u.Credentials)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
