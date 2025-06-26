using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using OnlineShopping;
using OnlineShopping.DTOs;
using OnlineShopping.Models;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OnlineShopping.Tests.IntegrationTests.Controllers
{
    public class CustomersControllerIntegrationTests : TestBase, IClassFixture<WebApplicationFactory<Program>>
    {
        public CustomersControllerIntegrationTests(WebApplicationFactory<Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task CreateCustomer_ValidData_ReturnsCreatedCustomer()
        {
            // Arrange
            var customer = new CreateCustomerDto
            {
                Name = "John Doe",
                Email = "john@example.com",
                //Phone = "123-456-7890"
            };

            var json = JsonConvert.SerializeObject(customer);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("/api/Customers", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdCustomer = JsonConvert.DeserializeObject<CustomerResponseDto>(responseContent);

            Assert.NotNull(createdCustomer);
            Assert.Equal(customer.Name, createdCustomer.Name);
            Assert.Equal(customer.Email, createdCustomer.Email);
            Assert.True(createdCustomer.Id > 0);
        }

        [Fact]
        public async Task GetCustomer_ExistingId_ReturnsCustomer()
        {
            // Arrange
            var customer = new Customer
            {
                Name = "Jane Doe",
                Email = "jane@example.com",
                //Phone = "098-765-4321"
            };
            DbContext.Customers.Add(customer);
            await DbContext.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync($"/api/Customers/{customer.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var retrievedCustomer = JsonConvert.DeserializeObject<CustomerResponseDto>(responseContent);

            Assert.NotNull(retrievedCustomer);
            Assert.Equal(customer.Name, retrievedCustomer.Name);
            Assert.Equal(customer.Email, retrievedCustomer.Email);
            //Assert.Equal(customer.Phone, retrievedCustomer.Phone);
        }

        [Fact]
        public async Task GetCustomer_NonExistingId_ReturnsNotFound()
        {
            // Act
            var response = await Client.GetAsync("/api/Customers/999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetAllCustomers_WithData_ReturnsAllCustomers()
        {
            // Arrange
            var customers = new List<Customer>
            {
                new Customer { Name = "Customer 1", Email = "customer1@example.com"  },
                new Customer { Name = "Customer 2", Email = "customer2@example.com" },
                new Customer { Name = "Customer 3", Email = "customer3@example.com" }
            };

            DbContext.Customers.AddRange(customers);
            await DbContext.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("/api/Customers");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var retrievedCustomers = JsonConvert.DeserializeObject<List<CustomerResponseDto>>(responseContent);

            Assert.NotNull(retrievedCustomers);
            Assert.Equal(3, retrievedCustomers.Count);
        }

        [Fact]
        public async Task UpdateCustomer_ExistingCustomer_ReturnsNoContent()
        {
            // Arrange
            var customer = new Customer
            {
                Name = "Update Test",
                Email = "update@example.com",
                //Phone = "444-444-4444"
            };
            DbContext.Customers.Add(customer);
            await DbContext.SaveChangesAsync();

            var updateDto = new CreateCustomerDto
            {
                Name = "Updated Name",
                Email = "updated@example.com",
                //Phone = "555-555-5555"
            };

            var json = JsonConvert.SerializeObject(updateDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PutAsync($"/api/Customers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify the update
            var getResponse = await Client.GetAsync($"/api/Customers/{customer.Id}");
            var responseContent = await getResponse.Content.ReadAsStringAsync();
            var updatedCustomer = JsonConvert.DeserializeObject<CustomerResponseDto>(responseContent);

            Assert.Equal(updateDto.Name, updatedCustomer.Name);
            Assert.Equal(updateDto.Email, updatedCustomer.Email);
            //Assert.Equal(updateDto.Phone, updatedCustomer.Phone);
        }

        [Fact]
        public async Task GetAllCustomers_EmptyDatabase_ReturnsEmptyList()
        {
            // Act
            var response = await Client.GetAsync("/api/Customers");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<List<CustomerResponseDto>>(responseContent);

            Assert.NotNull(customers);
            Assert.Empty(customers);
        }
    }
}