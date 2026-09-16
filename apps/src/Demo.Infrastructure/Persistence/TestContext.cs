using System.Reflection;

namespace Demo.Infrastructure.Persistence;

public class TestContext : BaseContext
{
    public TestContext(DbContextOptions<TestContext> options)
        : base(options) { }

    public DbSet<Dia> Dias { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<WhatsAppUsage> WhatsAppUsages { get; set; }

    // Event Bus Example Entities
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderLineItem> OrderLineItems { get; set; }

    // Analytics Example Entities
    public DbSet<Sale> Sales { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // 1. Register entities with default conventions and BaseEntityTypeConfiguration
        // Usuario will be mapped to table "Usuarios" (from DbSet name)
        BaseEntityRegistration.RegisterEntities(modelBuilder, typeof(TestContext), typeof(Usuario));
        SimpleEntityRegistration.RegisterEntities(modelBuilder, typeof(TestContext), typeof(Dia));

        // Register Order and OrderLineItem for event bus examples
        BaseEntityRegistration.RegisterEntities(modelBuilder, typeof(TestContext), typeof(Order), typeof(OrderLineItem));

        // Register Sale for analytics examples
        BaseEntityRegistration.RegisterEntities(modelBuilder, typeof(TestContext), typeof(Sale));
    }
}
