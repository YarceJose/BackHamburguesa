namespace Domain.Enums;

/// <summary>
/// Roles de la tienda.
/// Admin, Cook y Delivery son personal: tienen credenciales e ingresan desde Front-Hamburguesa.
/// Customer es el cliente: NO se registra ni inicia sesión, solo se guarda para asociarlo a pedidos.
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
