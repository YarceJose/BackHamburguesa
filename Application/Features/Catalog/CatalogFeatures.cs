using Application.DTOs.Store;
using Application.Exceptions;
using Application.Mediator;
using Domain.Entities;
using Domain.Errors;
using Domain.Interfaces;

namespace Application.Features.Catalog;

// ---------- Menu (public) ----------
public sealed record GetMenuQuery : IRequest<List<MenuProductResponse>>;

public sealed class GetMenuHandler(IProductRepository products) : IRequestHandler<GetMenuQuery, List<MenuProductResponse>>
{
    public async Task<List<MenuProductResponse>> Handle(GetMenuQuery q, CancellationToken ct) =>
        (await products.GetActiveAsync(ct)).Select(MenuProductResponse.From).ToList();
}

// ---------- Sectors ----------
public sealed record GetSectorsQuery(bool OnlyActive) : IRequest<List<SectorResponse>>;

public sealed class GetSectorsHandler(ISectorRepository sectors) : IRequestHandler<GetSectorsQuery, List<SectorResponse>>
{
    public async Task<List<SectorResponse>> Handle(GetSectorsQuery q, CancellationToken ct) =>
        (await sectors.GetAllAsync(q.OnlyActive, ct)).Select(SectorResponse.From).ToList();
}

public sealed record CreateSectorCommand(SectorRequest Data) : IRequest<SectorResponse>;

public sealed class CreateSectorHandler(ISectorRepository sectors) : IRequestHandler<CreateSectorCommand, SectorResponse>
{
    public async Task<SectorResponse> Handle(CreateSectorCommand cmd, CancellationToken ct)
    {
        if (await sectors.NameExistsAsync(cmd.Data.Name.Trim(), null, ct))
            throw new ConflictException(ErrorCodes.SectorNameTaken, "A sector with that name already exists.");

        var sector = Sector.Create(cmd.Data.Name, cmd.Data.SortOrder);
        sector.SetActive(cmd.Data.IsActive);
        await sectors.AddAsync(sector, ct);
        await sectors.SaveChangesAsync(ct);
        return SectorResponse.From(sector);
    }
}

public sealed record UpdateSectorCommand(int Id, SectorRequest Data) : IRequest<SectorResponse>;

public sealed class UpdateSectorHandler(ISectorRepository sectors) : IRequestHandler<UpdateSectorCommand, SectorResponse>
{
    public async Task<SectorResponse> Handle(UpdateSectorCommand cmd, CancellationToken ct)
    {
        var sector = await sectors.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(ErrorCodes.SectorNotFound, $"Sector {cmd.Id} not found.");
        if (await sectors.NameExistsAsync(cmd.Data.Name.Trim(), cmd.Id, ct))
            throw new ConflictException(ErrorCodes.SectorNameTaken, "A sector with that name already exists.");

        sector.Rename(cmd.Data.Name);
        sector.SetSortOrder(cmd.Data.SortOrder);
        sector.SetActive(cmd.Data.IsActive);
        await sectors.SaveChangesAsync(ct);
        return SectorResponse.From(sector);
    }
}
