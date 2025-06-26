using Microsoft.EntityFrameworkCore;
using OnlineShopping.Data;
using OnlineShopping.Services;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace OnlineShopping
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<OrderDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Add memory caching
            builder.Services.AddMemoryCache();

            // custom services
            builder.Services.AddScoped<IDiscountService, DiscountService>();
            builder.Services.AddScoped<IOrderManagement, OrderManagement>();
            builder.Services.AddScoped<ICustomerManagement, CustomerManagement>();
            builder.Services.AddScoped<IOrderStatusService, OrderStatusService>();
            builder.Services.AddScoped<IOrderStatusTransitionValidator, OrderStatusTransitionValidator>();
            builder.Services.AddScoped<OrderStatusTransitionValidator>(); // Also register the concrete type for DI

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // Enhanced Swagger configuration
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Online Shopping API",
                    Version = "v1",
                    Description = "A comprehensive RESTful API for managing online shopping operations including customers, orders, and promotions with an advanced discounting system.",
                    Contact = new OpenApiContact
                    {
                        Name = "Online Shopping Team",
                        Email = "support@onlineshopping.com",
                        Url = new Uri("https://www.onlineshopping.com"),
                    },
                    License = new OpenApiLicense
                    {
                        Name = "MIT License",
                        Url = new Uri("https://opensource.org/licenses/MIT"),
                    }
                });

                // Enable annotations
                c.EnableAnnotations();

                // Enable XML comments
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

                // Add common API responses
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                        },
                        new List<string>()
                    }
                });
            });

            var app = builder.Build();

            // Ensure database is created before the program runs
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                //dbContext.Database.EnsureCreated();
                dbContext.Database.Migrate();
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Online Shopping API V1");
                    c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
                    c.DocumentTitle = "Online Shopping API Documentation";
                });
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
