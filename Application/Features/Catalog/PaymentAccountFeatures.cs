using Application.Abstractions;
using Application.DTOs.Store;
using Application.Exceptions;
using Application.Mediator;
using Domain.Entities;
using Domain.Errors;
using Domain.Interfaces;

namespace Application.Features.Catalog;

// ---------- Public: where to pay ----------
public sealed record GetPaymentAccountsQuery(bool OnlyActive) : IRequest<List<PaymentAccountResponse>>;

public sealed class GetPaymentAccountsHandler(IPaymentAccountRepository accounts)
    : IRequestHandler<GetPaymentAccountsQuery, List<PaymentAccountResponse>>
{
    public async Task<List<PaymentAccountResponse>> Handle(GetPaymentAccountsQuery q, CancellationToken ct) =>
        (await accounts.GetAllAsync(q.OnlyActive, ct)).Select(PaymentAccountResponse.From).ToList();
}

public sealed record GetPaymentQrQuery(int AccountId) : IRequest<StoredFile>;

public sealed class GetPaymentQrHandler(IPaymentAccountRepository accounts, IFileStorage files) : IRequestHandler<GetPaymentQrQuery, StoredFile>
{
    public async Task<StoredFile> Handle(GetPaymentQrQuery q, CancellationToken ct)
    {
        var account = await accounts.GetByIdAsync(q.AccountId, ct);
        if (account is null || !account.IsActive || account.QrImagePath is null)
            throw new NotFoundException(ErrorCodes.PaymentAccountNotFound, "QR not found.");
        return await files.OpenAsync(account.QrImagePath, ct)
            ?? throw new NotFoundException(ErrorCodes.PaymentAccountNotFound, "QR not found.");
    }
}

// ---------- Admin ----------
public sealed record CreatePaymentAccountCommand(PaymentAccountRequest Data) : IRequest<PaymentAccountResponse>;

public sealed class CreatePaymentAccountHandler(IPaymentAccountRepository accounts)
    : IRequestHandler<CreatePaymentAccountCommand, PaymentAccountResponse>
{
    public async Task<PaymentAccountResponse> Handle(CreatePaymentAccountCommand cmd, CancellationToken ct)
    {
        var d = cmd.Data;
        var account = PaymentAccount.Create(d.Provider, d.HolderName, d.AccountReference, d.SortOrder);
        account.Update(d.Provider, d.HolderName, d.AccountReference, d.SortOrder, d.IsActive);
        await accounts.AddAsync(account, ct);
        await accounts.SaveChangesAsync(ct);
        return PaymentAccountResponse.From(account);
    }
}

public sealed record UpdatePaymentAccountCommand(int Id, PaymentAccountRequest Data) : IRequest<PaymentAccountResponse>;

public sealed class UpdatePaymentAccountHandler(IPaymentAccountRepository accounts)
    : IRequestHandler<UpdatePaymentAccountCommand, PaymentAccountResponse>
{
    public async Task<PaymentAccountResponse> Handle(UpdatePaymentAccountCommand cmd, CancellationToken ct)
    {
        var account = await accounts.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(ErrorCodes.PaymentAccountNotFound, $"Payment account {cmd.Id} not found.");
        var d = cmd.Data;
        account.Update(d.Provider, d.HolderName, d.AccountReference, d.SortOrder, d.IsActive);
        await accounts.SaveChangesAsync(ct);
        return PaymentAccountResponse.From(account);
    }
}

/// <summary>The admin uploads the QR their bank app generates for receiving payments.</summary>
public sealed record UploadPaymentQrCommand(int Id, UploadedFile File) : IRequest<PaymentAccountResponse>;

public sealed class UploadPaymentQrHandler(IPaymentAccountRepository accounts, IFileStorage files)
    : IRequestHandler<UploadPaymentQrCommand, PaymentAccountResponse>
{
    public async Task<PaymentAccountResponse> Handle(UploadPaymentQrCommand cmd, CancellationToken ct)
    {
        var account = await accounts.GetByIdAsync(cmd.Id, ct)
            ?? throw new NotFoundException(ErrorCodes.PaymentAccountNotFound, $"Payment account {cmd.Id} not found.");

        var (content, extension) = await UploadRules.ReadValidatedAsync(cmd.File, allowPdf: false, ct);
        await using (content)
        {
            var previous = account.QrImagePath;
            account.SetQrImage(await files.SaveAsync(content, "qr", extension, ct));
            await accounts.SaveChangesAsync(ct);
            if (previous is not null) await files.DeleteAsync(previous, ct);
        }
        return PaymentAccountResponse.From(account);
    }
}
