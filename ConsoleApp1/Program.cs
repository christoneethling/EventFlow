// The MIT License (MIT)
// 
// Copyright (c) 2015-2025 Rasmus Mikkelsen
// https://github.com/eventflow/EventFlow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using ConsoleApp1.QueryHandlers;
using ConsoleApp1.ReadModels;
using EventFlow;
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Core;
using EventFlow.Examples.Shipping;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Commands;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Queries;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.ValueObjects;
using EventFlow.Examples.Shipping.Domain.Model.LocationModel;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel.Commands;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel.ValueObjects;
using EventFlow.Extensions;
using EventFlow.Queries;
using EventFlow.SQLite.Connections;
using EventFlow.SQLite.Extensions;
using Microsoft.Extensions.DependencyInjection;

var databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EventFlowShipping.sqlite");

Console.WriteLine($"EventFlow Shipping Example - SQLite POC");
Console.WriteLine($"Database: {databasePath}");
Console.WriteLine();

// Ensure database file exists
if (File.Exists(databasePath))
    File.Delete(databasePath);

using (File.Create(databasePath)) { }
Console.WriteLine("Created new database file");


// Configure EventFlow with SQLite and Shipping domain
var serviceProvider = EventFlowOptions.New()
    .ConfigureSQLite(SQLiteConfiguration.New.SetConnectionString($"Data Source={databasePath};Version=3;"))
    .UseSQLiteEventStore()
    .ConfigureShippingDomain()
    .UseSQLiteReadModel<VoyageReadModel>()
    .UseSQLiteReadModel<CargoReadModel>()
    .UseSQLiteReadModel<LocationReadModel>()
    .RegisterServices(sr => sr.AddTransient<IQueryHandler<GetCargosDependentOnVoyageQuery, IReadOnlyCollection<Cargo>>, GetCargosDependentOnVoyageQueryHandler>())
    .ServiceCollection
    .BuildServiceProvider();

// Create the EventFlow table in SQLite
var connection = serviceProvider.GetRequiredService<ISQLiteConnection>();
const string sqlCreateTable = @"
    CREATE TABLE IF NOT EXISTS [EventFlow](
        [GlobalSequenceNumber] [INTEGER] PRIMARY KEY ASC NOT NULL,
        [BatchId] [uniqueidentifier] NOT NULL,
        [AggregateId] [nvarchar](255) NOT NULL,
        [AggregateName] [nvarchar](255) NOT NULL,
        [Data] [nvarchar](1024) NOT NULL,
        [Metadata] [nvarchar](1024) NOT NULL,
        [AggregateSequenceNumber] [int] NOT NULL
    )";
const string sqlCreateIndex = @"
    CREATE UNIQUE INDEX IF NOT EXISTS [IX_EventFlow_AggregateId_AggregateSequenceNumber] ON [EventFlow]
    (
        [AggregateId] ASC,
        [AggregateSequenceNumber] ASC
    )";

// Create read model tables
const string sqlCreateVoyageTable = @"
    CREATE TABLE IF NOT EXISTS [ReadModel-Voyage](
        [Id] [INTEGER] PRIMARY KEY ASC,
        [AggregateId] [nvarchar](64) NOT NULL,
        [Version] INTEGER,
        [VoyageNumber] [nvarchar](64) NOT NULL,
        [ScheduleItemCount] [int] NOT NULL,
        [IsDelayed] [bit] NOT NULL
    )";

const string sqlCreateCargoTable = @"
    CREATE TABLE IF NOT EXISTS [ReadModel-Cargo](
        [Id] [INTEGER] PRIMARY KEY ASC,
        [AggregateId] [nvarchar](64) NOT NULL,
        [Version] INTEGER,
        [CargoId] [nvarchar](64) NOT NULL,
        [OriginLocationId] [nvarchar](64) NOT NULL,
        [DestinationLocationId] [nvarchar](64) NOT NULL,
        [ArrivalDeadline] [nvarchar](64) NOT NULL,
        [DependentVoyageIds] [nvarchar](512),
        [ItineraryJson] [nvarchar](2048)
    )";

const string sqlCreateLocationTable = @"
    CREATE TABLE IF NOT EXISTS [ReadModel-Location](
        [Id] [INTEGER] PRIMARY KEY ASC,
        [AggregateId] [nvarchar](64) NOT NULL,
        [Version] INTEGER,
        [LocationId] [nvarchar](64) NOT NULL,
        [Name] [nvarchar](255) NOT NULL
    )";

await connection.ExecuteAsync(Label.Named("create-table"), string.Empty, CancellationToken.None, sqlCreateTable, null);
await connection.ExecuteAsync(Label.Named("create-index"), string.Empty, CancellationToken.None, sqlCreateIndex, null);
await connection.ExecuteAsync(Label.Named("create-voyage-table"), string.Empty, CancellationToken.None, sqlCreateVoyageTable, null);
await connection.ExecuteAsync(Label.Named("create-cargo-table"), string.Empty, CancellationToken.None, sqlCreateCargoTable, null);
await connection.ExecuteAsync(Label.Named("create-location-table"), string.Empty, CancellationToken.None, sqlCreateLocationTable, null);

var commandBus = serviceProvider.GetRequiredService<ICommandBus>();
var aggregateStore = serviceProvider.GetRequiredService<IAggregateStore>();

Console.WriteLine("EventFlow configured successfully");
Console.WriteLine("Database schema created (EventFlow + Read Models)");
Console.WriteLine();

