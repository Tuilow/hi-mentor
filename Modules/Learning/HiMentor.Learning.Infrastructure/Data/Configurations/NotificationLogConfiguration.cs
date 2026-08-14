using HiMentor.Learning.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HiMentor.Learning.Infrastructure.Data.Configurations;

public sealed class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("notification_logs", "learning");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Channel).IsRequired().HasMaxLength(20);
        builder.Property(n => n.Template).IsRequired().HasMaxLength(100);
        builder.Property(n => n.Recipient).IsRequired().HasMaxLength(320); // RFC 5321
        builder.Property(n => n.AsaasPaymentId).HasMaxLength(100);
        builder.Property(n => n.Error).HasMaxLength(500);

        // Índice de busca principal pro suporte: "esse pagamento chegou a notificar alguém?"
        builder.HasIndex(n => n.AsaasPaymentId).HasDatabaseName("ix_notification_logs_asaas_payment_id");
        builder.HasIndex(n => n.CorrelationId).HasDatabaseName("ix_notification_logs_correlation_id");
    }
}
