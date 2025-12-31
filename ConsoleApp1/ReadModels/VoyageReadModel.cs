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
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel;
using EventFlow.Examples.Shipping.Domain.Model.VoyageModel.Events;
using EventFlow.ReadStores;
using EventFlow.Sql.ReadModels.Attributes;

namespace ConsoleApp1.ReadModels
{
    [Table("ReadModel-Voyage")]
    public class VoyageReadModel : IReadModel,
        IAmReadModelFor<VoyageAggregate, VoyageId, VoyageCreatedEvent>,
        IAmReadModelFor<VoyageAggregate, VoyageId, VoyageScheduleUpdatedEvent>
    {
        [SqlReadModelIdentityColumn]
        public string AggregateId { get; set; }

        [SqlReadModelVersionColumn]
        public int Version { get; set; }

        public string VoyageNumber { get; set; }
        public int ScheduleItemCount { get; set; }
        public bool IsDelayed { get; set; }

        public Task ApplyAsync(
            IReadModelContext context,
            IDomainEvent<VoyageAggregate, VoyageId, VoyageCreatedEvent> domainEvent,
            CancellationToken cancellationToken)
        {
            AggregateId = domainEvent.AggregateIdentity.Value;
            VoyageNumber = domainEvent.AggregateIdentity.Value;
            ScheduleItemCount = domainEvent.AggregateEvent.Schedule.CarrierMovements.Count;
            IsDelayed = false;

            return Task.CompletedTask;
        }

        public Task ApplyAsync(
            IReadModelContext context,
            IDomainEvent<VoyageAggregate, VoyageId, VoyageScheduleUpdatedEvent> domainEvent,
            CancellationToken cancellationToken)
        {
            ScheduleItemCount = domainEvent.AggregateEvent.Schedule.CarrierMovements.Count;
            IsDelayed = true;

            return Task.CompletedTask;
        }
    }
}
