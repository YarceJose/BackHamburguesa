using Application.Abstractions;
using Application.DTOs.Store;
using Application.Exceptions;
using Application.Mediator;
using Domain.Entities;
using Domain.Errors;
using Domain.Interfaces;

namespace Application.Features.Catalog;

// ---------- Admin: list (active and hidden) ----------
public sealed record GetAdminProductsQuery : IRequest<List<AdminProductResponse>>;

public sealed class GetAdminProductsHandler(IProductRepository products)
    : IRequestHandler<GetAdminProductsQuery, List<AdminProductResponse>>
{
    public async Task<List<AdminProductResponse>> Handle(GetAdminProductsQuery q, CancellationToken ct) =>
        (await products.GetAllAsync(ct)).Select(AdminProductResponse.From).ToList();
}

// ---------- Admin: create ----------
public sealed record CreateProductCommand(ProductRequest Data) : IRequest<AdminProductResponse>;

public sealed class CreateProductHandler(IProductRepository products) : IRequestHandler<CreateProductCommand, AdminProductResponse>
{
    public async Task<AdminProductResponse> Handle(CreateProductCommand cmd, CancellationToken ct)
    {
        var d = cmd.Data;
        await ProductRules.EnsureNameFreeAsync(products, d.Name, null, ct);

        var product = Product.CreateMenuItem(d.Name, d.Price, d.Category, d.Description, d.ImageUrl);
        product.SetActive(d.IsActive);
        await products.AddAsync(product, ct);
        await products.SaveChangesAsync(ct);
        return AdminProductResponse.From(product);
    }
}

// ---------- Admin: edit (also hides or shows it in the menu) ----------
public sealed record UpdateProductCommand(int Id, ProductRequest Data) : IRequest<AdminProductResponse>;

public sealed class UpdateProductHandler(IProductRepository products) : IRequestHandler<UpdateProductCommand, AdminProductResponse>
{
    public async Task<AdminProductResponse> Handle(UpdateProductCommand cmd, CancellationToken ct)
    {
        var product = await ProductRules.FindAsync(products, cmd.Id, ct);
        var d = cmd.Data;
        await ProductRules.EnsureNameFreeAsync(products, d.Name, cmd.Id, ct);

        product.UpdateMenuItem(d.Name, d.Price, d.Category, d.Description, d.ImageUrl, d.IsActive);
        await products.SaveChangesAsync(ct);
        return AdminProductResponse.From(product);
    }
}

// ---------- Admin: picture ----------
public sealed record UploadProductImageCommand(int Id, UploadedFile File) : IRequest<AdminProductResponse>;

public sealed class UploadProductImageHandler(IProductRepository products, IFileStorage files)
    : IRequestHandler<UploadProductImageCommand, AdminProductResponse>
{
    public async Task<AdminProductResponse> Handle(UploadProductImageCommand cmd, CancellationToken ct)
    {
        var product = await ProductRules.FindAsync(products, cmd.Id, ct);

        var (content, extension) = await UploadRules.ReadValidatedAsync(cmd.File, allowPdf: false, ct, ErrorCodes.ProductImageInvalid);
        await using (content)
        {
            var previous = product.ImagePath;
            product.SetImage(await files.SaveAsync(content, "products", extension, ct));
            await products.SaveChangesAsync(ct);
            if (previous is not null) await files.DeleteAsync(previous, ct);
        }
        return AdminProductResponse.From(product);
    }
}

public sealed record RemoveProductImageCommand(int Id) : IRequest<AdminProductResponse>;

public sealed class RemoveProductImageHandler(IProductRepository products, IFileStorage files)
    : IRequestHandler<RemoveProductImageCommand, AdminProductResponse>
{
    public async Task<AdminProductResponse> Handle(RemoveProductImageCommand cmd, CancellationToken ct)
    {
        var product = await ProductRules.FindAsync(products, cmd.Id, ct);
        var previous = product.ImagePath;
        if (previous is not null)
        {
            product.SetImage(null);
            await products.SaveChangesAsync(ct);
            await files.DeleteAsync(previous, ct);
        }
        return AdminProductResponse.From(product);
    }
}

// ---------- Public: the uploaded picture ----------
public sealed record GetProductImageQuery(int Id) : IRequest<StoredFile>;

public sealed class GetProductImageHandler(IProductRepository products, IFileStorage files) : IRequestHandler<GetProductImageQuery, StoredFile>
{
    public async Task<StoredFile> Handle(GetProductImageQuery q, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(q.Id, ct);
        if (product?.ImagePath is null)
            throw new NotFoundException(ErrorCodes.ProductNotFound, "Image not found.");
        return await files.OpenAsync(product.ImagePath, ct)
            ?? throw new NotFoundException(ErrorCodes.ProductNotFound, "Image not found.");
    }
}

internal static class ProductRules
{
    public static async Task<Product> FindAsync(IProductRepository products, int id, CancellationToken ct) =>
        await products.GetByIdAsync(id, ct)
        ?? throw new NotFoundException(ErrorCodes.ProductNotFound, $"Product {id} not found.");

    public static async Task EnsureNameFreeAsync(IProductRepository products, string name, int? excludeId, CancellationToken ct)
    {
        if (await products.NameExistsAsync(name.Trim(), excludeId, ct))
            throw new ConflictException(ErrorCodes.ProductNameTaken, "A product with that name already exists.");
    }
}
