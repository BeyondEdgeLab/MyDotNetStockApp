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

### 3. Get Recent Prices for Multiple Symbols
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

### 4. Get Stock Momentum Analysis (NEW)
```
POST /stocks/momentum
```

Calculates the momentum of a list of stocks over multiple time windows to assess the strength and consistency of price movement. Returns momentum per window and an overall score, sorted by strongest momentum first.

**Request Body:**
```json
{
  "symbols": ["AAPL", "TSLA", "MSFT"],
  "windowsMinutes": [5, 30, 60, 1440],
  "weights": [0.4, 0.3, 0.2, 0.1]
}
```

**Fields:**
- `symbols`: List of stock tickers
- `windowsMinutes`: List of time windows (in minutes) to calculate momentum
- `weights`: Optional weights for each window to compute overall momentum score

**Calculation Logic:**

For each symbol and each window:
```
Momentum = ((CurrentPrice - PriceAtWindowStart) / PriceAtWindowStart) * 100
```

Compute overall weighted score (if weights provided):
```
Score = sum(Momentum_window[i] * weights[i])
```

Results are sorted by highest overall momentum score.

**Response:**
```json
{
  "asOfUtc": "2026-02-11T04:45:00Z",
  "results": [
    {
      "symbol": "TSLA",
      "momentum": {
        "5m": 2.5,
        "30m": 4.1,
        "60m": 5.0,
        "1440m": 8.3
      },
      "score": 4.95
    },
    {
      "symbol": "AAPL",
      "momentum": {
        "5m": 1.2,
        "30m": 2.0,
        "60m": 3.1,
        "1440m": 6.0
      },
      "score": 2.97
    }
  ]
}
```

**Features:**
- Accepts a list of stock symbols
- Configurable multiple time windows (in minutes)
- Optional weights for calculating overall momentum score
- Each momentum value is a percentage
- Results sorted by highest score first
- Newest data (current price) is always used as the endpoint

**Example:**
```bash
curl -X POST http://localhost:5000/stocks/momentum \
  -H "Content-Type: application/json" \
  -d '{
    "symbols": ["AAPL", "TSLA", "MSFT"],
    "windowsMinutes": [5, 30, 60, 1440],
    "weights": [0.4, 0.3, 0.2, 0.1]
  }'
```

### 5. Get Stock Growth Analysis
```
POST /stock/growth
```

Retrieves stock price data for multiple symbols within a configurable time window and calculates percentage growth. Results are sorted by highest percentage growth first, with prices ordered from most recent to oldest.

**Request Body:**
```json
{
  "symbols": ["AAPL", "MSFT", "TSLA"],
  "windowMinutes": 5
}
```

**Response:**
```json
{
  "windowMinutes": 5,
  "asOfUtc": "2026-02-11T04:22:00Z",
  "results": [
    {
      "symbol": "TSLA",
      "startPrice": 192.40,
      "endPrice": 198.10,
      "percentageGrowth": 2.96,
      "prices": [
        {
          "timestamp": "2026-02-11T04:22:00Z",
          "price": 198.10
        },
        {
          "timestamp": "2026-02-11T04:21:00Z",
          "price": 196.80
        },
        {
          "timestamp": "2026-02-11T04:20:00Z",
          "price": 192.40
        }
      ]
    },
    {
      "symbol": "AAPL",
      "startPrice": 178.20,
      "endPrice": 179.10,
      "percentageGrowth": 0.50,
      "prices": [
        {
          "timestamp": "2026-02-11T04:22:00Z",
          "price": 179.10
        },
        {
          "timestamp": "2026-02-11T04:21:00Z",
          "price": 178.60
        },
        {
          "timestamp": "2026-02-11T04:20:00Z",
          "price": 178.20
        }
      ]
    }
  ]
}
```

**Features:**
- Accepts a list of stock symbols
- Configurable time window (in minutes)
- Calculates percentage growth: `((endPrice - startPrice) / startPrice) * 100`
- Results sorted by highest percentage growth first
- Prices within each result sorted by most recent first
- Returns `asOfUtc` timestamp indicating when the data was retrieved
- Handles edge cases (empty data, insufficient price points, division by zero)

**Example:**
```bash
curl -X POST http://localhost:5000/stock/growth \
  -H "Content-Type: application/json" \
  -d '{
    "symbols": ["AAPL", "MSFT", "TSLA"],
    "windowMinutes": 5
  }'
```

### 6. Get Volatility Spikes
```
POST /stocks/volatility/spikes
```

Detect stocks whose short-term volatility has spiked significantly compared to their normal baseline. This helps identify unusual price movements or unstable stocks.

**Request Body:**
```json
{
  "symbols": ["AAPL", "TSLA", "MSFT"],
  "windowMinutes": 5,
  "baselineMinutes": 60,
  "spikeThreshold": 2.0
}
```

**Fields:**
- `symbols`: List of stock tickers
- `windowMinutes`: Short-term volatility window (from NOW backward)
- `baselineMinutes`: Reference window for normal volatility
- `spikeThreshold`: Minimum multiple of baseline volatility to be considered a spike

**Calculation Logic:**

1. Fetch price data for each symbol from now - baselineMinutes to now
2. Compute short-term volatility for the last windowMinutes using standard deviation of log returns:
   ```
   return_t = ln(price_t / price_t-1)
   shortTermVolatility = std(return_t)
   ```
3. Compute baseline volatility using the rest of the data:
   ```
   baselineVolatility = std(return_t from (now - baselineMinutes) to (now - windowMinutes))
   ```
4. Calculate spike factor:
   ```
   spikeFactor = shortTermVolatility / baselineVolatility
   ```
5. Filter: only include stocks where spikeFactor >= spikeThreshold
6. Sort: by highest spikeFactor first

**Response:**
```json
{
  "windowMinutes": 5,
  "baselineMinutes": 60,
  "asOfUtc": "2026-02-11T04:50:00Z",
  "results": [
    {
      "symbol": "TSLA",
      "shortTermVolatility": 0.0042,
      "baselineVolatility": 0.0015,
      "spikeFactor": 2.8,
      "priceChangePercent": 3.1
    },
    {
      "symbol": "AAPL",
      "shortTermVolatility": 0.0021,
      "baselineVolatility": 0.0010,
      "spikeFactor": 2.1,
      "priceChangePercent": 1.4
    }
  ]
}
```

**Notes:**
- `priceChangePercent = ((current - start) / start) * 100`
- Excludes stocks with insufficient data or baseline volatility = 0
- Endpoint works from current time backward

**Features:**
- Detects unusual volatility spikes in stock prices
- Uses log returns for better statistical properties
- Configurable time windows and spike threshold
- Returns only stocks that exceed the spike threshold
- Results sorted by highest spike factor first

**Example:**
```bash
curl -X POST http://localhost:5000/stocks/volatility/spikes \
  -H "Content-Type: application/json" \
  -d '{
    "symbols": ["AAPL", "TSLA", "MSFT"],
    "windowMinutes": 5,
    "baselineMinutes": 60,
    "spikeThreshold": 2.0
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
