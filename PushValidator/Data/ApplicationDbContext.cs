using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PushValidator.Models;

namespace PushValidator.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {

        public DbSet<DeviceModel> Devices { get; set; }
        public DbSet<TransactionModel> Transactions { get; set; }
        public DbSet<AuthenticationResultModel> AuthenticationResults { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Core Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Core Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);

            // Custom data types
            //builder.Entity<DeviceModel>(b =>
            //{
            //    b.Property(a => a.Id);
            //    b.Property(a => a.UserId);
            //    b.Property(a => a.DeviceToken);
            //    b.Property(a => a.SymmetricKey);
            //    b.Property(a => a.Registered);

            //    b.HasKey(a => a.Id);

            //    b.ToTable("Devices");
            //});

            //builder.Entity<TransactionModel>(b =>
            //{
            //    b.Property(a => a.Id);
            //    b.Property(a => a.UserId);
            //    b.Property(a => a.ApplicationId);
            //    b.Property(a => a.ClientIP);
            //    b.Property(a => a.GeoLocation);
            //    b.Property(a => a.UserName);

            //    b.HasKey(a => a.Id);

            //    b.ToTable("Transactions");
            //});

            //builder.Entity<AuthenticationResultModel>(b =>
            //{
            //    b.Property(a => a.Id);
            //    b.Property(a => a.TransactionId).IsRequired();
            //    b.Property(a => a.Result).IsRequired();
            //    b.Property(a => a.CertificateFingerprint);
            //    b.Property(a => a.ActualClientIP);
            //    b.Property(a => a.ClientIPMatch);
            //    b.Property(a => a.ServerURI);

            //    b.HasKey(a => a.Id);

            //    b.ToTable("AuthenticationResults");
            //});

            //builder.Entity("PushValidator.Models.DeviceModel", b =>
            //{
            //    b.HasOne("PushValidator.Models.ApplicationUser")
            //            .WithMany()
            //            .HasForeignKey("UserId")
            //            .OnDelete(DeleteBehavior.Cascade);
            //});

            //builder.Entity("PushValidator.Models.TransactionModel", b =>
            //{
            //    b.HasOne("PushValidator.Models.ApplicationUser")
            //            .WithMany()
            //            .HasForeignKey("UserId")
            //            .OnDelete(DeleteBehavior.Cascade);
            //});

            //builder.Entity("PushValidator.Models.AuthenticationResultModel", b =>
            //{
            //    b.HasOne("PushValidator.Models.TransactionModel")
            //            .WithOne()
            //            .HasForeignKey(typeof(TransactionModel).ToString(), "TransactionId")
            //            .OnDelete(DeleteBehavior.Cascade);
            //});
            
            //OnModelCreating(builder);
        }
    }
}
