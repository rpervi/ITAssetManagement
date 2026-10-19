using ITAssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAssetManagement.Infrastructure.Persistence.Configurations;

internal sealed class StockReceiptConfiguration : IEntityTypeConfiguration<StockReceipt>
{
    public void Configure(EntityTypeBuilder<StockReceipt> builder)
    {
        builder.ToTable("StockReceipts");
        builder.HasKey(receipt => receipt.ReceiptId);
        builder.Property(receipt => receipt.ReceiptNumber).HasMaxLength(50).IsRequired();
        builder.Property(receipt => receipt.ReceivedDate).HasColumnType("datetime2");
        builder.Property(receipt => receipt.Supplier).HasMaxLength(200);
        builder.Property(receipt => receipt.InvoiceNumber).HasMaxLength(100);
        builder.Property(receipt => receipt.Remarks).HasMaxLength(1000);
        builder.HasIndex(receipt => receipt.ReceiptNumber).IsUnique();
        builder.ConfigureAudit();
    }
}
