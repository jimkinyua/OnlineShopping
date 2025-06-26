# Discounting System Documentation

## Overview

The OnlineShopping application now includes a comprehensive discounting system that applies different promotion rules based on customer segments and order history. This system is designed to be flexible, extensible, and easy to manage.

## Key Features

1. **Multiple Discount Types**

   - Percentage discounts
   - Fixed amount discounts
   - Buy X Get Y promotions
   - Free shipping

2. **Flexible Criteria**

   - Customer segment-based (Regular, Premium, VIP)
   - Order count-based (loyalty rewards)
   - Total spent-based (big spender bonuses)
   - First-time customer discounts
   - Minimum order amount requirements
   - Product-specific promotions

3. **Advanced Features**
   - Priority-based discount application
   - Combinable and non-combinable promotions
   - Maximum usage limits per customer
   - Time-based validity (start and end dates)
   - Promotion usage tracking

## Database Structure

### New Tables

1. **PromotionRules** - Stores all promotion configurations
2. **OrderItems** - Tracks individual items in each order
3. **AppliedDiscounts** - Records which discounts were applied to each order

### Modified Tables

1. **Orders** - Now includes SubTotal, TotalDiscount, and ShippingCost fields

## API Endpoints

### Orders Controller

- `POST /api/orders` - Create a new order with automatic discount calculation
- `POST /api/orders/calculate-total` - Preview order total with applicable discounts
- `GET /api/orders/{id}` - Get detailed order information including discounts
- `GET /api/orders/customer/{customerId}/promotions` - Get available promotions for a customer
- `GET /api/orders/customer/{customerId}/statistics` - Get customer order statistics

### Promotions Controller

- `GET /api/promotions` - Get all promotions (with optional activeOnly filter)
- `GET /api/promotions/{id}` - Get specific promotion details
- `POST /api/promotions` - Create a new promotion
- `PUT /api/promotions/{id}` - Update an existing promotion
- `DELETE /api/promotions/{id}` - Delete or deactivate a promotion
- `GET /api/promotions/{id}/usage` - Get usage statistics for a promotion

## Default Promotion Rules

The system comes with 5 pre-configured promotion rules:

1. **VIP 20% Discount** - 20% off for VIP customers
2. **Premium 10% Discount** - 10% off for Premium customers
3. **First Time Customer Discount** - $50 off for first-time customers on orders over $200
4. **Loyal Customer Reward** - 15% off for customers with 5+ previous orders
5. **Big Spender Bonus** - $100 off for customers who have spent over $5000 (one-time use)

## How Discounts Are Applied

1. When an order is created, the system automatically:

   - Identifies applicable promotions based on customer and order criteria
   - Sorts promotions by priority
   - Applies non-combinable promotions (choosing the best one) OR combinable promotions
   - Calculates total discount amount
   - Checks for free shipping eligibility
   - Records all applied discounts for tracking

2. Discount calculation considers:
   - Customer segment
   - Order history (count and total spent)
   - Current order amount
   - Promotion validity dates
   - Usage limits

## Example Usage

### Creating an Order with Discounts

```json
POST /api/orders
{
  "customerId": 1,
  "items": [
    {
      "productId": 1,
      "quantity": 2
    },
    {
      "productId": 2,
      "quantity": 1
    }
  ],
  "shippingCost": 10
}
```

Response will include:

- Subtotal
- Applied discounts with details
- Total discount amount
- Shipping cost (possibly free)
- Final total

### Creating a New Promotion

```json
POST /api/promotions
{
  "name": "Summer Sale",
  "description": "25% off all orders over $100",
  "type": 0,  // PercentageDiscount
  "criteria": 4,  // OrderAmount
  "discountValue": 25,
  "minimumOrderAmount": 100,
  "startDate": "2025-06-01",
  "endDate": "2025-08-31",
  "isActive": true,
  "priority": 1,
  "isCombinable": false
}
```

## Business Rules

1. **Priority System**: Lower priority numbers are applied first
2. **Combinability**: Non-combinable promotions exclude all others; only the best one is applied
3. **Usage Limits**: Promotions can have per-customer usage limits
4. **Validity Period**: Promotions are only active between start and end dates
5. **Soft Delete**: Used promotions are deactivated rather than deleted to maintain order history

## Future Enhancements

The system is designed to support future additions such as:

- Coupon codes
- Product-specific discounts
- Category-based promotions
- Time-of-day promotions
- Bulk purchase discounts
- Referral discounts
