namespace Demo.Application.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponse, DomainError>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<LoginResponse, DomainError>> RegisterAsync(LoginRequest request, CancellationToken ct = default);
}
