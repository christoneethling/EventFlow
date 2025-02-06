using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

[Generator]
public class AggregateExtensionsAttributeGenerator : IIncrementalGenerator
{
    public const string Namespace = "EventFlow.SourceGenerators";

    public const string AttributeName = "AggregateExtensions";

    private const string SourceCodeTemplate =
    """
    using System;

    namespace {0}
    {{
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        public class {1}Attribute : Attribute
        {{

        }}
    }}
    """;

    private readonly string SourceCode = string.Format(
        SourceCodeTemplate,
        Namespace,
        AttributeName);

    private readonly string SourceCodeFile = $"{AttributeName}Attribute.g.cs";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
            ctx.AddSource(SourceCodeFile, SourceText.From(SourceCode, Encoding.UTF8)));
    }
}