try
{
    // Step 1: Create Locations
    Console.WriteLine("Step 1: Creating Locations...");
    var tokyoId = new LocationId("JNTKO");
    var helsinkiId = new LocationId("FIHEL");
    var dallasId = new LocationId("USDAL");
    var hamburgId = new LocationId("DEHAM");
    var stockholmId = new LocationId("SESTO");

    await CreateLocationAsync(aggregateStore, tokyoId, "Tokyo");
    await CreateLocationAsync(aggregateStore, helsinkiId, "Helsinki");
    await CreateLocationAsync(aggregateStore, dallasId, "Dallas");
    await CreateLocationAsync(aggregateStore, hamburgId, "Hamburg");
    await CreateLocationAsync(aggregateStore, stockholmId, "Stockholm");
    Console.WriteLine("✓ Locations created");
    Console.WriteLine();

    // Step 2: Create a Voyage
    Console.WriteLine("Step 2: Creating Voyage (Dallas to Helsinki)...");
    var voyageId = new VoyageId("0300A");
    var schedule = new ScheduleBuilder(dallasId)
        .Add(hamburgId, new DateTime(2008, 10, 29, 3, 30, 0), new DateTime(2008, 10, 31, 14, 0, 0))
        .Add(stockholmId, new DateTime(2008, 11, 1, 15, 20, 0), new DateTime(2008, 11, 1, 18, 40, 0))
        .Add(helsinkiId, new DateTime(2008, 11, 2, 9, 0, 0), new DateTime(2008, 11, 2, 11, 15, 0))
        .Build();

    await commandBus.PublishAsync(new VoyageCreateCommand(voyageId, schedule), CancellationToken.None);
    Console.WriteLine($"✓ Voyage {voyageId} created");
    Console.WriteLine();

    // Step 3: Delay the Voyage
    Console.WriteLine("Step 3: Delaying Voyage by 7 days...");
    var delay = TimeSpan.FromDays(7);
    await commandBus.PublishAsync(new VoyageDelayCommand(voyageId, delay), CancellationToken.None);
    Console.WriteLine($"✓ Voyage {voyageId} delayed by {delay.TotalDays} days");
    Console.WriteLine();

    // Step 4: Book Cargo
    Console.WriteLine("Step 4: Booking Cargo (Tokyo to Helsinki)...");
    var cargoId = CargoId.New;
    var route = new Route(
        tokyoId,
        helsinkiId,
        new DateTime(2008, 10, 1, 11, 0, 0),
        new DateTime(2008, 11, 6, 12, 0, 0));

    await commandBus.PublishAsync(new CargoBookCommand(cargoId, route), CancellationToken.None);
    Console.WriteLine($"✓ Cargo {cargoId} booked from Tokyo to Helsinki");
    Console.WriteLine();

    Console.WriteLine("SUCCESS! All commands executed successfully.");
    Console.WriteLine($"Data persisted to: {databasePath}");
    Console.WriteLine();

    // Step 5: Query Read Models
    Console.WriteLine("==========================================");
    Console.WriteLine("QUERYING READ MODELS FROM SQLITE");
    Console.WriteLine("==========================================");
    Console.WriteLine();

    // Query Locations
    Console.WriteLine("📍 Locations:");
    var locations = await connection.QueryAsync<LocationReadModel>(
        Label.Named("query-locations"),
        string.Empty,
        CancellationToken.None,
        "SELECT * FROM [ReadModel-Location]",
        null);
    foreach (var location in locations)
    {
        Console.WriteLine($"  • {location.Name} ({location.LocationId})");
    }
    Console.WriteLine();

    // Query Voyages
    Console.WriteLine("🚢 Voyages:");
    var voyages = await connection.QueryAsync<VoyageReadModel>(
        Label.Named("query-voyages"),
        string.Empty,
        CancellationToken.None,
        "SELECT * FROM [ReadModel-Voyage]",
        null);
    foreach (var voyage in voyages)
    {
        Console.WriteLine($"  • Voyage {voyage.VoyageNumber}");
        Console.WriteLine($"    - Schedule Items: {voyage.ScheduleItemCount}");
        Console.WriteLine($"    - Delayed: {voyage.IsDelayed}");
    }
    Console.WriteLine();

    // Query Cargo
    Console.WriteLine("📦 Cargo:");
    var cargos = await connection.QueryAsync<CargoReadModel>(
        Label.Named("query-cargo"),
        string.Empty,
        CancellationToken.None,
        "SELECT * FROM [ReadModel-Cargo]",
        null);
    foreach (var cargo in cargos)
    {
        Console.WriteLine($"  • Cargo {cargo.CargoId}");
        Console.WriteLine($"    - From: {cargo.OriginLocationId}");
        Console.WriteLine($"    - To: {cargo.DestinationLocationId}");
        Console.WriteLine($"    - Deadline: {cargo.ArrivalDeadline}");
    }
    Console.WriteLine();

    Console.WriteLine("==========================================");
    Console.WriteLine("END-TO-END DEMO COMPLETE!");
    Console.WriteLine("Events stored → Read models updated → Queried successfully");
    Console.WriteLine("==========================================");
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
finally
{
    if (serviceProvider is IDisposable disposable)
    {
        disposable.Dispose();
    }
}

static async Task CreateLocationAsync(IAggregateStore aggregateStore, LocationId locationId, string name)
{
    await aggregateStore.UpdateAsync<LocationAggregate, LocationId>(
        locationId,
        SourceId.New,
        (aggregate, cancellationToken) =>
        {
            aggregate.Create(name);
            return Task.CompletedTask;
        },
        CancellationToken.None);
    Console.WriteLine($"  - Created location: {name} ({locationId})");
}
