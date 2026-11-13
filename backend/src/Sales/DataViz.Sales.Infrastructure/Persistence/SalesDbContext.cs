using DataViz.BuildingBlocks.Infrastructure.Outbox;
using DataViz.Sales.Application.Stores;
using DataViz.Sales.Domain.Sales;
using DataViz.Sales.Domain.Stores;
using Microsoft.EntityFrameworkCore;

namespace DataViz.Sales.Infrastructure.Persistence;

public sealed class SalesDbContext(DbContextOptions<SalesDbContext> options) : DbContext(options)
{
    public DbSet<Store> Stores => Set<Store>();

    public DbSet<Sale> Sales => Set<Sale>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());

        modelBuilder.Entity<Store>(store =>
        {
            store.ToTable("stores");
            store.HasKey(s => s.Id);
            store.Property(s => s.Id).ValueGeneratedNever();
            store.Property(s => s.Name).HasMaxLength(120);
            store.Property(s => s.City).HasMaxLength(120);
            store.Ignore(s => s.DomainEvents);
        });

        modelBuilder.Entity<Sale>(sale =>
        {
            sale.ToTable("sales");
            sale.HasKey(s => s.Id);
            sale.Property(s => s.Id).ValueGeneratedNever();
            sale.Property(s => s.Category).HasConversion<string>().HasMaxLength(30);
            sale.Property(s => s.Channel).HasConversion<string>().HasMaxLength(30);
            sale.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
            sale.HasOne<Store>().WithMany().HasForeignKey(s => s.StoreId).OnDelete(DeleteBehavior.Restrict);
            sale.Ignore(s => s.DomainEvents);
            sale.ComplexProperty(s => s.Amount, amount =>
            {
                amount.Property(money => money.Amount).HasColumnName("Amount").HasPrecision(18, 2);
                amount.Property(money => money.Currency).HasColumnName("Currency").HasMaxLength(3);
            });
        });
    }
}

internal sealed class StoreRepository(SalesDbContext context) : IStoreRepository
{
    public Task<Store?> GetById(Guid id, CancellationToken cancellationToken) =>
        context.Stores.FirstOrDefaultAsync(store => store.Id == id, cancellationToken);

    public void Add(Store store) => context.Stores.Add(store);
}

internal sealed class SaleRepository(SalesDbContext context) : ISaleRepository
{
    public Task<Sale?> GetById(Guid id, CancellationToken cancellationToken) =>
        context.Sales.FirstOrDefaultAsync(sale => sale.Id == id, cancellationToken);

    public void Add(Sale sale) => context.Sales.Add(sale);
}

internal sealed class SalesQueries(SalesDbContext context) : ISalesQueries
{
    public Task<bool> HasAnySale(CancellationToken cancellationToken) =>
        context.Sales.AnyAsync(cancellationToken);

    public async Task<IReadOnlyList<StoreResponse>> ListStores(CancellationToken cancellationToken) =>
        await context.Stores
            .AsNoTracking()
            .OrderBy(store => store.Name)
            .Select(store => new StoreResponse(store.Id, store.Name, store.City))
            .ToListAsync(cancellationToken);
}
