using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserService.Domain.Entities;
using UserService.Domain.ValueObjects;

namespace UserService.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedNever();

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.IsActive)
            .IsRequired();

        builder.Property(u => u.Role)
            .IsRequired();

        // Value Object: Email
        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value)
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnName("Email");
                
            email.HasIndex(e => e.Value).IsUnique();
        });

        // Ignore domain events (not persisted)
        builder.Ignore(u => u.DomainEvents);

        builder.ToTable("Users");
    }
}
