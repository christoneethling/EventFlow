using System.Threading;
using System.Threading.Tasks;
using EventFlow.TestHelpers;
using NUnit.Framework;

namespace EventFlow.SourceGenerators.Tests.Unit
{
    public class AggregateSourceGeneratorTests : IntegrationTest
    {
        protected override IEventFlowOptions Options(IEventFlowOptions eventFlowOptions)
        {
            eventFlowOptions.AddEvents(new[] { typeof(CustomTestEvent) });

            return base.Options(eventFlowOptions);
        }

        [Test]
        public async Task SourceGeneratorWorks()
        {
            // Arrange
            var id = TestAggregateId.New;
            var ct = CancellationToken.None;

            // Act
            await AggregateStore.UpdateAsync(id, a => a.DoSomething(), ct);

            // Assert
            var events = await EventStore.LoadEventsAsync(id, ct);
            Assert.That(events, Has.Count.EqualTo(1));
        }
    }
}
