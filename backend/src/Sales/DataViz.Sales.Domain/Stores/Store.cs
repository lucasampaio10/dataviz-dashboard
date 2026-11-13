using DataViz.BuildingBlocks.Domain;

namespace DataViz.Sales.Domain.Stores;

public sealed class Store : AggregateRoot<Guid>
{
    private Store()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public static Store Open(string name, string city)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("store.name_required", "A store needs a name.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new DomainException("store.city_required", "A store needs a city.");
        }

        return new Store { Id = Guid.NewGuid(), Name = name.Trim(), City = city.Trim() };
    }
}

public interface IStoreRepository
{
    Task<Store?> GetById(Guid id, CancellationToken cancellationToken);

    void Add(Store store);
}
