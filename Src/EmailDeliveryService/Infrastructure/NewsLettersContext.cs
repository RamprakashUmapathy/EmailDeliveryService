using EmailDeliveryService.Model;
using Microsoft.EntityFrameworkCore;

namespace EmailDeliveryService.Infrastructure
{
    public partial class NewsLettersContext : DbContext
    {


        public NewsLettersContext(DbContextOptions<NewsLettersContext> options)
            : base(options)
        {
        }

        public virtual DbSet<MailArchive> MailArchives { get; set; }
        public virtual DbSet<MailStatus> MailStatus { get; set; }
        public virtual DbSet<Mail> Mails { get; set; }
        public virtual DbSet<Template> Templates { get; set; }
        public virtual DbSet<ContoApertoTemplateData> ContoApertoTemplateData { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<ContoApertoTemplateData>(entity =>
            {
                entity.ToTable("ContoApertoTemplateData", "nl");

                entity.Property(e => e.BrandId)
                    .IsRequired()
                    .HasMaxLength(120)
                    .IsUnicode(false);

                entity.Property(e => e.CardId)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.Property(e => e.Discount)
                    .IsRequired()
                    .HasMaxLength(3)
                    .IsUnicode(false);

                entity.Property(e => e.DueAmount).HasColumnType("money");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(300)
                    .IsUnicode(false);

                entity.Property(e => e.LastPurchaseDate).HasColumnType("date");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(120)
                    .IsUnicode(false);

                entity.Property(e => e.ShopId)
                    .IsRequired()
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.ShopName)
                    .IsRequired()
                    .HasMaxLength(20)
                    .IsUnicode(false);
               
                entity.Property(e => e.Surname)
                    .IsRequired()
                    .HasMaxLength(120)
                    .IsUnicode(false);

                entity.Property(e => e.TotalPurchase).HasColumnType("money");
            });

            modelBuilder.Entity<MailArchive>(entity =>
            {
                entity.ToTable("MailArchives", "nl");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.BodyParametersData).IsRequired();

                entity.Property(e => e.CardId)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.EmailId)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(e => e.EmailIdBcc)
                    .IsRequired()
                    .HasColumnName("EmailIdBCC")
                    .HasMaxLength(2000);

                entity.Property(e => e.EmailIdCc)
                    .IsRequired()
                    .HasColumnName("EmailIdCC")
                    .HasMaxLength(2000);

                entity.Property(e => e.SentDate).HasColumnType("datetime");

                entity.HasOne(d => d.Template)
                    .WithMany(p => p.MailArchives)
                    .HasForeignKey(d => d.TemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_MailArchives_Templates");
            });

            modelBuilder.Entity<MailStatus>(entity =>
            {
                entity.HasKey(e => new { e.MailId, e.LineNumber });

                entity.ToTable("MailStatus", "nl");

                entity.Property(e => e.Date).HasColumnType("date");


                entity.Property(e => e.MailResponseId).HasMaxLength(250);

                entity.Property(e => e.MailStatus1)
                    .IsRequired()
                    .HasColumnName("MailStatus")
                    .HasMaxLength(8)
                    .IsUnicode(false);

                entity.HasOne(d => d.Mail)
                    .WithMany(p => p.MailStatusNavigation)
                    .HasForeignKey(d => d.MailId)
                    .HasConstraintName("FK_MailStatus_MailId_Mails");
            });

            modelBuilder.Entity<Mail>(entity =>
            {
                entity.ToTable("Mails", "nl");

                entity.Property(e => e.CardId)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Date).HasColumnType("date");

                entity.Property(e => e.EmailId)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(e => e.EmailIdBcc)
                    .HasColumnName("EmailIdBCC")
                    .HasMaxLength(2000);

                entity.Property(e => e.EmailIdCc)
                    .HasColumnName("EmailIdCC")
                    .HasMaxLength(2000);

                entity.Property(e => e.MailStatus)
                    .IsRequired()
                    .HasMaxLength(8)
                    .IsUnicode(false);

                entity.HasOne(d => d.Template)
                    .WithMany(p => p.Mails)
                    .HasForeignKey(d => d.TemplateId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Mails_TemplateId_Templates");
            });

            modelBuilder.Entity<Template>(entity =>
            {
                entity.ToTable("Templates", "nl");

                entity.HasIndex(e => e.Name)
                    .IsUnique();

                entity.Property(e => e.Id).HasDefaultValueSql("(newid())");

                entity.Property(e => e.Awssesname)
                    .HasColumnName("AWSSESName")
                    .HasMaxLength(90);

                entity.Property(e => e.Body).IsRequired();

                entity.Property(e => e.BodyParameters).HasMaxLength(2000);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(90);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
