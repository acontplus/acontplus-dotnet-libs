namespace Demo.Application.Dtos;

public record LoginRequest(string Username, string Password);

public record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    int UserId,
    string Username,
    string Email,
    int RoleId);
