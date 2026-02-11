# Stock App API

A .NET 9.0 Web API for retrieving stock price data from Yahoo Finance.

## Endpoints

### 1. Get Stock Prices for a Date Range
```
GET /stock/{symbol}?startDate={date}&endDate={date}
```

Retrieves daily stock prices for a specific symbol within a date range.

**Parameters:**
- `symbol` (path): Stock symbol (e.g., AAPL, MSFT)
- `startDate` (query, optional): Start date (defaults to 30 days ago)
- `endDate` (query, optional): End date (defaults to today)

**Example:**
```bash
curl "http://localhost:5000/stock/AAPL?startDate=2024-01-01&endDate=2024-01-31"
```

### 2. Get Recent Prices for Single Symbol
```
GET /stock/{symbol}/recent?minutes={minutes}
```

Retrieves intraday stock prices for a specific symbol within a time window.

**Parameters:**
- `symbol` (path): Stock symbol (e.g., AAPL, MSFT)
- `minutes` (query, optional): Time window in minutes (defaults to 5)

**Example:**
```bash
curl "http://localhost:5000/stock/AAPL/recent?minutes=5"
```

### 3. Get Recent Prices for Multiple Symbols (NEW)
```
POST /stock/recent
```

Retrieves recent intraday stock prices for multiple symbols within a specified time window. All timestamps are in UTC format.

**Request Body:**
```json
{
  "symbols": ["AAPL", "MSFT", "GOOGL"],
  "windowMinutes": 5
}
```

**Response:**
```json
[
  {
    "symbol": "AAPL",
    "prices": [
      {
        "price": 150.25,
        "timestamp": "2024-02-11T14:35:00Z"
      },
      {
        "price": 150.20,
        "timestamp": "2024-02-11T14:34:00Z"
      }
    ]
  },
  {
    "symbol": "MSFT",
    "prices": [
      {
        "price": 380.50,
        "timestamp": "2024-02-11T14:35:00Z"
      }
    ]
  }
]
```

**Features:**
- Accepts a list of stock symbols
- Configurable time window (in minutes)
- Returns prices with DateTimeOffset in UTC
- Prices are sorted by most recent first for each symbol
- Handles errors gracefully (returns empty array for symbols with no data)

**Example:**
```bash
curl -X POST http://localhost:5000/stock/recent \
  -H "Content-Type: application/json" \
  -d '{
    "symbols": ["AAPL", "MSFT", "GOOGL"],
    "windowMinutes": 5
  }'
```

## Building and Running

### Prerequisites
- .NET 9.0 SDK

### Build
```bash
dotnet build
```

### Run
```bash
dotnet run
```

The API will be available at `http://localhost:5000` (or the port specified in launchSettings.json).

### Swagger UI
When running in Development or Production mode, Swagger UI is available at:
```
http://localhost:5000/swagger
```

## Data Source
This application uses the Yahoo Finance API to retrieve stock price data.
