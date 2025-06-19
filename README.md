# Simple Microservices Architecture

This project demonstrates a simple microservices architecture using .NET 8, consisting of three services:

## Services

### 1. Product Service (Port 5001)
- Manages product information
- Endpoints:
  - GET /products - List all products
  - GET /products/{id} - Get product by ID
  - POST /products - Create new product

### 2. Order Service (Port 5002)
- Handles order processing
- Endpoints:
  - POST /order - Create new order

### 3. API Gateway (Port 7000)
- Routes requests to appropriate services using YARP
- Routes:
  - /products/* → Product Service
  - /order/* → Order Service

## Technology Stack
- .NET 8
- Entity Framework Core
- SQL Server
- YARP (Yet Another Reverse Proxy)

## Setup Instructions

1. Prerequisites:
   - .NET 8 SDK
   - SQL Server
   - Visual Studio 2022 or VS Code

2. Database Setup:
   ```bash
   # For Product Service
   cd ProductService
   dotnet ef database update

   # For Order Service
   cd ../OrderService
   dotnet ef database update
   ```

3. Running the Services:
   ```bash
   # Start Product Service
   cd ProductService
   dotnet run

   # Start Order Service
   cd ../OrderService
   dotnet run

   # Start API Gateway
   cd ../ApiGateway
   dotnet run
   ```

## Architecture
- Each service has its own database
- Services communicate through the API Gateway
- Windows Authentication is used for SQL Server connections
- Entity Framework Core is used for data access 