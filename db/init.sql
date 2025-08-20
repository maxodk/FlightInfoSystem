/* =========================
   Flight Information System
   DB + Table + Stored Procs
   ========================= */

-- 0) CREATE DATABASE (idempotent)
IF DB_ID(N'FlightsDb') IS NULL
BEGIN
    CREATE DATABASE FlightsDb;
END
GO

ALTER DATABASE FlightsDb SET RECOVERY SIMPLE;
GO

USE FlightsDb;
GO

-- 1) TABLE dbo.Flights (idempotent)
IF OBJECT_ID(N'dbo.Flights', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Flights
    (
        FlightNumber         NVARCHAR(10)  NOT NULL, -- PK
        DepartureDateTime    DATETIME2(0)  NOT NULL,
        DepartureAirportCity NVARCHAR(100) NOT NULL,
        ArrivalAirportCity   NVARCHAR(100) NOT NULL,
        DurationMinutes      INT           NOT NULL,
        Status BIT NOT NULL CONSTRAINT DF_YourTableName_Status DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_YourTableName_CreatedAtUtc DEFAULT SYSUTCDATETIME()
        

        CONSTRAINT PK_Flights PRIMARY KEY CLUSTERED (FlightNumber),
        CONSTRAINT CK_Flights_Duration_Positive CHECK (DurationMinutes > 0),
        CONSTRAINT CK_Flights_Cities_NotEmpty CHECK (
            LEN(LTRIM(RTRIM(DepartureAirportCity))) > 0 AND
            LEN(LTRIM(RTRIM(ArrivalAirportCity))) > 0
        )
    );
END
GO

-- 2) Helpful indexes for queries by date/city
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Flights_DepartureDate')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Flights_DepartureDate
        ON dbo.Flights (DepartureDateTime);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Flights_DepartureCity_Date')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Flights_DepartureCity_Date
        ON dbo.Flights (DepartureAirportCity, DepartureDateTime);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Flights_ArrivalCity_Date')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Flights_ArrivalCity_Date
        ON dbo.Flights (ArrivalAirportCity, DepartureDateTime);
END
GO

/* 3) STORED PROCEDURES
   -------------------- */

-- 3.1 AddFlight: Inserts only if DepartureDateTime is within [now, now+7 days)
IF OBJECT_ID(N'dbo.AddFlight', N'P') IS NOT NULL
    DROP PROCEDURE dbo.AddFlight;
GO
CREATE PROCEDURE dbo.AddFlight
    @FlightNumber         NVARCHAR(10),
    @DepartureDateTime    DATETIME2(0),
    @DepartureAirportCity NVARCHAR(100),
    @ArrivalAirportCity   NVARCHAR(100),
    @DurationMinutes      INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Basic validation
        IF @FlightNumber IS NULL OR LTRIM(RTRIM(@FlightNumber)) = N''
            THROW 50001, 'FlightNumber is required.', 1;

        IF @DepartureAirportCity IS NULL OR LTRIM(RTRIM(@DepartureAirportCity)) = N''
            THROW 50002, 'DepartureAirportCity is required.', 1;

        IF @ArrivalAirportCity IS NULL OR LTRIM(RTRIM(@ArrivalAirportCity)) = N''
            THROW 50003, 'ArrivalAirportCity is required.', 1;

        IF @DurationMinutes IS NULL OR @DurationMinutes <= 0
            THROW 50004, 'DurationMinutes must be > 0.', 1;

        DECLARE @nowUtc DATETIME2(0) = SYSUTCDATETIME();
        DECLARE @maxUtc DATETIME2(0) = DATEADD(DAY, 7, @nowUtc);

        IF @DepartureDateTime < @nowUtc OR @DepartureDateTime >= @maxUtc
            THROW 50005, 'DepartureDateTime must be within the next 7 days (UTC).', 1;

        -- Prevent duplicates
        IF EXISTS (SELECT 1 FROM dbo.Flights WHERE FlightNumber = @FlightNumber)
            THROW 50006, 'Flight with the same FlightNumber already exists.', 1;

        -- Insert new flight with Status = 1 and CreatedAtUtc = current UTC
        INSERT INTO dbo.Flights
        (
            FlightNumber, DepartureDateTime, DepartureAirportCity,
            ArrivalAirportCity, DurationMinutes, Status, CreatedAtUtc
        )
        VALUES
        (
            @FlightNumber, @DepartureDateTime, LTRIM(RTRIM(@DepartureAirportCity)),
            LTRIM(RTRIM(@ArrivalAirportCity)), @DurationMinutes, 1, SYSUTCDATETIME()
        );

        -- Return inserted row
        SELECT f.*
        FROM dbo.Flights AS f
        WHERE f.FlightNumber = @FlightNumber;

    END TRY
    BEGIN CATCH
        DECLARE @msg NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @num INT = ERROR_NUMBER();
        DECLARE @state INT = ERROR_STATE();
        RAISERROR(@msg, 16, 1);
        RETURN @num;
    END CATCH
END
GO

-- 3.2 GetFlightByNumber
IF OBJECT_ID(N'dbo.GetFlightByNumber', N'P') IS NOT NULL
    DROP PROCEDURE dbo.GetFlightByNumber;
