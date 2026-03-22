namespace Demo.Application.Interfaces;

public interface ICustomerService
{
    Task<Result<CustomerDto, DomainErrors>> GetByIdCardAsync(string idCard);
}
