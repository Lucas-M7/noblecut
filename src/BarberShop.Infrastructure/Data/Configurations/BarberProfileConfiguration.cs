using BarberShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarberShop.Infrastructure.Data.Configurations;

public class BarberProfileConfiguration : IEntityTypeConfiguration<BarberProfile>
{
    public void Configure(EntityTypeBuilder<BarberProfile> builder)
    {
        builder.ToTable("barber_profiles");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(b => b.UserId).HasColumnName("user_id");
        builder.Property(b => b.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
        builder.Property(b => b.BusinessName).HasColumnName("business_name").HasMaxLength(150).IsRequired();
        builder.Property(b => b.Phone).HasColumnName("phone").HasMaxLength(20);
        builder.Property(b => b.Slug).HasColumnName("slug").HasMaxLength(100).IsRequired();
        builder.Property(b => b.PhotoUrl).HasColumnName("photo_url").HasMaxLength(500);
        builder.Property(b => b.PrimaryColor).HasColumnName("primary_color").HasMaxLength(7).HasDefaultValue("#18181b");
        builder.Property(b => b.CreatedAt).HasColumnName("created_at");
        builder.Property(b => b.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(b => b.Slug).IsUnique();
    }
}