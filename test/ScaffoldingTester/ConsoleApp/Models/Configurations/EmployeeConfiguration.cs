﻿using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConsoleApp.Models.Configurations;

public partial class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> entity)
    {
        entity.ToTable("Employee");

        entity.HasIndex(e => e.ReportsTo, "IFK_EmployeeReportsTo");

        entity.Property(e => e.Address).HasMaxLength(70);
        entity.Property(e => e.BirthDate).HasColumnType("datetime");
        entity.Property(e => e.City).HasMaxLength(40);
        entity.Property(e => e.Country).HasMaxLength(40);
        entity.Property(e => e.Email).HasMaxLength(60);
        entity.Property(e => e.Fax).HasMaxLength(24);
        entity.Property(e => e.FirstName)
            .IsRequired()
            .HasMaxLength(20);
        entity.Property(e => e.HireDate).HasColumnType("datetime");
        entity.Property(e => e.LastName)
            .IsRequired()
            .HasMaxLength(20);
        entity.Property(e => e.Phone).HasMaxLength(24);
        entity.Property(e => e.PostalCode).HasMaxLength(10);
        entity.Property(e => e.State).HasMaxLength(40);
        entity.Property(e => e.Title).HasMaxLength(30);

        entity.HasOne(d => d.ReportsToNavigation).WithMany(p => p.InverseReportsToNavigation)
            .HasForeignKey(d => d.ReportsTo)
            .HasConstraintName("FK_EmployeeReportsTo");

        OnConfigurePartial(entity);
    }

    partial void OnConfigurePartial(EntityTypeBuilder<Employee> modelBuilder);
}
