using ITAssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAssetManagement.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(employee => employee.EmployeeId);
        builder.Property(employee => employee.EmployeeCode).HasMaxLength(50).IsRequired();
        builder.Property(employee => employee.EmployeeName).HasMaxLength(150).IsRequired();
        builder.Property(employee => employee.Email).HasMaxLength(254).IsRequired();
        builder.Property(employee => employee.Department).HasMaxLength(100).IsRequired();
        builder.Property(employee => employee.Designation).HasMaxLength(100);
        builder.Property(employee => employee.Phone).HasMaxLength(30);
        builder.Property(employee => employee.Location).HasMaxLength(150);
        builder.Property(employee => employee.JoiningDate).HasColumnType("date");
        builder.Property(employee => employee.IsActive).HasDefaultValue(true);
        builder.HasIndex(employee => employee.EmployeeCode).IsUnique();
        builder.HasIndex(employee => employee.Email).IsUnique();
        builder.ConfigureAudit();
    }
}
