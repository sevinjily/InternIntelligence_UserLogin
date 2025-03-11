using Business.Abstract;
using Business.Concrete;
using Business.Utilities.Message.Abstract;
using Business.Utilities.Message.Concrete;
using Business.Utilities.Security.Abstract;
using Business.Utilities.Security.Concrete;
using DataAccess.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Business.DependencyResolver
{
    public static class ServiceRegistration
    {
        public static void AddBusinessService(this IServiceCollection services)
        {
            //IoC-inversion of Control
            services.AddScoped<AppDbContext>();
            services.AddScoped<IAuthService, AuthManager>();
            services.AddScoped<ITokenService,TokenManager>();
            services.AddScoped<IMessageService, MessageService>();

        }
    }
}
