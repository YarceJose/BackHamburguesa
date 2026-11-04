namespace Domain.Enums;

/// <summary>
/// Store roles.
/// Admin, Cook and Delivery are staff: they have credentials and sign in from Front-Hamburguesa.
/// Customer does NOT register or sign in; it is only stored to be linked to orders.
/// </summary>
public enum UserRole
{
    Admin = 1,
    Cook = 2,
    Delivery = 3,
    Customer = 4
}

public static class UserRoleExtensions
{
    public static bool IsStaff(this UserRole role) => role is UserRole.Admin or UserRole.Cook or UserRole.Delivery;
}
