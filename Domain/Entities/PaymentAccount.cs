using Domain.Errors;
using Domain.Exceptions;

namespace Domain.Entities;

/// <summary>
/// Where customers send transfers: Nequi, Bancolombia, a Bre-B key… The admin uploads the QR the bank app
/// generates for receiving payments; the app shows it with the order total.
/// </summary>
public sealed class PaymentAccount
{
    private PaymentAccount() { }

    public int Id { get; private set; }
    public string Provider { get; private set; } = null!;
    public string HolderName { get; private set; } = null!;
    /// <summary>Phone number, account number or Bre-B key the customer can type manually.</summary>
    public string AccountReference { get; private set; } = null!;
    public string? QrImagePath { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }

    public static PaymentAccount Create(string provider, string holderName, string accountReference, int sortOrder)
    {
        var account = new PaymentAccount();
        account.Update(provider, holderName, accountReference, sortOrder, true);
        return account;
    }

    public void Update(string provider, string holderName, string accountReference, int sortOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(holderName) || string.IsNullOrWhiteSpace(accountReference))
            throw new DomainException(ErrorCodes.PaymentAccountInvalid, "Provider, holder and account are required.");
        Provider = provider.Trim();
        HolderName = holderName.Trim();
        AccountReference = accountReference.Trim();
        SortOrder = sortOrder;
        IsActive = isActive;
    }

    public void SetQrImage(string path) => QrImagePath = path;
}
