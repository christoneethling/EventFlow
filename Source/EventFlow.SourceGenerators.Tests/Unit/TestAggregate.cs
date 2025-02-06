using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;

namespace EventFlow.SourceGenerators.Tests.Unit
{
    [AggregateExtensions]
    public class TestAggregate : AggregateRoot<TestAggregate, TestAggregateId>,
        IEmit<CustomTestEvent>
    {
        public TestAggregate(TestAggregateId id) : base(id) { }

        public void Apply(CustomTestEvent aggregateEvent) { }

        public Task DoSomething()
        {
            Emit(new CustomTestEvent());
            return Task.CompletedTask;
        }
    }

    public class TestAggregateId : Identity<TestAggregateId>
    {
        public TestAggregateId(string value) : base(value) { }
    }

    public class CustomTestEvent : TestAggregateEvent { }

    public class TestSubscribers : ISubscribeSynchronousTo<CustomTestEvent>
    {
        public Task HandleAsync(
            IDomainEvent<TestAggregate, TestAggregateId, CustomTestEvent> domainEvent,
            CancellationToken cancellationToken)
        {
            // Func
            return Task.CompletedTask;
        }
    }
}