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

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.Events;
using EventFlow.Examples.Shipping.Domain.Model.CargoModel.ValueObjects;
using EventFlow.Examples.Shipping.Domain.Model.LocationModel;
using EventFlow.ReadStores;
using EventFlow.Sql.ReadModels.Attributes;

namespace ConsoleApp1.ReadModels
{
    [Table("ReadModel-Cargo")]
    public class CargoReadModel : IReadModel,
        IAmReadModelFor<CargoAggregate, CargoId, CargoBookedEvent>,
        IAmReadModelFor<CargoAggregate, CargoId, CargoItinerarySetEvent>
    {
        [SqlReadModelIdentityColumn]
        public string AggregateId { get; set; }

        [SqlReadModelVersionColumn]
        public int Version { get; set; }

        public string CargoId { get; set; }
        public string OriginLocationId { get; set; }
        public string DestinationLocationId { get; set; }
        public string ArrivalDeadline { get; set; }
        public string DependentVoyageIds { get; set; }
        public string ItineraryJson { get; set; }

        public Task ApplyAsync(
            IReadModelContext context,
            IDomainEvent<CargoAggregate, CargoId, CargoBookedEvent> domainEvent,
            CancellationToken cancellationToken)
        {
            AggregateId = domainEvent.AggregateIdentity.Value;
            CargoId = domainEvent.AggregateIdentity.Value;
            OriginLocationId = domainEvent.AggregateEvent.Route.OriginLocationId.Value;
            DestinationLocationId = domainEvent.AggregateEvent.Route.DestinationLocationId.Value;
            ArrivalDeadline = domainEvent.AggregateEvent.Route.ArrivalDeadline.ToString("yyyy-MM-dd HH:mm:ss");

            return Task.CompletedTask;
        }

        public Task ApplyAsync(
            IReadModelContext context,
            IDomainEvent<CargoAggregate, CargoId, CargoItinerarySetEvent> domainEvent,
            CancellationToken cancellationToken)
        {
            var itinerary = domainEvent.AggregateEvent.Itinerary;
            
            if (itinerary != null)
            {
                var voyageIds = new List<string>();
                foreach (var leg in itinerary.TransportLegs)
                {
                    voyageIds.Add(leg.VoyageId.Value);
                }
                
                DependentVoyageIds = string.Join(",", voyageIds);
                ItineraryJson = JsonSerializer.Serialize(itinerary);
            }

            return Task.CompletedTask;
        }

        public Cargo ToCargo()
        {
            var route = new Route(
                new LocationId(OriginLocationId),
                new LocationId(DestinationLocationId),
                DateTime.Parse(ArrivalDeadline),
                DateTime.Parse(ArrivalDeadline));

            Itinerary itinerary = null;
            if (!string.IsNullOrEmpty(ItineraryJson))
            {
                itinerary = JsonSerializer.Deserialize<Itinerary>(ItineraryJson);
            }

            return new Cargo(
                new CargoId(CargoId),
                route,
                itinerary);
        }
    }
}
