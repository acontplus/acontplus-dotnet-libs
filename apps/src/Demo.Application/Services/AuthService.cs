using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Acontplus.Utilities.Security.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Demo.Application.Services;

/// <summary>
/// Handles credential validation and JWT token issuance.
/// The token embeds UserId, Username, Email and RoleId claims so that
/// <c>HttpAuditContext</c> can automatically stamp every auditable entity
/// on any subsequent CRUD operation without extra application-layer code.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IRepository<Usuario> _users;
    private readonly IUnitOfWork _uow;
    private readonly IPasswordSecurityService _password;
    private readonly IConfiguration _config;

    public AuthService(
        IUnitOfWork uow,
        IPasswordSecurityService password,
        IConfiguration config)
    {
        _uow = uow;
        _users = uow.GetRepository<Usuario>();
        _password = password;
        _config = config;
    }

    /// <inheritdoc />
    public async Task<Result<LoginResponse, DomainError>> LoginAsync(
        LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetFirstOrDefaultAsync(
            u => u.Username == request.Username && !u.IsDeleted,
            cancellationToken: ct);

        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
            return DomainError.Unauthorized("INVALID_CREDENTIALS", "Invalid username or password.");

        if (!_password.VerifyPassword(request.Password, user.PasswordHash))
            return DomainError.Unauthorized("INVALID_CREDENTIALS", "Invalid username or password.");

        return BuildResponse(user);
    }

    /// <inheritdoc />
    public async Task<Result<LoginResponse, DomainError>> RegisterAsync(
        LoginRequest request, CancellationToken ct = default)
    {
        var exists = await _users.GetFirstOrDefaultAsync(
            u => u.Username == request.Username, cancellationToken: ct);

        if (exists is not null)
            return DomainError.Conflict("USERNAME_EXISTS", $"Username '{request.Username}' is already taken.");

        var user = new Usuario
        {
            Id = 0,
            Username = request.Username,
            Email = $"{request.Username}@demo.local",
            PasswordHash = _password.HashPassword(request.Password),
            RoleId = 1,
        };

        await _users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);  // audit fields filled automatically by BaseContext

        return BuildResponse(user);
    }

    // ------------------------------------------------------------------

    private Result<LoginResponse, DomainError> BuildResponse(Usuario user)
    {
        var (token, expiresIn) = GenerateJwt(user);
        return Result<LoginResponse, DomainError>.Success(new LoginResponse(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: expiresIn,
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email,
            RoleId: user.RoleId));
    }

    private (string Token, int ExpiresInSeconds) GenerateJwt(Usuario user)
    {
        var issuer = _config["JwtSettings:Issuer"]!;
        var audience = _config["JwtSettings:Audience"]!;
        var key = _config["JwtSettings:SecurityKey"]!;
        var expiryMinutes = int.Parse(_config["JwtSettings:ExpiryMinutes"] ?? "60");

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        // These exact claim names are what IUserContext + HttpAuditContext read:
        //   ClaimTypes.NameIdentifier → UserId
        //   ClaimTypes.Name           → UserName
        //   ClaimTypes.Email          → Email
        //   "userRoleId"              → UserRoleId
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("userRoleId", user.RoleId.ToString()),
        };

        var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expiryMinutes * 60);
    }
}
