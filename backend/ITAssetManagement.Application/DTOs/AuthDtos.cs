using System.ComponentModel.DataAnnotations;

namespace ITAssetManagement.Application.DTOs;

public sealed record LoginRequestDto(
    [Required, StringLength(100)] string Username,
    [Required, StringLength(256)] string Password);

public sealed record ForgotPasswordRequestDto(
    [Required, EmailAddress, StringLength(254)] string Email);

public sealed record ResetPasswordRequestDto(
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(512)] string Token,
    [Required, MinLength(12), StringLength(128)] string NewPassword);

public sealed record AuthenticatedUserDto(
    int UserId,
    string Username,
    string Email,
    IReadOnlyList<string> Roles);

public sealed record AuthSessionDto(
    AuthenticatedUserDto User,
    Guid SessionId,
    string RefreshToken,
    DateTime ExpiresOn);
