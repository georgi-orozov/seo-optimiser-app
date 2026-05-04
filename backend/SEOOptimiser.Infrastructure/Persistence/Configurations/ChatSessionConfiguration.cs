using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEOOptimiser.Core.Entities;

namespace SEOOptimiser.Infrastructure.Persistence.Configurations;

public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.ToTable("chat_sessions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.UserId).IsRequired().HasMaxLength(128);
        builder.Property(s => s.Title).IsRequired().HasMaxLength(256);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.HasIndex(s => s.UserId);

        builder.HasMany(s => s.Messages)
               .WithOne(m => m.Session)
               .HasForeignKey(m => m.SessionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
