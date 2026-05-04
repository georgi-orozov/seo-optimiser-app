using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEOOptimiser.Core.Entities;

namespace SEOOptimiser.Infrastructure.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.SessionId).IsRequired();
        builder.Property(m => m.Role).IsRequired().HasConversion<string>();
        builder.Property(m => m.Content).IsRequired().HasColumnType("text");
        builder.Property(m => m.CreatedAt).IsRequired();

        builder.HasIndex(m => m.SessionId);
        builder.HasIndex(m => new { m.SessionId, m.CreatedAt });
    }
}
