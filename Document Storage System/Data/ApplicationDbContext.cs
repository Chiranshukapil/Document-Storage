using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Document_Storage_System.Models;               
using Document_Storage_System.Models.Entities;

namespace Document_Storage_System.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Library> Libraries { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<LibraryPermission> LibraryPermissions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Topic>()
                .HasOne(t => t.ParentTopic)
                .WithMany(t => t.SubTopics)
                .HasForeignKey(t => t.ParentTopicId)
                .OnDelete(DeleteBehavior.Restrict); 
        }
    }
}
