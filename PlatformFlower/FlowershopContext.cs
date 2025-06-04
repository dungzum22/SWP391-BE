using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using PlatformFlower.Entities;

namespace PlatformFlower;

public partial class FlowershopContext : DbContext
{
    public FlowershopContext()
    {
    }

    public FlowershopContext(DbContextOptions<FlowershopContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<FlowerInfo> FlowerInfos { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrdersDetail> OrdersDetails { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<Seller> Sellers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserInfo> UserInfos { get; set; }

    public virtual DbSet<UserVoucherStatus> UserVoucherStatuses { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.AddressId).HasName("PK__Address__CAA247C8D4E4362E");

            entity.ToTable("Address");

            entity.Property(e => e.AddressId).HasColumnName("address_id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.UserInfoId).HasColumnName("user_info_id");

            entity.HasOne(d => d.UserInfo).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.UserInfoId)
                .HasConstraintName("FK__Address__user_in__59063A47");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Cart__2EF52A272A4A09B5");

            entity.ToTable("Cart");

            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.FlowerId).HasColumnName("flower_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Flower).WithMany(p => p.Carts)
                .HasForeignKey(d => d.FlowerId)
                .HasConstraintName("FK__Cart__flower_id__5629CD9C");

            entity.HasOne(d => d.User).WithMany(p => p.Carts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Cart__user_id__5535A963");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Category__D54EE9B4CB87C5CE");

            entity.ToTable("Category");

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(255)
                .HasColumnName("category_name");
        });

        modelBuilder.Entity<FlowerInfo>(entity =>
        {
            entity.HasKey(e => e.FlowerId).HasName("PK__Flower_I__177E0A7ED3B1A7BB");

            entity.ToTable("Flower_Info");

            entity.Property(e => e.FlowerId).HasColumnName("flower_id");
            entity.Property(e => e.AvailableQuantity).HasColumnName("available_quantity");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.FlowerDescription)
                .HasMaxLength(255)
                .HasColumnName("flower_description");
            entity.Property(e => e.FlowerName)
                .HasMaxLength(255)
                .HasColumnName("flower_name");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("image_url");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.SellerId).HasColumnName("seller_id");

            entity.HasOne(d => d.Category).WithMany(p => p.FlowerInfos)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Flower_In__categ__5165187F");

            entity.HasOne(d => d.Seller).WithMany(p => p.FlowerInfos)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("FK__Flower_In__selle__52593CB8");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__46596229BCF2FF24");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.AddressId).HasColumnName("address_id");
            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.DeliveryMethod)
                .HasMaxLength(255)
                .HasColumnName("delivery_method");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("phone_number");
            entity.Property(e => e.StatusPayment)
                .HasMaxLength(20)
                .HasColumnName("status_payment");
            entity.Property(e => e.TotalPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_price");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.UserVoucherStatusId).HasColumnName("user_voucher_status_id");

            entity.HasOne(d => d.Address).WithMany(p => p.Orders)
                .HasForeignKey(d => d.AddressId)
                .HasConstraintName("FK__Orders__address___6477ECF3");

            entity.HasOne(d => d.Cart).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CartId)
                .HasConstraintName("FK__Orders__cart_id__656C112C");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Orders__user_id__628FA481");

