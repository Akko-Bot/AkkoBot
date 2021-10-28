using AkkoCore.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkkoCore.Services.Database.Config
{
    /// <summary>
    /// Configures relationships for <see cref="TimerEntity"/>.
    /// </summary>
    public sealed class TimerEntityTypeConfiguration : IEntityTypeConfiguration<TimerEntity>
    {
        public void Configure(EntityTypeBuilder<TimerEntity> builder)
        {
            // Timer -> Repeater
            builder.HasOne<RepeaterEntity>()
                .WithOne(x => x.TimerRel!)
                .HasForeignKey<RepeaterEntity>(x => x.TimerIdFK)
                .HasPrincipalKey<TimerEntity>(x => x.Id)
                .OnDelete(DeleteBehavior.Cascade);

            // Timer -> Reminder
            builder.HasOne<ReminderEntity>()
                .WithOne(x => x.TimerRel!)
                .HasForeignKey<ReminderEntity>(x => x.TimerIdFK)
                .HasPrincipalKey<TimerEntity>(x => x.Id)
                .OnDelete(DeleteBehavior.Cascade);

            // Timer -> Autocommand
            builder.HasOne<AutoCommandEntity>()
                .WithOne(x => x.TimerRel!)
                .HasForeignKey<AutoCommandEntity>(x => x.TimerIdFK)
                .HasPrincipalKey<TimerEntity>(x => x.Id)
                .OnDelete(DeleteBehavior.Cascade);

            // Timer -> Infraction
            builder.HasOne(x => x.WarnRel)
                .WithOne(x => x!.TimerRel!)
                .HasForeignKey<WarnEntity>(x => x.TimerIdFK)
                .HasPrincipalKey<TimerEntity>(x => x.Id)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}