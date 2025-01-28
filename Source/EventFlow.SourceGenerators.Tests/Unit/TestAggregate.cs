using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;

namespace EventFlow.SourceGenerators.Tests.Unit
{
    public class TestAggregate : AggregateRoot<TestAggregate, TestAggregateId>
    {
        public TestAggregate(TestAggregateId id) : base(id)
        {
        }

        public Task DoSomething() => Task.CompletedTask;
    }

    public class TestAggregateId : Identity<TestAggregateId>
    {
        public TestAggregateId(string value) : base(value)
        {
        }
    }

    public class CustomTestEvent : TestEvent
    {
        
    }

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