using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Reflection.Metadata;

namespace myMessenger_back.Models
{
    public class ApplicationContext: DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<UserData> UsersData { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationMembers> ConversationMembers { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ConversationsMessages> ConversationsMessages { get; set; }

        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConversationMembers>()
                .HasOne(c => c.ConversationData)
                .WithMany(b => b.ConversationMembers)
                .HasForeignKey(m => m.ConversationId);
        }
    }
}
