using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace EventFlow.SourceGenerators
{
    [Generator]
    public class AggregateSourceGenerator : IIncrementalGenerator
    {
        private const string ClassPostfix = "Aggregate";

        private const string SourceCodeTemplate =
        """
        using System;
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;
        using EventFlow.Aggregates;
        using EventFlow.Aggregates.ExecutionResults;
        using EventFlow.Core;
        using EventFlow.Subscribers;

        namespace {0}
        {{
            /// <summary>
            /// A base class for all events of the <see cref="{1}Aggregate"/>
            /// </summary>
            public abstract class {1}Event :
                IAggregateEvent<{1}Aggregate, {1}AggregateId>
            {{
            }}

            /// <summary>
            /// An interface for synchronous subscribers of the <see cref="{1}Aggregate"/>
            /// </summary>
            /// <typeparam name="TEvent">The type of the event</typeparam>
            public interface ISubscribeSynchronousTo<TEvent> :
                ISubscribeSynchronousTo<{1}Aggregate, {1}AggregateId, TEvent>
                where TEvent : {1}Event
            {{
            }}

            /// <summary>
            /// An interface for asynchronous subscribers of the <see cref="{1}Aggregate"/>
            /// </summary>
            /// <typeparam name="TEvent">The type of the event</typeparam>
            public interface ISubscribeAsynchronousTo<TEvent> :
                ISubscribeAsynchronousTo<{1}Aggregate, {1}AggregateId, TEvent>
                where TEvent : {1}Event
            {{
            }}
            
            /// <summary>
            /// Provides extension methods for the <see cref="{1}Aggregate"/>
            /// </summary>
            public static class AggregateStoreExtensions
            {{
                /// <summary>
                /// Updates the <see cref="{1}Aggregate"/>
                /// </summary>
                /// <param name="aggregateStore">Aggregate store</param>
                /// <param name="id">ID of the aggregate</param>
                /// <param name="update">Update function</param>
                /// <param name="cancellationToken">Cancellation token</param>
                /// <returns>A task that return a collection of domain events that happened during the update</returns>
                public static Task<IReadOnlyCollection<IDomainEvent>> UpdateAsync(
                    this IAggregateStore aggregateStore,
                    {1}AggregateId id,
                    Func<{1}Aggregate, Task> update,
                    CancellationToken cancellationToken) =>
                        aggregateStore.UpdateAsync<{1}Aggregate, {1}AggregateId>(
                            id,
                            SourceId.New,
                            (aggregate, _) => update(aggregate),
                            cancellationToken);

                /// <summary>
                /// Loads the <see cref="{1}Aggregate"/>
                /// </summary>
                /// <param name="aggregateStore">Aggregate store</param>
                /// <param name="id">ID of the aggregate</param>
                /// <param name="cancellationToken">Cancellation token</param>
                /// <returns>A task that returns the <see cref="{1}Aggregate"/></returns>
                public static Task<{1}Aggregate> LoadAsync(
                    this IAggregateStore aggregateStore,
                    {1}AggregateId id,
                    CancellationToken cancellationToken) =>
                        aggregateStore.LoadAsync<{1}Aggregate, {1}AggregateId>(
                            id,
                            cancellationToken);

                /// <summary>
                /// Updates the <see cref="{1}Aggregate"/>
                /// </summary>
                /// <typeparam name="TExecutionResult">The type of the execution result</typeparam>
                /// <param name="aggregateStore">Aggregate store</param>
                /// <param name="id">ID of the aggregate</param>
                /// <param name="update">Update function</param>
                /// <param name="cancellationToken">Cancellation token</param>
                /// <returns>A task that returns an <see cref="IAggregateUpdateResult{{TExecutionResult}}"/></returns>
                public static Task<IAggregateUpdateResult<TExecutionResult>> UpdateAsync<TExecutionResult>(
                    this IAggregateStore aggregateStore,
                    {1}AggregateId id,
                    Func<{1}Aggregate, Task<TExecutionResult>> update,
                    CancellationToken cancellationToken)
                        where TExecutionResult : IExecutionResult =>
                        aggregateStore.UpdateAsync<{1}Aggregate, {1}AggregateId, TExecutionResult>(
                            id,
                            SourceId.New,
                            (aggregate, _) => update(aggregate),
                            cancellationToken);

                /// <summary>
                /// Stores the <see cref="{1}Aggregate"/>
                /// </summary>
                /// <param name="aggregate">Aggregate to store</param>
                /// <param name="cancellationToken">Cancellation token</param>
                /// <returns>A task that return a collection of domain events that happened during the update</returns>
                public static Task<IReadOnlyCollection<IDomainEvent>> StoreAsync(
                    this IAggregateStore aggregateStore,
                    {1}Aggregate aggregate,
                    CancellationToken cancellationToken) =>
                        aggregateStore.StoreAsync<{1}Aggregate, {1}AggregateId>(
                            aggregate,
                            SourceId.New,
                            cancellationToken);
            }}
        }}
        """;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var provider = context.SyntaxProvider.CreateSyntaxProvider(
                static (syntaxNode, _) =>
                {
                    // Filter out not classes
                    if (syntaxNode is not ClassDeclarationSyntax classDeclarationSyntax)
                    {
                        return false;
                    }

                    // We use convention that all aggregate classes end with "Aggregate"
                    if (!classDeclarationSyntax.Identifier.ValueText.EndsWith(ClassPostfix, StringComparison.Ordinal))
                    {
                        return false;
                    }

                    return true;
                },
                static (syntaxContext, _) => (ClassDeclarationSyntax)syntaxContext.Node);

            context.RegisterSourceOutput(provider, static (ctx, classDeclarationSyntax) =>
            {
                var @namespace = GetNamespace(classDeclarationSyntax);

                var className = classDeclarationSyntax.Identifier.ValueText.Replace(ClassPostfix, string.Empty);

                var source = string.Format(SourceCodeTemplate, @namespace, className);

                ctx.AddSource(
                    $"{className}.g.cs",
                    SourceText.From(source, Encoding.UTF8));
            });
        }

        private static string GetNamespace(ClassDeclarationSyntax classDeclarationSyntax)
        {
            var namespaceDeclaration = classDeclarationSyntax
                .Ancestors()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault();

            if (namespaceDeclaration is not null)
            {
                return namespaceDeclaration.Name.ToString();
            }

            // Handle case for file-scoped namespaces (C# 10+)
            var fileScopedNamespace = classDeclarationSyntax
                .Ancestors()
                .OfType<FileScopedNamespaceDeclarationSyntax>()
                .First();

            return fileScopedNamespace.Name.ToString();
        }
    }
}
