using OrderProcessing.API.Domain;

namespace OrderProcessing.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        RemovePluralizingTableNameConvention(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        ConfigureBaseProperties(modelBuilder);
        EnforceIndexIsCreatedOnline(modelBuilder);
    }

    private static void RemovePluralizingTableNameConvention(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            entity.SetTableName(entity.DisplayName());
    }

    private static void EnforceIndexIsCreatedOnline(ModelBuilder modelBuilder)
    {
        foreach (var mutableIndex in modelBuilder.Model.GetEntityTypes().SelectMany(x => x.GetIndexes()))
            mutableIndex.SetIsCreatedConcurrently(true);
    }

    private static void ConfigureBaseProperties(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes()
                     .Where(t => typeof(IEntity).IsAssignableFrom(t.ClrType)))
        {
            builder.Entity(
                entityType.Name,
                x => x.Property(nameof(IEntity.CreatedAt)).HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'"));

            builder.Entity(
                entityType.Name,
                x => x.HasIndex(nameof(IEntity.CreatedAt)));
        }
    }
}