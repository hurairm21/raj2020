using Microsoft.Extensions.DependencyInjection;


namespace Library.API.Helpers
{
    public static class ServiceCollectionExtension
    {
        //just an ext method of IServiceCOllection interaface to add our nlogger class dependency
        public static void ConfigureLoggerService(this IServiceCollection services)
        {
            services.AddSingleton<ILoggerManager, LoggerManager>();
        }
    }
}
