namespace Domain.Enums;

/// <summary>
/// Roles of the people who sign in. Customers are not users: their name, phone and sector
/// are stored only in each order.
/// </summary>
public enum UserRole
{
    Admin = 1,
    Cook = 2,
    Delivery = 3
}

public static class UserRoleExtensions
{
    public static bool IsStaff(this UserRole role) => role is UserRole.Admin or UserRole.Cook or UserRole.Delivery;
}
