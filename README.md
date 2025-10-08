# ElasticAPI

[![.NET](https://img.shields.io/badge/.NET-5.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/5.0)
[![Elasticsearch](https://img.shields.io/badge/Elasticsearch-7.x-yellow.svg)](https://www.elastic.co/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

A healthcare data management API built with ASP.NET Core that integrates with Elasticsearch for efficient patient and examination data indexing, searching, and filtering.

## Features

- **Data Indexing**: Automatically imports healthcare data from SQL Server to Elasticsearch
- **Advanced Filtering**: Filter patients by multiple clinical criteria including:
  - Laboratory test results
  - Serology markers
  - Non-invasive diagnostic tests
  - Vaccination records
  - Clinical examinations
- **Patient Search**: Search and retrieve patient data by examination IDs
- **RESTful API**: Clean endpoints for data manipulation and retrieval

## Prerequisites

- .NET 5.0 SDK
- SQL Server
- Elasticsearch 7.x
- Visual Studio 2019+ or VS Code

## Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd ElasticAPI
   ```

2. **Configure connection strings**
   Update `appsettings.json` with your database and Elasticsearch URLs:
   ```json
   {
     "ConnectionStrings": {
       "SqlServer": "Server=.;Database=YourDatabase;Integrated Security=True;",
       "Elasticsearch": "http://localhost:9200"
     }
   }
   ```

3. **Install dependencies**
   ```bash
   dotnet restore
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

The API will be available at `https://localhost:5001` with Swagger documentation for testing endpoints.

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/ElasticMain/trigger` | Index all medical data to Elasticsearch |
| GET | `/ElasticMain/patients` | Retrieve all patients |
| POST | `/ElasticMain/search` | Search patients by examination IDs |
| POST | `/ElasticMain/filter` | Filter patients by clinical criteria |

## Usage Example

**Filter patients by clinical criteria:**
```json
POST /ElasticMain/filter
{
  "biopcy": ["hai", "fibrosis"],
  "laboratoryFindings": ["marker1", "marker2"],
  "vaccinationInfo": ["vaccine1", "vaccine2"]
}
```

## Project Structure

- `Controllers/` - API controllers
- `Services/ElasticService/` - Elasticsearch integration services
- `Services/LogicService/` - Business logic services
- `Models/` - Data transfer objects and request models
- `Enums/` - Application enumerations

## Technologies Used

- **ASP.NET Core 5.0** - Web API framework
- **NEST** - Elasticsearch .NET client
- **Dapper** - Micro ORM for database operations
- **Entity Framework Core** - ORM framework
- **Swagger** - API documentation