            entity.HasOne(d => d.UserVoucherStatus).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserVoucherStatusId)
                .HasConstraintName("FK__Orders__user_vou__6383C8BA");
        });

        modelBuilder.Entity<OrdersDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("PK__Orders_D__3C5A408078DAC500");

            entity.ToTable("Orders_Detail");

            entity.Property(e => e.OrderDetailId).HasColumnName("order_detail_id");
            entity.Property(e => e.AddressId).HasColumnName("address_id");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DeliveryMethod)
                .HasMaxLength(255)
                .HasColumnName("delivery_method");
            entity.Property(e => e.FlowerId).HasColumnName("flower_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.UserVoucherStatusId).HasColumnName("user_voucher_status_id");

            entity.HasOne(d => d.Address).WithMany(p => p.OrdersDetails)
                .HasForeignKey(d => d.AddressId)
                .HasConstraintName("FK__Orders_De__addre__6D0D32F4");

            entity.HasOne(d => d.Flower).WithMany(p => p.OrdersDetails)
                .HasForeignKey(d => d.FlowerId)
                .HasConstraintName("FK__Orders_De__flowe__6B24EA82");

            entity.HasOne(d => d.Order).WithMany(p => p.OrdersDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Orders_De__order__6A30C649");

            entity.HasOne(d => d.Seller).WithMany(p => p.OrdersDetails)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("FK__Orders_De__selle__6C190EBB");

            entity.HasOne(d => d.UserVoucherStatus).WithMany(p => p.OrdersDetails)
                .HasForeignKey(d => d.UserVoucherStatusId)
                .HasConstraintName("FK__Orders_De__user___6E01572D");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__Report__779B7C58DEDFFF56");

            entity.ToTable("Report");

            entity.Property(e => e.ReportId).HasColumnName("report_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.FlowerId).HasColumnName("flower_id");
            entity.Property(e => e.ReportDescription)
                .HasMaxLength(255)
                .HasColumnName("report_description");
            entity.Property(e => e.ReportReason)
                .HasMaxLength(255)
                .HasColumnName("report_reason");
            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Flower).WithMany(p => p.Reports)
                .HasForeignKey(d => d.FlowerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Report__flower_i__75A278F5");

            entity.HasOne(d => d.Seller).WithMany(p => p.Reports)
                .HasForeignKey(d => d.SellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Report__seller_i__76969D2E");

            entity.HasOne(d => d.User).WithMany(p => p.Reports)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Report__user_id__74AE54BC");
        });

        modelBuilder.Entity<Seller>(entity =>
        {
            entity.HasKey(e => e.SellerId).HasName("PK__Seller__780A0A9753C9D88D");

            entity.ToTable("Seller");

            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.Property(e => e.AddressSeller)
                .HasMaxLength(255)
                .HasColumnName("address_seller");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Introduction)
                .HasColumnType("text")
                .HasColumnName("introduction");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(0)
                .HasColumnName("quantity");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasColumnName("role");
            entity.Property(e => e.ShopName)
                .HasMaxLength(255)
                .HasColumnName("shop_name");
            entity.Property(e => e.TotalProduct)
                .HasDefaultValue(0)
                .HasColumnName("total_product");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .HasDefaultValue("seller")
                .HasColumnName("type");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Sellers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Seller__user_id__440B1D61");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__B9BE370F22BF4A40");

            entity.HasIndex(e => e.Username, "UQ__Users__F3DBC57202E6E7D0").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .HasColumnName("type");
            entity.Property(e => e.Username)
                .HasMaxLength(255)
                .HasColumnName("username");
        });

        modelBuilder.Entity<UserInfo>(entity =>
        {
            entity.HasKey(e => e.UserInfoId).HasName("PK__User_Inf__82ABEB546D1C44D2");

            entity.ToTable("User_Info");

            entity.Property(e => e.UserInfoId).HasColumnName("user_info_id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.Avatar)
                .HasMaxLength(255)
                .HasColumnName("avatar");
            entity.Property(e => e.BirthDate).HasColumnName("birth_date");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.IsSeller)
                .HasDefaultValue(false)
                .HasColumnName("is_seller");
            entity.Property(e => e.Points).HasDefaultValue(100);
            entity.Property(e => e.Sex)
                .HasMaxLength(10)
                .HasColumnName("sex");
            entity.Property(e => e.UpdatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_date");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserInfos)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__User_Info__user___4AB81AF0");
        });

        modelBuilder.Entity<UserVoucherStatus>(entity =>
        {
            entity.HasKey(e => e.UserVoucherStatusId).HasName("PK__User_Vou__6804F51C560607A9");

            entity.ToTable("User_Voucher_Status");

            entity.Property(e => e.UserVoucherStatusId).HasColumnName("user_voucher_status_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.Discount).HasColumnName("discount");
            entity.Property(e => e.EndDate)
                .HasColumnType("datetime")
                .HasColumnName("end_date");
            entity.Property(e => e.RemainingCount).HasColumnName("remaining_count");
            entity.Property(e => e.ShopId).HasColumnName("shop_id");
            entity.Property(e => e.StartDate)
                .HasColumnType("datetime")
                .HasColumnName("start_date");
            entity.Property(e => e.UsageCount)
                .HasDefaultValue(0)
                .HasColumnName("usage_count");
            entity.Property(e => e.UsageLimit).HasColumnName("usage_limit");
            entity.Property(e => e.UserInfoId).HasColumnName("user_info_id");
            entity.Property(e => e.VoucherCode)
                .HasMaxLength(50)
                .HasColumnName("voucher_code");

            entity.HasOne(d => d.Shop).WithMany(p => p.UserVoucherStatuses)
                .HasForeignKey(d => d.ShopId)
                .HasConstraintName("FK__User_Vouc__shop___5EBF139D");

            entity.HasOne(d => d.UserInfo).WithMany(p => p.UserVoucherStatuses)
                .HasForeignKey(d => d.UserInfoId)
                .HasConstraintName("FK__User_Vouc__user___5DCAEF64");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
