using System;
using System.Collections.Generic;
using System.Text;

using FinFlow.Orion.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinFlow.Orion.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(512);

        builder.HasIndex(rt => rt.Token)
            .IsUnique();

        builder.Property(rt => rt.IsRevoked)
            .HasDefaultValue(false);
    }
}