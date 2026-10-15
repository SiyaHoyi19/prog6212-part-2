using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Migrations;

[DbContext(typeof(RaceDayDbContext))]
internal sealed class RaceDayDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) => ConfigureModel(modelBuilder);

    internal static void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.11")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);
        modelBuilder.UseIdentityColumns();

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(item => item.RoleId);
            entity.Property(item => item.RoleId).ValueGeneratedOnAdd();
            entity.Property(item => item.Name).IsRequired().HasMaxLength(30);
            entity.HasIndex(item => item.Name).IsUnique();
            entity.HasData(
                new Role { RoleId = 1, Name = "Organiser" },
                new Role { RoleId = 2, Name = "Participant" });
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("AppUsers");
            entity.HasKey(item => item.UserId);
            entity.Property(item => item.UserId).ValueGeneratedOnAdd();
            entity.Property(item => item.FullName).IsRequired().HasMaxLength(50);
            entity.Property(item => item.Email).IsRequired().HasMaxLength(50);
            entity.Property(item => item.PhoneNumber).IsRequired().HasMaxLength(15);
            entity.Property(item => item.DateOfBirth).HasColumnType("date");
            entity.Property(item => item.Username).IsRequired().HasMaxLength(50);
            entity.Property(item => item.PasswordHash).IsRequired().HasMaxLength(500);
            entity.HasIndex(item => item.Email).IsUnique();
            entity.HasIndex(item => item.Username).IsUnique();
            entity.HasOne(item => item.Role).WithMany(item => item.Users)
                .HasForeignKey(item => item.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(item => item.EventId);
            entity.Property(item => item.EventId).ValueGeneratedOnAdd();
            entity.Property(item => item.Name).IsRequired().HasMaxLength(100);
            entity.Property(item => item.Description);
            entity.Property(item => item.Date).HasColumnType("date");
            entity.Property(item => item.Location).IsRequired().HasMaxLength(100);
            entity.Property(item => item.Distance);
            entity.Property(item => item.Type).IsRequired().HasMaxLength(10);
            entity.HasOne(item => item.Organiser).WithMany(item => item.Events)
                .HasForeignKey(item => item.OrganiserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(item => item.CategoryId);
            entity.Property(item => item.CategoryId).ValueGeneratedOnAdd();
            entity.Property(item => item.Name).IsRequired();
            entity.Property(item => item.Distance).IsRequired();
            entity.Property(item => item.MinAge);
            entity.HasOne(item => item.Event).WithMany(item => item.Categories)
                .HasForeignKey(item => item.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.ToTable("Enrolments");
            entity.HasKey(item => item.EnrolmentId);
            entity.Property(item => item.EnrolmentId).ValueGeneratedOnAdd();
            entity.Property(item => item.Date).HasColumnType("date");
            entity.Property(item => item.Status).IsRequired();
            entity.HasIndex(item => new { item.UserId, item.EventId }).IsUnique();
            entity.HasOne(item => item.User).WithMany(item => item.Enrolments)
                .HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Event).WithMany(item => item.Enrolments)
                .HasForeignKey(item => item.EventId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Category).WithMany(item => item.Enrolments)
                .HasForeignKey(item => item.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Result).WithOne(item => item.Enrolment)
                .HasForeignKey<Result>(item => item.EnrolmentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Results");
            entity.HasKey(item => item.ResultId);
            entity.Property(item => item.ResultId).ValueGeneratedOnAdd();
            entity.Property(item => item.FinishTime);
            entity.Property(item => item.Position);
            entity.Property(item => item.ResultStatus).IsRequired();
            entity.HasIndex(item => item.EnrolmentId).IsUnique();
        });
    }
}
