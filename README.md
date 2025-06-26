# OnlineShopping - E-commerce Order Management System

## Project Overview

This is a RESTful API for an online shopping platform built with ASP.NET Core. The system provides a complete e-commerce backend solution with features for managing customers, orders, products, and promotions with a "sophisticated" discounting system.

### Key Features

- **Customer Management**: Support for different customer segments (Regular, Premium, VIP)
- **Order Processing**: Complete order lifecycle management with status tracking
- **Product Catalog**: Basic product management with pricing
- **Advanced Discounting System**: Flexible promotion rules with multiple criteria and discount types
- **Order Status Workflow**: Validated order status transitions with full history tracking
- **RESTful API**: Well-documented API with Swagger/OpenAPI integration
- **Data Persistence**: SQLite database with Entity Framework Core migrations

## Technical Architecture

### Technology Stack

- **Framework**: ASP.NET Core 6.0+ Web API
- **Database**: SQLite with Entity Framework Core
- **ORM**: Entity Framework Core with Code-First approach
- **API Documentation**: Swagger/OpenAPI with enhanced descriptions
- **Testing**: xUnit test framework with comprehensive test coverage
- **Caching**: In-memory caching for performance optimization

### Project Structure

```
OnlineShopping/
├── Controllers/          # API endpoints
├── Data/                # Database context and configurations
├── DTOs/                # Data Transfer Objects
├── Models/              # Domain entities
├── Services/            # Business logic layer
├── Migrations/          # EF Core database migrations
├── Docs/                # Additional documentation
└── Tests/               # Unit and integration tests
```

## Core Components

### 1. Customer Management

- Three customer segments: Regular, Premium, and VIP
- Customer creation with automatic timestamp
- Order history tracking per customer

### 2. Order Management

- Complete order lifecycle: Pending → Processing → Shipped → Delivered
- Order cancellation at appropriate stages
- Automatic order number generation
- Support for multiple items per order
- Real-time total calculation with discounts

### 3. Discounting System

The system includes a sophisticated promotion engine with:

#### Discount Types

- **Percentage Discounts**: Apply a percentage off the total
- **Fixed Amount Discounts**: Apply a fixed dollar amount off
- **Buy X Get Y**: Future support planned
- **Free Shipping**: Automatic shipping cost waiver

#### Promotion Criteria

- **Customer Segment**: Different discounts for Regular/Premium/VIP
- **Order History**: Rewards based on previous order count
- **Total Spent**: Discounts for high-value customers
- **First-Time Customer**: Special offers for new customers
- **Minimum Order Amount**: Threshold-based promotions

#### Pre-configured Promotions

1. VIP customers receive 20% off all orders
2. Premium customers receive 10% off all orders
3. First-time customers get $50 off orders over $200
4. Customers with 5+ orders receive 15% off
5. Customers who've spent $5000+ get a one-time $100 discount

### 4. Order Status Management

- **Status Flow Validation**: Prevents invalid status transitions
- **Full History Tracking**: Every status change is recorded with timestamp and user
- **Stale Order Detection**: Identifies orders that haven't been updated recently
- **Status Statistics**: Real-time analytics with caching

## Design Approach

### 1. Clean Architecture

- **Separation of Concerns**: Controllers handle HTTP, Services contain business logic, Data layer manages persistence
- **Dependency Injection**: All services are injected, making the system testable and maintainable
- **DTOs**: Separate data models for API communication to prevent over-exposure of domain entities

### 2. Domain-Driven Design Elements

- **Rich Domain Models**: Entities contain business logic where appropriate
- **Value Objects**: Enums for CustomerSegment, OrderStatus, PromotionType
- **Aggregates**: Order serves as an aggregate root with OrderItems

### 3. RESTful API Design

- **Resource-Based URLs**: Clear, predictable endpoint patterns
- **HTTP Verbs**: Proper use of GET, POST, PUT, DELETE
- **Status Codes**: Appropriate HTTP status codes for different scenarios
- **Consistent Response Format**: Standardized DTOs for responses

### 4. Database Design

- **Normalized Structure**: Proper relationships between entities
- **Audit Trail**: Status history and applied discounts are tracked
- **Soft Deletes**: Promotions are deactivated rather than deleted
- **Optimistic Concurrency**: Handled by Entity Framework

### 5. Performance Considerations

- **Caching**: Order statistics cached to reduce database load
- **Eager Loading**: Strategic use of Include() to prevent N+1 queries
- **Async Operations**: All database operations are asynchronous
- **Database Indexes**: Applied on frequently queried columns

## Key Assumptions

### Business Assumptions

1. **Customer Segments**: Customers are pre-assigned to segments (Regular/Premium/VIP) - segment assignment logic is handled externally
2. **Discount Stacking**: Multiple combinable discounts can be applied to a single order
3. **Discount Priority**: Lower priority numbers are processed first; non-combinable discounts exclude all others
4. **Order Immutability**: Once created, order items and amounts cannot be modified - only status can change
5. **Promotion Validity**: Promotions are checked at order creation time; subsequent promotion changes don't affect existing orders

### Technical Assumptions

1. **Single Currency**: All monetary values are in USD (currency conversion not implemented)
2. **No Authentication**: Authentication/authorization middleware is not implemented (would be added in production)
3. **Synchronous Processing**: Order processing is synchronous (async message queues could be added for scale)
4. **Local Time**: All timestamps use UTC (timezone handling would be needed for global deployment)
5. **English Only**: No internationalization/localization support

### Data Assumptions

1. **Product Availability**: Inventory management is not implemented - all products are assumed available
2. **Pricing Stability**: Product prices are fixed at order creation time
3. **Customer Data**: Minimal customer information is stored (GDPR compliance would require enhancement)
4. **Payment Processing**: Payment gateway integration is not included
5. **Shipping Calculation**: Shipping costs are provided by the client, not calculated

### Running Tests

```bash
cd OnlineShopping.Tests
./run-tests.sh
```
