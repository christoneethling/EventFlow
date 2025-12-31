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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Examples.Shipping.Domain.Model.LocationModel;
using EventFlow.Examples.Shipping.Domain.Model.LocationModel.Events;
using EventFlow.ReadStores;
using EventFlow.Sql.ReadModels.Attributes;

namespace ConsoleApp1.ReadModels
{
    [Table("ReadModel-Location")]
    public class LocationReadModel : IReadModel,
        IAmReadModelFor<LocationAggregate, LocationId, LocationCreatedEvent>
    {
        [SqlReadModelIdentityColumn]
        public string AggregateId { get; set; }

        [SqlReadModelVersionColumn]
        public int Version { get; set; }

        public string LocationId { get; set; }
        public string Name { get; set; }

        public Task ApplyAsync(
            IReadModelContext context,
            IDomainEvent<LocationAggregate, LocationId, LocationCreatedEvent> domainEvent,
            CancellationToken cancellationToken)
        {
            AggregateId = domainEvent.AggregateIdentity.Value;
            LocationId = domainEvent.AggregateIdentity.Value;
            Name = domainEvent.AggregateEvent.Name;

            return Task.CompletedTask;
        }
    }
}
