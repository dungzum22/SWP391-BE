# SWP391-BE - Flower Shop Platform

A backend API for a flower shop e-commerce platform built with ASP.NET Core 8.0 and Entity Framework Core.

## Project Overview

This project provides a RESTful API for a flower shop e-commerce platform. It handles user authentication, product management, shopping cart functionality, order processing, and payment integration.

## Technology Stack

- **Framework**: ASP.NET Core 8.0- **ORM**: Entity Framework Core
- **Database**: SQL Server- **API Documentation**: Swagger/OpenAPI
- **Authentication**: JWT-based authentication

## Features

- User authentication and authorization- Product catalog management
- Shopping cart functionality- Order processing
- Payment integration- Admin dashboard

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- SQL Server- Visual Studio 2022 (recommended)

### Installation

1. Clone the repository:
   ```
   git clone https://github.com/dungzum22/SWP391-BE.git
   ```
2. Navigate to the project directory:
   ```
   cd SWP391-BE
   ```
3. Restore dependencies:
   ```
   dotnet restore
   ```
4. Update the database connection string in `PlatformFlower/appsettings.json` if needed:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Data Source=(local);Initial Catalog=Flowershop;Persist Security Info=True;User ID=sa;Password=123;Encrypt=True;Trust Server Certificate=True"
   }
   ```
5. Apply database migrations:
   ```
   dotnet ef database update
   ```
6. Run the application:
   ```
   dotnet run --project PlatformFlower
   ```
7. Access the API:
   - HTTP: http://localhost:5116
   - HTTPS: https://localhost:7274
   - Swagger UI: https://localhost:7274/swagger

## Project Structure

- **PlatformFlower/**: Main project directory  - **Controllers/**: API endpoints
  - **Models/**: Data models and DTOs  - **Services/**: Business logic implementation
  - **Data/**: Database context and configurations  - **Middleware/**: Custom middleware components
  - **Program.cs**: Application entry point and configuration

## API Documentation

API documentation is available through Swagger UI when running the application. Navigate to `/swagger` to view and test the available endpoints.

## Database Schema

The database includes the following main entities:- Users

- Products- Categories
- Orders- OrderItems
- Carts- Payments

## Error Handling

The application uses a global exception handler middleware to provide consistent error responses across all endpoints. API responses follow a standardized format:

```json
{  "success": true/false,
  "message": "Operation result message",  "data": { /* Response data */ },
  "errors": { /* Error details if any */ }}
```

## Contributing

Interested in contributing? Check out our [CONTRIBUTING.md](CONTRIBUTING.md) to find resources around contributing along with a guide on how to set up a development environment.
Join our amazing community as a code contributor, and help accelerate!

<!-- This section will automatically display contributors' GitHub avatars when hosted on GitHub -->

<a href="https://github.com/dungzum22/SWP391-BE/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=dungzum22/SWP391-BE" />
</a>
## License
This project is licensed under the MIT License - see the LICENSE file for details.
## Contact

Project Link: [https://github.com/dungzum22/SWP391-BE](https://github.com/dungzum22/SWP391-BE)
