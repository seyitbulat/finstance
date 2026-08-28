using Finstance.dbContext.Models;
using Microsoft.EntityFrameworkCore;

namespace Finstance.dbContext;



public class DataBaseContext : DbContext
{
    public DbSet<ExpenseModel> Expenses { get; set; }
    public DbSet<UserModel> Users { get; set; }
    public DbSet<ExpenseLocationModel> Locations { get; set; }
    public DbSet<BankStatementModel> BankStatements { get; set; }

    public DataBaseContext(DbContextOptions<DataBaseContext> options) : base(options){}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserModel>(entity =>
        {
           entity.HasKey(e => e.Id);

           entity.Property(e => e.Username);
           entity.Property(e => e.Password); 
        });

        modelBuilder.Entity<ExpenseLocationModel>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name);

            entity.Property(e => e.Category);
        });

        modelBuilder.Entity<ExpenseModel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Date).HasColumnType("date");
            entity.Property(e => e.Amount).HasColumnType("decimal");
            entity.HasOne(e => e.Location).WithMany().HasForeignKey(e => e.LocationId);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.BankStatement).WithMany().HasForeignKey(e => e.BankStatementId);
        });
    }
}


