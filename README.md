# Flight Information System

## Опис

Система складається з двох підсистем:

1. **FlightStorageService** – бекенд (ASP.NET Core Web API), який зберігає та надає дані про авіарейси.
2. **FlightClientApp** – фронтенд (ASP.NET MVC/Razor Pages), який надає простий веб-інтерфейс для пошуку рейсів через REST API.

Взаємодія з базою даних відбувається через **ADO.NET** (SqlConnection, SqlCommand) та **збережені процедури** у MS SQL Server.

## Технології

- .NET 9 (C# 11)
- ASP.NET Core Web API
- ASP.NET MVC / Razor Pages
- ADO.NET
- MS SQL Server 2019+
- Swagger (Swashbuckle)
- Bootstrap (UI)

## Структура репозиторію

FlightInfoSystem/
├── FlightStorageService/ # Web API
├── FlightClientApp/ # MVC клієнт
├─ db/
│ └── init.sql # SQL-скрипт створення БД та збережених процедур
├── README.md
└── .gitignore
## База даних

- Назва: `FlightsDb`
- Таблиця: `dbo.Flights`

| Поле                 | Тип            | Опис                           |
|-----------------------|---------------|--------------------------------|
| FlightNumber (PK)     | NVARCHAR(10)  | Унікальний номер рейсу         |
| DepartureDateTime     | DATETIME2     | Дата і час вильоту             |
| DepartureAirportCity  | NVARCHAR(100) | Місто/аеропорт вильоту         |
| ArrivalAirportCity    | NVARCHAR(100) | Місто/аеропорт прильоту        |
| DurationMinutes       | INT           | Тривалість польоту у хвилинах  |

Обмеження: дані доступні лише для найближчих 7 днів.

## Ендпоінти API

- `GET /api/flights/{flightNumber}` – пошук рейсу за номером
- `GET /api/flights?date={yyyy-MM-dd}` – всі рейси на задану дату
- `GET /api/flights/departure?city={city}&date={yyyy-MM-dd}` – рейси, що вилітають з міста
- `GET /api/flights/arrival?city={city}&date={yyyy-MM-dd}` – рейси, що прилітають у місто

## Запуск

### 1. База даних
Виконати SQL-скрипт `db/init.sql` у MS SQL Server.

### 2–3. Запуск FlightStorageService і FlightClientApp
```bash
# Запуск бекенду
cd FlightStorageService
dotnet restore
dotnet build
dotnet run
# API буде доступне за адресою: https://localhost:8081/swagger

# Запуск фронтенду
cd .FlightClientApp
dotnet restore
dotnet build
dotnet run
# UI буде доступне за адресою: https://localhost:8082
