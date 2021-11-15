using AkkoCore.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkkoCore.Services.Database.Config;

/// <summary>
/// Configures relationships for <see cref="GuildConfigEntity"/>.
/// </summary>
public sealed class GuildConfigEntityTypeConfiguration : IEntityTypeConfiguration<GuildConfigEntity>
{
    public void Configure(EntityTypeBuilder<GuildConfigEntity> builder)
    {
        builder.HasAlternateKey(x => x.GuildId);

        // Guild -> Aliases
        builder.HasMany(x => x.AliasRel)
            .WithOne(x => x.GuildConfigRel!)
            .HasForeignKey(x => x.GuildIdFK)
            .HasPrincipalKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Guild -> Command Cooldown
        builder.HasMany(x => x.CommandCooldownRel)
            .WithOne(x => x.GuildConfigRel!)
            .HasForeignKey(x => x.GuildIdFK)
            .HasPrincipalKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Guild -> Muted User
        builder.HasMany(x => x.MutedUserRel)
            .WithOne(x => x.GuildConfigRel!)
            .HasForeignKey(x => x.GuildIdFK)
            .HasPrincipalKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Guild -> Infractions
        builder.HasMany(x => x.WarnRel)
            .WithOne(x => x.GuildConfigRel!)
            .HasForeignKey(x => x.GuildIdFK)
            .HasPrincipalKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Guild -> Punishments
        builder.HasMany(x => x.WarnPunishRel)
            .WithOne(x => x.GuildConfigRel!)
            .HasForeignKey(x => x.GuildIdFK)
            .HasPrincipalKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Guild -> Occurrences
        builder.HasMany(x => x.OccurrenceRel)
            .WithOne(x => x.GuildConfigRel!)
            .HasForeignKey(x => x.GuildIdFK)
            .HasPrincipalKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Guild -> Filtered Words
        builder.HasOne(x => x.FilteredWordsRel)
            .WithOne(x => x!.GuildConfigRel!)
            .HasForeignKey<FilteredWordsEntity>(x => x.GuildIdFK)
            .HasPrincipalKey<GuildConfigEntity>(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Guild -> Filtered Content
        builder.HasMany(x => x.FilteredContentRel)
            .WithOne(x => x.GuildConfigRel!)
            .HasForeignKey(x => x.GuildIdFK)
            .HasPrincipalKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Guild -> Voice Roles
        builder.HasMany(x => x.VoiceRolesRel)
            .WithOne(x => x.GuildConfigRel!)
            .HasForeignKey(x => x.GuildIdFk)
            .HasPrincipalKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Guild -> Polls
        builder.HasMany(x => x.PollRel)
            .WithOne(x => x.GuildConfigRel!)
            .HasForeignKey(x => x.GuildIdFK)
            .HasPrincipalKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Guild -> Repeaters
        builder.HasMany(x => x.RepeaterRel)
            .WithOne(x => x.GuildConfigRel!)
            .HasForeignKey(x => x.GuildIdFK)
            .HasPrincipalKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Guild -> Timers
        builder.HasMany(x => x.TimerRel)
            .WithOne(x => x.GuildConfigRel!)
            .HasForeignKey(x => x.GuildIdFK)
            .HasPrincipalKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Guild -> Gatekeeping
        builder.HasOne(x => x.GatekeepRel)
            .WithOne(x => x!.GuildConfigRel!)
            .HasForeignKey<GatekeepEntity>(x => x.GuildIdFK)
            .HasPrincipalKey<GuildConfigEntity>(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Guild -> Autoslowmode
        builder.HasOne(x => x.AutoSlowmodeRel)
            .WithOne(x => x!.GuildConfigRel!)
            .HasForeignKey<AutoSlowmodeEntity>(x => x.GuildIdFK)
            .HasPrincipalKey<GuildConfigEntity>(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Guild -> LogSettings
        builder.HasMany(x => x.GuildLogsRel)
            .WithOne(x => x.GuildConfigRel!)
            .HasForeignKey(x => x.GuildIdFK)
            .HasPrincipalKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Guild -> Tags
        builder.HasMany(x => x.TagsRel)
            .WithOne(x => x.GuildConfigRel!)
            .HasForeignKey(x => x.GuildIdFK)
            .HasPrincipalKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        // Guild -> Command Permission Override
        builder.HasMany(x => x.PermissionOverrideRel)
            .WithOne(x => x.GuildConfigRel!)
            .HasForeignKey(x => x.GuildIdFK)
            .HasPrincipalKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}