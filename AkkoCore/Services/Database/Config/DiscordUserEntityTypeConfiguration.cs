using AkkoCore.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkkoCore.Services.Database.Config;

/// <summary>
/// Configures relationships for <see cref="DiscordUserEntity"/>.
/// </summary>
public sealed class DiscordUserEntityTypeConfiguration : IEntityTypeConfiguration<DiscordUserEntity>
{
    public void Configure(EntityTypeBuilder<DiscordUserEntity> builder)
    {
        builder.HasAlternateKey(x => x.UserId);

        // User -> Timers
        builder.HasMany(x => x.TimerRel)
            .WithOne(x => x.UserRel!)
            .HasForeignKey(x => x.UserIdFK)
            .HasPrincipalKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // User -> Warnings
        builder.HasMany(x => x.WarnRel)
            .WithOne(x => x.UserRel!)
            .HasForeignKey(x => x.UserIdFK)
            .HasPrincipalKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}