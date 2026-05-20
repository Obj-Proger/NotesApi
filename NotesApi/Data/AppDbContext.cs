using Microsoft.EntityFrameworkCore;
using NotesApi.Models;

namespace NotesApi.Data
{
    /// <summary>
    /// Entity Framework Core database context for the Notes API application.
    /// Manages data access for users, notes, and tasks with automatic audit trail tracking.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Database set for user accounts and authentication data.
        /// </summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>
        /// Database set for user notes.
        /// </summary>
        public DbSet<Note> Notes { get; set; } = null!;

        /// <summary>
        /// Database set for user tasks with priority and due date tracking.
        /// </summary>
        public DbSet<TaskItem> TaskItems { get; set; } = null!;

        /// <summary>
        /// Overrides SaveChangesAsync to automatically update the UpdatedAt timestamp
        /// for all entities implementing the IAuditable interface.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>The number of state entries written to the database.</returns>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Update the modification timestamp for all modified auditable entities.
            foreach (var entry in ChangeTracker.Entries<IAuditable>())
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Configures entity models, relationships, constraints, and database schema.
        /// </summary>
        /// <param name="modelBuilder">The model builder used to configure entity mappings.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureUserEntity(modelBuilder);
            ConfigureNoteEntity(modelBuilder);
            ConfigureTaskItemEntity(modelBuilder);
        }

        /// <summary>
        /// Configures the User entity with unique constraints and audit timestamps.
        /// </summary>
        private static void ConfigureUserEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                // Enforce uniqueness on email and username to prevent duplicate accounts.
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();

                // Define column constraints and lengths.
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);

                // Configure audit timestamps with UTC default values.
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("now() at time zone 'utc'")
                      .ValueGeneratedOnAdd();
                entity.Property(e => e.UpdatedAt)
                      .ValueGeneratedOnUpdate();
            });
        }

        /// <summary>
        /// Configures the Note entity with foreign key relationships and cascading delete.
        /// </summary>
        private static void ConfigureNoteEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Note>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                // Index UserId for efficient queries filtering notes by user.
                entity.HasIndex(e => e.UserId);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.Color).HasMaxLength(50);

                // Configure audit timestamps with UTC default values.
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("now() at time zone 'utc'")
                      .ValueGeneratedOnAdd();
                entity.Property(e => e.UpdatedAt)
                      .ValueGeneratedOnUpdate();

                // Configure one-to-many relationship with User.
                // Cascade delete ensures notes are removed when the user is deleted.
                entity.HasOne(e => e.User)
                      .WithMany(e => e.Notes)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        /// <summary>
        /// Configures the TaskItem entity with priority enum conversion and cascading delete.
        /// </summary>
        private static void ConfigureTaskItemEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();

                // Index UserId for efficient queries filtering tasks by user.
                entity.HasIndex(e => e.UserId);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(2000);

                // Store enum as integer value in the database for better performance.
                entity.Property(e => e.Priority).HasConversion<int>();

                // Configure audit timestamps with UTC default values.
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("now() at time zone 'utc'")
                      .ValueGeneratedOnAdd();
                entity.Property(e => e.UpdatedAt)
                      .ValueGeneratedOnUpdate();

                // Configure one-to-many relationship with User.
                // Cascade delete ensures tasks are removed when the user is deleted.
                entity.HasOne(e => e.User)
                      .WithMany(e => e.TaskItems)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}