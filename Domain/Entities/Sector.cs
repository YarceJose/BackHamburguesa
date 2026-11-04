using Domain.Errors;
using Domain.Exceptions;

namespace Domain.Entities;

/// <summary>Rural delivery area (vereda, sector or barrio). Towns without street addresses deliver by sector.</summary>
public sealed class Sector
{
    private Sector() { }

    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }

    public static Sector Create(string name, int sortOrder = 0) => new() { Name = Normalize(name), SortOrder = sortOrder };

    public void Rename(string name) => Name = Normalize(name);
    public void SetActive(bool isActive) => IsActive = isActive;
    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;

    private static string Normalize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(ErrorCodes.SectorNameRequired, "Sector name is required.");
        return name.Trim();
    }
}
