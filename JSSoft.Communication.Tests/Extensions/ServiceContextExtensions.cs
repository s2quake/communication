
namespace JSSoft.Communication.Tests.Extensions;

static class ServiceContextExtensions
{
    public static async Task ReleaseAsync(this IServiceContext @this, Guid token)
    {
        if (@this.ServiceState == ServiceState.Open)
        {
            try
            {
                await @this.CloseAsync(token, CancellationToken.None);
            }
            catch
            {
            }
        }
        if (@this.ServiceState == ServiceState.Faulted)
        {
            await @this.AbortAsync();
        }
    }
}
