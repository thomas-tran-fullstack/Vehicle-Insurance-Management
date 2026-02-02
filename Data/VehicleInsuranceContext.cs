using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using VehicleInsuranceAPI.Models; // Đảm bảo đã import Models

namespace VehicleInsuranceAPI.Data
{
    public partial class VehicleInsuranceContext : DbContext
    {
        public VehicleInsuranceContext()
        {
        }

        public VehicleInsuranceContext(DbContextOptions<VehicleInsuranceContext> options)
            : base(options)
        {
        }

        public virtual DbSet<AuditLog> AuditLogs { get; set; }

        public virtual DbSet<Bill> Bills { get; set; }

        public virtual DbSet<Claim> Claims { get; set; }

        public virtual DbSet<CompanyExpense> CompanyExpenses { get; set; }

        public virtual DbSet<Contact> Contacts { get; set; }

        public virtual DbSet<Customer> Customers { get; set; }

        public virtual DbSet<Estimate> Estimates { get; set; }

        public virtual DbSet<Faq> Faqs { get; set; }

        public virtual DbSet<Feedback> Feedbacks { get; set; }

        public virtual DbSet<InsuranceCancellation> InsuranceCancellations { get; set; }

        public virtual DbSet<Notification> Notifications { get; set; }

        public virtual DbSet<Penalty> Penalties { get; set; }

        public virtual DbSet<Policy> Policies { get; set; }

        public virtual DbSet<Role> Roles { get; set; }

        public virtual DbSet<Staff> Staff { get; set; }

        public virtual DbSet<Testimonial> Testimonials { get; set; }

        public virtual DbSet<User> Users { get; set; }

        public virtual DbSet<Vehicle> Vehicles { get; set; }

        public virtual DbSet<VehicleInspection> VehicleInspections { get; set; }

        public virtual DbSet<VehicleModel> VehicleModels { get; set; }

        // Đã xóa hàm OnConfiguring để tránh lỗi hard-code connection string

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.LogId).HasName("PK__AuditLog__5E54864819BDE266");

