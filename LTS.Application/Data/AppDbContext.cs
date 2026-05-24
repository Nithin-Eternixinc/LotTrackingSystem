using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using LTS.Common.Models;

namespace LTS.Application.Data;

public class AppDbContext : DbContext   //gets all database features from EF
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) // receive database settings and pass them to the EF Core
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<ApplicationLog> ApplicationLogs { get; set; }
    public DbSet<WaferMaster> WaferMasters { get; set; }
    public DbSet<Carrier> Carriers { get; set; }
    public DbSet<Lot> Lots { get; set; }
    public DbSet<ProcessLocation> ProcessLocations { get; set; }
    public DbSet<LotRoute> LotRoutes { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder) //specl method , rules and instruction to build db model, OnModelCreating() to manually configure everything
    {
        base.OnModelCreating(modelBuilder);

        // unique constraints
        modelBuilder.Entity<User>()
            .HasIndex(u => u.UserName)
            .IsUnique();

        modelBuilder.Entity<Supplier>()
            .HasIndex(s => s.SupplierName)
            .IsUnique();

        modelBuilder.Entity<WaferMaster>()
          .HasIndex(w => w.WaferSerialNo)
          .IsUnique();
        modelBuilder.Entity<WaferMaster>()
            .HasKey(w => w.WaferId);

        // Supplier → WaferMaster (one supplier has many wafers)
        // Wafer → Lot (one wafer belongs to one lot)
        modelBuilder.Entity<WaferMaster>()
            .HasOne(w => w.Lot)
            .WithMany()
            .HasForeignKey(w => w.LotId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Carrier>()
            .HasIndex(c => c.CarrierCode)
            .IsUnique();

        modelBuilder.Entity<Lot>()
            .HasOne(l => l.Carrier)
            .WithMany()
            .HasForeignKey(l => l.CarrierId);

        // ProcessLocation → Lot
        modelBuilder.Entity<ProcessLocation>()
            .HasOne(p => p.CurrentLot)
            .WithMany()
            .HasForeignKey(p => p.CurrentLotId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<LotRoute>()
            .HasOne(r => r.Lot)
            .WithMany(l => l.LotRoutes)
            .HasForeignKey(r => r.LotId)
            .OnDelete(DeleteBehavior.Cascade);

        // LotRoute → ProcessLocation
        modelBuilder.Entity<LotRoute>()
            .HasOne(r => r.ProcessLocation)
            .WithMany()
            .HasForeignKey(r => r.ProcessLocationId)
            .OnDelete(DeleteBehavior.Restrict);


        // seed data 
        modelBuilder.Entity<User>().HasData(
            new User
            {
                UserId = 1,
                UserName = "Engineer01",
                PasswordHash = "123",
                Role = "Engineer",
                IsActive = true
            },
            new User
            {
                UserId = 2,
                UserName = "Operator01",
                PasswordHash = "123",
                Role = "Operator",
                IsActive = true
            }

            );
        modelBuilder.Entity<ProcessLocation>().HasData(
            Enumerable.Range(1, 10).Select(i => new ProcessLocation
            {
                ProcessLocationId = i,
                Name = $"PL-{i:D2}",  //D2 means:2 digit formatting
                SequenceNo = i,
                Status = "Available",
                CurrentLotId = null
            }).ToArray());



    }
}

    