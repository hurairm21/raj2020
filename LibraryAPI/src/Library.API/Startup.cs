using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Library.API.Services;
using Library.API.Entities;
using Microsoft.EntityFrameworkCore;
using Library.API.Helpers;
using Library.API.Models;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Diagnostics;
using NLog.Extensions.Logging;

namespace Library.API
{
    public class Startup
    {
        public static IConfiguration Configuration;

        public Startup(IConfiguration configuration)
        {
            //nlog setting
            //LogManager.LoadConfiguration(String.Concat(Directory.GetCurrentDirectory(),"/nlog.config"));

            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            #region "add nlogger service dependency"
            //adding logger dependency using extension method
            ServiceCollectionExtension.ConfigureLoggerService(services);

            //we can also use below line
            //services.AddSingleton<ILoggerManager, LoggerManager>();
            #endregion

            #region "XmlInput Output formatter"
            //Adding support for xml input formatter and output as xml foter
            //Reason of adding XmlDataContractSerializerOutputFormatter as xml input formatter 
            //is bcoz it provides datetimeoffset option while sending input
            services.AddMvc(setupAction =>
            {
                setupAction.ReturnHttpNotAcceptable = true;
                setupAction.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
                setupAction.InputFormatters.Add(new XmlDataContractSerializerInputFormatter());
            });
            #endregion

            // register the DbContext on the container, getting the connection string from
            // appSettings (note: use this during development; in a production environment,
            // it's better to store the connection string in an environment variable)
            var connectionString = Configuration["connectionStrings:libraryDBConnectionString"];
            services.AddDbContext<LibraryContext>(o => o.UseSqlServer(connectionString));

            // register the repository
            services.AddScoped<ILibraryRepository, LibraryRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env,
            ILoggerFactory loggerFactory, LibraryContext libraryContext)
        {
            loggerFactory.AddConsole();
            //not supprted inthis version I think
            //loggerFactory.AddDebug();
            loggerFactory.AddNLog();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                /*RequestDelegate rd = new RequestDelegate(async context =>
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("");
                });*/


                app.UseExceptionHandler(appBuilder =>
                {
                    #region "logging code snippet"
                    //appBuilder.Run(rd); // as declared above OR below piece of code
                    appBuilder.Run(async context =>
                    {
                        /*context.Features gives the collection of http features provided 
                        by server middleware aavailable on this request*/
                        //but we want only ExceptionHandlerFeature
                        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                        if (exceptionHandlerFeature != null)
                        {
                            var logger = loggerFactory.CreateLogger("Global exception logger");
                            logger.LogError(500, exceptionHandlerFeature.Error, exceptionHandlerFeature.Error.Message);
                            }

                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("An unexpected fault happened. Try again later");
                    });
                    #endregion
                });
            }
            #region"Automapper config"
            AutoMapper.Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<Entities.Author, Models.AuthorDto>()
                //in destn object name has to be concatenation of Fisrt Name & Last Name, so we use FOrMember method
                .ForMember(destn => destn.Name, opt => opt.MapFrom(src =>
                    $"{src.FirstName} {src.LastName}"))
                //Age is calculated using GetCurrentAge extn method, so we pass src.DOB to extn method
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src =>
                    src.DateOfBirth.GetCurrentAge()));


                cfg.CreateMap<Entities.Book, Models.BookDto>();

                //For POST we map author input values to correct author obj
                cfg.CreateMap<Models.AuthorForCreationDto, Entities.Author>();

                cfg.CreateMap<BookForCreationDto, Book>();
                cfg.CreateMap<BookForUpdateDto, Book>();
                cfg.CreateMap<Book, BookForUpdateDto>();

            });
            #endregion
            libraryContext.EnsureSeedDataForContext();


            app.UseMvc();
        }
    }
}