GO
CREATE PROCEDURE dbo.GetFlightByNumber
    @FlightNumber NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT f.*
    FROM dbo.Flights AS f
    WHERE f.FlightNumber = @FlightNumber;
END
GO

-- 3.3 GetFlightsByDate
IF OBJECT_ID(N'dbo.GetFlightsByDate', N'P') IS NOT NULL
    DROP PROCEDURE dbo.GetFlightsByDate;
GO
CREATE PROCEDURE dbo.GetFlightsByDate
    @Date DATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Using cast to DATE as per spec
    SELECT f.*
    FROM dbo.Flights AS f
    WHERE CAST(f.DepartureDateTime AS DATE) = @Date
    ORDER BY f.DepartureDateTime ASC, f.FlightNumber ASC;
END
GO

-- 3.4 GetFlightsByDepartureCityAndDate
IF OBJECT_ID(N'dbo.GetFlightsByDepartureCityAndDate', N'P') IS NOT NULL
    DROP PROCEDURE dbo.GetFlightsByDepartureCityAndDate;
GO
CREATE PROCEDURE dbo.GetFlightsByDepartureCityAndDate
    @City NVARCHAR(100),
    @Date DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT f.*
    FROM dbo.Flights AS f
    WHERE f.DepartureAirportCity = @City
      AND CAST(f.DepartureDateTime AS DATE) = @Date
    ORDER BY f.DepartureDateTime ASC, f.FlightNumber ASC;
END
GO

-- 3.5 GetFlightsByArrivalCityAndDate
IF OBJECT_ID(N'dbo.GetFlightsByArrivalCityAndDate', N'P') IS NOT NULL
    DROP PROCEDURE dbo.GetFlightsByArrivalCityAndDate;
GO
CREATE PROCEDURE dbo.GetFlightsByArrivalCityAndDate
    @City NVARCHAR(100),
    @Date DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT f.*
    FROM dbo.Flights AS f
    WHERE f.ArrivalAirportCity = @City
      AND CAST(f.DepartureDateTime AS DATE) = @Date
    ORDER BY f.DepartureDateTime ASC, f.FlightNumber ASC;
END
GO

-- 3.6 (Optional) CleanupOldFlights: delete departures before now (UTC)
IF OBJECT_ID(N'dbo.CleanupOldFlights', N'P') IS NOT NULL
    DROP PROCEDURE dbo.CleanupOldFlights;
GO
CREATE PROCEDURE dbo.CleanupOldFlights
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Flights
    SET Status = 0
    WHERE DepartureDateTime < DATEADD(DAY, -7, SYSUTCDATETIME());
END
GO



/* 5) Seed data for quick manual testing (within next 7 days)
      We generate a couple of flights relative to SYSUTCDATETIME().
      You can comment this block out after first run. */
DECLARE @now DATETIME2(0) = SYSUTCDATETIME();
DECLARE @dt1 DATETIME2(0) = DATEADD(HOUR, 6, @now);
DECLARE @dt2 DATETIME2(0) = DATEADD(HOUR, 2, DATEADD(DAY, 1, @now));
DECLARE @dt3 DATETIME2(0) = DATEADD(HOUR, 5, DATEADD(DAY, 1, @now));
DECLARE @dt4 DATETIME2(0) = DATEADD(HOUR, 3, DATEADD(DAY, 2, @now));

EXEC dbo.AddFlight
    @FlightNumber = N'PS101',
    @DepartureDateTime = @dt1,
    @DepartureAirportCity = N'Kyiv',
    @ArrivalAirportCity = N'Warsaw',
    @DurationMinutes = 95;

EXEC dbo.AddFlight
    @FlightNumber = N'LH250',
    @DepartureDateTime = @dt2,
    @DepartureAirportCity = N'Kyiv',
    @ArrivalAirportCity = N'Frankfurt',
    @DurationMinutes = 150;

EXEC dbo.AddFlight
    @FlightNumber = N'LO702',
    @DepartureDateTime = @dt3,
    @DepartureAirportCity = N'Warsaw',
    @ArrivalAirportCity = N'Kyiv',
    @DurationMinutes = 100;

EXEC dbo.AddFlight
    @FlightNumber = N'FR900',
    @DepartureDateTime = @dt4,
    @DepartureAirportCity = N'Krakow',
    @ArrivalAirportCity = N'Paris',
    @DurationMinutes = 140;

GO

/* 6) Example manual calls:

-- One flight by number
EXEC dbo.GetFlightByNumber @FlightNumber = N'PS101';

-- All flights on a specific date (UTC date of tomorrow)
DECLARE @tomorrow DATE = CAST(DATEADD(DAY, 1, SYSUTCDATETIME()) AS DATE);
EXEC dbo.GetFlightsByDate @Date = @tomorrow;

-- By departure city + date
EXEC dbo.GetFlightsByDepartureCityAndDate @City = N'Kyiv', @Date = @tomorrow;

-- By arrival city + date
EXEC dbo.GetFlightsByArrivalCityAndDate @City = N'Kyiv', @Date = @tomorrow;

-- Cleanup old flights (if you need)
-- EXEC dbo.CleanupOldFlights;

*/