                entity.Property(e => e.Action).HasMaxLength(255);
                entity.Property(e => e.LogDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");

                entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK__AuditLogs__UserI__412EB0B6");
            });

            modelBuilder.Entity<Bill>(entity =>
            {
                entity.HasKey(e => e.BillId).HasName("PK__Bills__11F2FC6AD00BE617");

                entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.Paid).HasDefaultValue(false);

                entity.HasOne(d => d.Policy).WithMany(p => p.Bills)
                    .HasForeignKey(d => d.PolicyId)
                    .HasConstraintName("FK__Bills__PolicyId__5DCAEF64");
            });

            modelBuilder.Entity<Claim>(entity =>
            {
                entity.HasKey(e => e.ClaimId).HasName("PK__Claims__EF2E139BA4BC5961");

                entity.Property(e => e.AccidentPlace).HasMaxLength(255);
                entity.Property(e => e.ClaimAmount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.InsuredAmount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.Status).HasMaxLength(50);

                entity.HasOne(d => d.Policy).WithMany(p => p.Claims)
                    .HasForeignKey(d => d.PolicyId)
                    .HasConstraintName("FK__Claims__PolicyId__60A75C0F");
            });

            modelBuilder.Entity<CompanyExpense>(entity =>
            {
                entity.HasKey(e => e.ExpenseId).HasName("PK__CompanyE__1445CFD307641396");

                entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.ExpenseType).HasMaxLength(100);
            });

            modelBuilder.Entity<Contact>(entity =>
            {
                entity.HasKey(e => e.ContactId).HasName("PK__Contacts__5C66259B78481A1B");

                entity.Property(e => e.CreatedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.Email).HasMaxLength(150);
                entity.Property(e => e.Message).HasMaxLength(500);
                entity.Property(e => e.Name).HasMaxLength(150);
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.CustomerId).HasName("PK__Customer__A4AE64D852FDE23B");

                entity.Property(e => e.Address).HasMaxLength(255);
                entity.Property(e => e.CustomerName).HasMaxLength(150);
                entity.Property(e => e.Phone).HasMaxLength(20);

                entity.HasOne(d => d.User).WithMany(p => p.Customers)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK__Customers__UserI__47DBAE45");
            });

            modelBuilder.Entity<Estimate>(entity =>
            {
                entity.HasKey(e => e.EstimateId).HasName("PK__Estimate__ABEBF4B5C276F608");

                entity.Property(e => e.CreatedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.EstimateAmount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.PolicyType).HasMaxLength(100);
                entity.Property(e => e.Warranty).HasMaxLength(100);

                entity.HasOne(d => d.Customer).WithMany(p => p.Estimates)
                    .HasForeignKey(d => d.CustomerId)
                    .HasConstraintName("FK__Estimates__Custo__5535A963");

                entity.HasOne(d => d.Vehicle).WithMany(p => p.Estimates)
                    .HasForeignKey(d => d.VehicleId)
                    .HasConstraintName("FK__Estimates__Vehic__5629CD9C");
            });

            modelBuilder.Entity<Faq>(entity =>
            {
                entity.HasKey(e => e.Faqid).HasName("PK__FAQs__4B89D182977D75E3");

                entity.ToTable("FAQs");

                entity.Property(e => e.Faqid).HasColumnName("FAQId");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.Question).HasMaxLength(500);
            });

            modelBuilder.Entity<Feedback>(entity =>
            {
                entity.HasKey(e => e.FeedbackId).HasName("PK__Feedback__6A4BEDD693D7ECC8");

                entity.ToTable("Feedback");

                entity.Property(e => e.Content).HasMaxLength(500);
                entity.Property(e => e.CreatedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");

                entity.HasOne(d => d.Customer).WithMany(p => p.Feedbacks)
                    .HasForeignKey(d => d.CustomerId)
                    .HasConstraintName("FK__Feedback__Custom__6C190EBB");
            });

            modelBuilder.Entity<InsuranceCancellation>(entity =>
            {
                entity.HasKey(e => e.CancellationId).HasName("PK__Insuranc__6A2D9A3A7D836BC8");

                entity.Property(e => e.RefundAmount).HasColumnType("decimal(18, 2)");

                entity.HasOne(d => d.Policy).WithMany(p => p.InsuranceCancellations)
                    .HasForeignKey(d => d.PolicyId)
                    .HasConstraintName("FK__Insurance__Polic__66603565");
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E12C5331BC1");

                entity.Property(e => e.CreatedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.IsRead).HasDefaultValue(false);
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Type).HasMaxLength(50);

                entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK__Notificat__UserI__778AC167");
            });

            modelBuilder.Entity<Penalty>(entity =>
            {
                entity.HasKey(e => e.PenaltyId).HasName("PK__Penaltie__567E06C7CB6E39AD");

                entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.Reason).HasMaxLength(255);
                entity.Property(e => e.Status).HasMaxLength(50);

                entity.HasOne(d => d.Policy).WithMany(p => p.Penalties)
                    .HasForeignKey(d => d.PolicyId)
                    .HasConstraintName("FK__Penalties__Polic__6383C8BA");
            });

            modelBuilder.Entity<Policy>(entity =>
            {
                entity.HasKey(e => e.PolicyId).HasName("PK__Policies__2E1339A45F41D5A8");

                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.Warranty).HasMaxLength(100);
                entity.Property(e => e.IsHidden).HasDefaultValue(false);

                entity.HasOne(d => d.Customer).WithMany(p => p.Policies)
                    .HasForeignKey(d => d.CustomerId)
                    .HasConstraintName("FK__Policies__Custom__59063A47");

                entity.HasOne(d => d.Vehicle).WithMany(p => p.Policies)
                    .HasForeignKey(d => d.VehicleId)
                    .HasConstraintName("FK__Policies__Vehicl__59FA5E80");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1ADA630380");

                entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B61605683E1A1").IsUnique();

                entity.Property(e => e.RoleName).HasMaxLength(50);
            });

            modelBuilder.Entity<Staff>(entity =>
            {
                entity.HasKey(e => e.StaffId).HasName("PK__Staff__96D4AB178336D0EA");

                entity.Property(e => e.FullName).HasMaxLength(150);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Position).HasMaxLength(100);

                entity.HasOne(d => d.User).WithMany(p => p.Staff)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Staff__UserId__44FF419A");
            });

            modelBuilder.Entity<Testimonial>(entity =>
            {
                entity.HasKey(e => e.TestimonialId).HasName("PK__Testimon__91A23E73186D8690");

                entity.Property(e => e.Approved).HasDefaultValue(false);
                entity.Property(e => e.Content).HasMaxLength(500);

                entity.HasOne(d => d.Customer).WithMany(p => p.Testimonials)
                    .HasForeignKey(d => d.CustomerId)
                    .HasConstraintName("FK__Testimoni__Custo__6FE99F9F");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C69B8279C");

                entity.HasIndex(e => e.Username, "UQ__Users__536C85E4DA98F8F0").IsUnique();

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.Email).HasMaxLength(150);
                entity.Property(e => e.IsLocked).HasDefaultValue(false);
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("ACTIVE");
                entity.Property(e => e.BannedUntil).HasColumnType("datetime");
                entity.Property(e => e.PasswordHash).HasMaxLength(255);
                entity.Property(e => e.Username).HasMaxLength(100);

                entity.HasOne(d => d.Role).WithMany(p => p.Users)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Users__RoleId__3D5E1FD2");
            });

            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(e => e.VehicleId).HasName("PK__Vehicles__476B5492A5AA2C9A");

                entity.Property(e => e.BodyNumber).HasMaxLength(100);
                entity.Property(e => e.EngineNumber).HasMaxLength(100);
                entity.Property(e => e.VehicleName).HasMaxLength(100);
                entity.Property(e => e.VehicleNumber).HasMaxLength(50);
                entity.Property(e => e.VehicleRate).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.VehicleVersion).HasMaxLength(50);

                entity.HasOne(d => d.Customer).WithMany(p => p.Vehicles)
                    .HasForeignKey(d => d.CustomerId)
                    .HasConstraintName("FK__Vehicles__Custom__4CA06362");

                entity.HasOne(d => d.Model).WithMany(p => p.Vehicles)
                    .HasForeignKey(d => d.ModelId)
                    .HasConstraintName("FK__Vehicles__ModelI__4D94879B");
            });

            modelBuilder.Entity<VehicleInspection>(entity =>
            {
                entity.HasKey(e => e.InspectionId).HasName("PK__VehicleI__30B2DC0838F32527");

                entity.Property(e => e.InspectionDate).HasColumnType("datetime");
                entity.Property(e => e.Result).HasMaxLength(255);
                entity.Property(e => e.Status).HasMaxLength(50);

                entity.HasOne(d => d.Staff).WithMany(p => p.VehicleInspections)
                    .HasForeignKey(d => d.StaffId)
                    .HasConstraintName("FK__VehicleIn__Staff__5165187F");

                entity.HasOne(d => d.Vehicle).WithMany(p => p.VehicleInspections)
                    .HasForeignKey(d => d.VehicleId)
                    .HasConstraintName("FK__VehicleIn__Vehic__5070F446");
            });

            modelBuilder.Entity<VehicleModel>(entity =>
            {
                entity.HasKey(e => e.ModelId).HasName("PK__VehicleM__E8D7A12CE3CF0DC1");

                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.ModelName).HasMaxLength(100);
                entity.Property(e => e.VehicleClass).HasMaxLength(50);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}