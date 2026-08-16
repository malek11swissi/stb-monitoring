using HrSimulator.Models;
using Microsoft.EntityFrameworkCore;

namespace HrSimulator.Data;

public sealed class HrDbContext(DbContextOptions<HrDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Employee>(e => { e.ToTable("employees"); e.HasKey(x => x.Matricule); e.Property(x => x.Matricule).HasMaxLength(20); e.Property(x => x.FirstName).HasMaxLength(80); e.Property(x => x.LastName).HasMaxLength(80); e.Property(x => x.DepartmentCode).HasMaxLength(20); e.Property(x => x.JobTitle).HasMaxLength(120); e.Property(x => x.EmploymentStatus).HasMaxLength(20); });
        builder.Entity<Department>(e => { e.ToTable("departments"); e.HasKey(x => x.Code); e.Property(x => x.Code).HasMaxLength(20); e.Property(x => x.Name).HasMaxLength(120); e.Property(x => x.Location).HasMaxLength(120); });
    }
}
