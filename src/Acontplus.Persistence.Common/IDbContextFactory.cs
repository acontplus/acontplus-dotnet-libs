namespace Acontplus.Persistence.Common;

public interface IDbContextFactory<out TContext> where TContext : DbContext
{
    TContext GetContext(string contextName);
}
