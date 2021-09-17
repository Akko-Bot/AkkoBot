using AkkoCore.Services.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AkkoCore.Services.Database.Config
{
    /// <summary>
    /// Configures relationships for <see cref="BlacklistEntity"/>.
    /// </summary>
    public class BlacklistEntityTypeConfiguration : IEntityTypeConfiguration<BlacklistEntity>
    {
        public void Configure(EntityTypeBuilder<BlacklistEntity> builder)
            => builder.HasAlternateKey(g => g.ContextId);
    }
}
