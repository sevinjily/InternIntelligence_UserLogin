using Business.Abstract;
using Business.Concrete;
using Business.Message.Abstract;
using Business.Message.Concrete;
using Business.Results.Abstract;
using Business.Results.Concrete;
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
            services.AddScoped<IMessageService, MessageService>();
        }
        }
}
