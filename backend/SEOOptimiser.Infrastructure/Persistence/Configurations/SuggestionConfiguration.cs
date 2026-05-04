using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SEOOptimiser.Core.Entities;

namespace SEOOptimiser.Infrastructure.Persistence.Configurations;

public class SuggestionConfiguration : IEntityTypeConfiguration<Suggestion>
{
    public void Configure(EntityTypeBuilder<Suggestion> builder)
    {
        builder.ToTable("chat_suggestions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.MessageId).IsRequired();
        builder.Property(s => s.Tag).IsRequired().HasMaxLength(64);
        builder.Property(s => s.CurrentValue).HasColumnType("text");
        builder.Property(s => s.SuggestedValue).IsRequired().HasColumnType("text");

        builder.HasIndex(s => s.MessageId);

        builder.HasOne(s => s.Message)
               .WithMany(m => m.Suggestions)
               .HasForeignKey(s => s.MessageId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
