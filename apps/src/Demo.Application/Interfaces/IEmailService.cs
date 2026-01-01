namespace Demo.Application.Interfaces;

public interface IEmailService
{
    public Task<DataTable> GetAsync(int quantity);
    public Task<int> UpdateAsync(int id, string estado, string? msgError = null);
}
