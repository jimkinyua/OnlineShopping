using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OnlineShopping.Data;
using System;
using System.Net.Http;
using System.Linq;

namespace OnlineShopping.Tests.IntegrationTests
{
    public class TestBase : IDisposable
    {
        protected readonly HttpClient Client;
        private readonly WebApplicationFactory<Program> _factory;
        private readonly IServiceScope _scope;
        protected readonly OrderDbContext DbContext;

        public TestBase(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove all existing DbContext and database provider registrations
                    var dbContextDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<OrderDbContext>));
                    if (dbContextDescriptor != null)
                    {
                        services.Remove(dbContextDescriptor);
                    }

                    // Also remove the DbContext itself
                    var contextDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(OrderDbContext));
                    if (contextDescriptor != null)
                    {
                        services.Remove(contextDescriptor);
                    }

                    // Add InMemory database for testing
                    services.AddDbContext<OrderDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());
                        options.EnableSensitiveDataLogging(); // Optional: for better debugging
                    });
                });

                // Configure logging if needed
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                });
            });

            Client = _factory.CreateClient();
            _scope = _factory.Services.CreateScope();
            DbContext = _scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            // Ensure database is created
            DbContext.Database.EnsureCreated();
        }

        public void Dispose()
        {
            DbContext?.Dispose();
            _scope?.Dispose();
            Client?.Dispose();
            _factory?.Dispose();
        }
    }
}