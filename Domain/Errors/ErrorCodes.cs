namespace Domain.Errors;

/// <summary>
/// Stable, machine-readable error codes returned in ProblemDetails (field "code").
/// The front translates them for the user; never change an existing value.
/// </summary>
public static class ErrorCodes
{
    public const string ValidationFailed = "validation.failed";

    public const string InvalidCredentials = "auth.invalid_credentials";
    public const string WrongCurrentPassword = "auth.wrong_current_password";
    public const string Forbidden = "auth.forbidden";

    public const string UserNotFound = "user.not_found";
    public const string UserNameRequired = "user.name_required";
    public const string UserEmailRequired = "user.email_required";
    public const string UserPasswordRequired = "user.password_required";
    public const string UserEmailTaken = "user.email_taken";
    public const string UserRoleNotStaff = "user.role_not_staff";
    public const string UserCannotDemoteSelf = "user.cannot_demote_self";
    public const string UserCannotDeactivateSelf = "user.cannot_deactivate_self";
    public const string UserLastAdmin = "user.last_admin";

    public const string CustomerPhoneRequired = "customer.phone_required";
    public const string CustomerNameRequired = "customer.name_required";

    public const string StoreClosed = "store.closed";
    public const string StoreAtCapacity = "store.at_capacity";
    public const string StoreSettingsInvalid = "store.settings_invalid";

    public const string SectorRequired = "sector.required";
    public const string SectorNameRequired = "sector.name_required";
    public const string SectorNotFound = "sector.not_found";
    public const string SectorNameTaken = "sector.name_taken";

    public const string ProductNotFound = "product.not_found";

    public const string OrderNotFound = "order.not_found";
    public const string OrderEmpty = "order.empty";
    public const string OrderInvalidQuantity = "order.invalid_quantity";
    public const string OrderInvalidTransition = "order.invalid_transition";
    public const string OrderCashTooLow = "order.cash_too_low";
    public const string OrderNotTransfer = "order.not_transfer";
    public const string ReceiptInvalid = "order.receipt_invalid";

    public const string PaymentAccountInvalid = "payment_account.invalid";
    public const string PaymentAccountNotFound = "payment_account.not_found";
    public const string NoPaymentAccounts = "payment_account.none_active";

    public const string UserUsernameRequired = "user.username_required";
    public const string UserUsernameInvalid = "user.username_invalid";
    public const string UserUsernameTaken = "user.username_taken";
}
