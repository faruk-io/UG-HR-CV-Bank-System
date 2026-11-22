namespace CvBank.Api.Models;

public record LoginRequest(string UserName, string Password);

public record LoginResponse(
    string Token,
    int UserId,
    string FullName,
    string Role,
    string[] Permissions,
    DateTime ExpiresAtUtc);

public record CurrentUserDto(
    int UserId,
    string UserName,
    string FullName,
    string Role,
    string[] Permissions);
