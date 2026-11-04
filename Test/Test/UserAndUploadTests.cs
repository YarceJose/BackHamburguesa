using Application.Abstractions;
using Application.Exceptions;
using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using Domain.Exceptions;

namespace Test;

public class UserAndUploadTests
{
    [Fact]
    public void Staff_username_is_normalized_and_email_is_optional()
    {
        var user = User.CreateStaff("Usuario Cocina", "  Cocina ", null, null, "hash", UserRole.Cook);
        Assert.Equal("cocina", user.Username);
        Assert.Null(user.Email);
        Assert.True(user.CanLogin);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("con espacio")]
    [InlineData("ñandú")]
    public void Invalid_usernames_are_rejected(string username) =>
        Assert.Equal(ErrorCodes.UserUsernameInvalid,
            Assert.Throws<DomainException>(() => User.CreateStaff("X Y", username, null, null, "hash", UserRole.Cook)).Code);

    [Fact]
    public async Task Png_receipt_is_accepted()
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0];
        var (content, extension) = await UploadRules.ReadValidatedAsync(new UploadedFile(new MemoryStream(png), png.Length), allowPdf: true, default);
        Assert.Equal(".png", extension);
        Assert.Equal(png.Length, content.Length);
    }

    [Fact]
    public async Task Pdf_only_where_allowed_and_fake_images_rejected()
    {
        byte[] pdf = "%PDF-1.7"u8.ToArray();
        byte[] text = "hola, no soy una imagen"u8.ToArray();

        Assert.Equal(".pdf", (await UploadRules.ReadValidatedAsync(new UploadedFile(new MemoryStream(pdf), pdf.Length), true, default)).Extension);
        await Assert.ThrowsAsync<BusinessRuleException>(() => UploadRules.ReadValidatedAsync(new UploadedFile(new MemoryStream(pdf), pdf.Length), false, default));
        await Assert.ThrowsAsync<BusinessRuleException>(() => UploadRules.ReadValidatedAsync(new UploadedFile(new MemoryStream(text), text.Length), true, default));
    }

    [Fact]
    public async Task Files_over_5_mb_are_rejected() =>
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            UploadRules.ReadValidatedAsync(new UploadedFile(Stream.Null, UploadRules.MaxBytes + 1), true, default));
}

public class SecurityStampTests
{
    private static User Cook() => User.CreateStaff("Usuario Cocina", "cocina", null, null, "hash", UserRole.Cook);

    [Fact]
    public void Deactivating_rotates_the_stamp()
    {
        var user = Cook();
        var before = user.SecurityStamp;
        user.Deactivate();
        Assert.NotEqual(before, user.SecurityStamp);
    }

    [Fact]
    public void New_password_rotates_the_stamp()
    {
        var user = Cook();
        var before = user.SecurityStamp;
        user.ChangePassword("new-hash");
        Assert.NotEqual(before, user.SecurityStamp);
    }

    [Fact]
    public void New_role_rotates_the_stamp_but_same_role_does_not()
    {
        var user = Cook();
        var before = user.SecurityStamp;
        user.ChangeRole(UserRole.Cook);
        Assert.Equal(before, user.SecurityStamp);
        user.ChangeRole(UserRole.Delivery);
        Assert.NotEqual(before, user.SecurityStamp);
    }

    [Fact]
    public void Profile_changes_and_reactivation_keep_the_stamp()
    {
        var user = Cook();
        var before = user.SecurityStamp;
        user.UpdateProfile("Otro Nombre", "cocina", null, "3001234567");
        user.RegisterLogin();
        Assert.Equal(before, user.SecurityStamp);
    }

    [Fact]
    public void Every_user_gets_a_different_stamp() =>
        Assert.NotEqual(Cook().SecurityStamp, Cook().SecurityStamp);
}
