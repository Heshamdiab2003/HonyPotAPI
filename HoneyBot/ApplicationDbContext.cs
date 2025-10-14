using HoneyBot.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace HoneyBot
{
    public class ApplicationDbContext : DbContext
    {
        // هذا الكونستركتور ضروري لـ Dependency Injection
        // ليتمكن من تمرير إعدادات الاتصال بقاعدة البيانات من ملف Program.cs
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // كل خاصية من نوع DbSet تمثل جدول في قاعدة البيانات
        public DbSet<Honeypot> Honeypots { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<RequestLog> RequestLogs { get; set; }
        public DbSet<IpBlock> IpBlocks { get; set; }

        public DbSet<Student> Students { get; set; }

        public DbSet<SecurityProfile> SecurityProfiles { get; set; }

        // (اختياري ولكن موصى به)
        // هذه الدالة لتحديد العلاقات بين الجداول بشكل صريح
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // تحديد العلاقة: الحادثة الواحدة (Incident) لديها العديد من سجلات الطلبات (RequestLogs)
            modelBuilder.Entity<Incident>()
                .HasMany(i => i.RequestLogs) // Incident has many RequestLogs
                .WithOne(r => r.Incident)    // Each RequestLog has one Incident
                .HasForeignKey(r => r.IncidentId) // The foreign key is IncidentId
                .OnDelete(DeleteBehavior.Cascade); // إذا تم حذف حادثة، احذف كل سجلاتها المرتبطة بها

            // تحديد العلاقة: المصيدة الواحدة (Honeypot) يمكن أن تكون هدفًا للعديد من الحوادث (Incidents)
            modelBuilder.Entity<Honeypot>()
                .HasMany<Incident>() // A Honeypot can have many Incidents
                .WithOne(i => i.Honeypot) // Each Incident belongs to one Honeypot
                .HasForeignKey(i => i.HoneypotId) // The foreign key is HoneypotId
                .OnDelete(DeleteBehavior.Restrict); // امنع حذف مصيدة إذا كان لديها حوادث مسجلة
        }
    }
}
