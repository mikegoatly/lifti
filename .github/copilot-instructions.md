# LIFTI - Lightweight Full Text Indexing for .NET

## Project Overview

LIFTI is a lightweight, in-memory full text search indexing library for .NET. The project provides high-performance text indexing and querying capabilities with support for loose text, object properties, powerful query syntax, thesaurus support, and binary serialization.

### Key Features

- In-memory full text indexing
- Object property indexing with fluent configuration
- Advanced query language with operators (AND, OR, NOT, NEAR, wildcards, fuzzy matching)
- Thesaurus support for synonym expansion
- Score boosting (freshness, magnitude, field-level)
- Binary serialization for index persistence
- Support for .NET 8.0 and 9.0

## Architecture

### Core Components

1. **FullTextIndex<TKey>**: Main entry point for creating and querying indexes
2. **FullTextIndexBuilder<TKey>**: Fluent builder for configuring indexes
3. **IndexNode**: Tree-based index structure using a character-based trie
4. **Query System**: Complex query parsing and execution with scoring
5. **Tokenization**: Customizable text tokenization with stemming support
6. **Serialization**: Binary serialization for index persistence

### Key Namespaces

- `Lifti`: Core index types and builders
- `Lifti.Querying`: Query parsing, execution, and scoring
- `Lifti.Tokenization`: Text tokenization, stemming, and text extraction
- `Lifti.Serialization`: Binary serialization support

## Coding Standards

### General Principles

1. **Nullable Reference Types**: Enabled project-wide. Always annotate nullability correctly.
2. **Performance First**: LIFTI is a performance-critical library. Consider memory allocations, object pooling, and algorithmic efficiency.
3. **Immutability**: Prefer immutable data structures where possible. Use mutation patterns with care.
4. **Documentation**: All public APIs must have XML documentation comments.
5. **Backwards Compatibility**: Maintain serialization compatibility across versions when possible.

### Code Style

- Follow the `.editorconfig` settings (4-space indentation, CRLF line endings)
- Use `var` for local variables
- Use expression-bodied members for properties and simple methods
- Prefer `this.` qualification for fields, properties, and methods
- Use file-scoped namespaces (C# 10+) when appropriate
- Enable and address all compiler warnings (`TreatWarningsAsErrors=True`)

### Exception Handling

**CRITICAL**: All exception messages must be defined in `ExceptionMessages.resx` resource file:

1. **Never use hard-coded exception messages** - Always define them in `src/Lifti.Core/ExceptionMessages.resx`
2. **Add the resource entry** with a descriptive name (e.g., `EndAnchorWithoutPrecedingText`)
3. **Update the Designer.cs file** - Add the corresponding property to `src/Lifti.Core/ExceptionMessages.Designer.cs`:
   ```csharp
   /// <summary>
   ///   Looks up a localized string similar to [Your message here].
   /// </summary>
   internal static string YourExceptionName {
       get {
           return ResourceManager.GetString("YourExceptionName", resourceCulture);
       }
   }
   ```
4. **Use the resource in code** - Reference it via `ExceptionMessages.YourExceptionName`
5. **XML-encode special characters** in the resx file (e.g., `&lt;` for `<`, `&gt;` for `>`)

Example:

```csharp
// ❌ WRONG - Hard-coded message
throw new QueryParserException("End anchor (>>) must be preceded by text");

// ✅ CORRECT - Using resource
throw new QueryParserException(ExceptionMessages.EndAnchorWithoutPrecedingText);
```

### Naming Conventions

- Interfaces: `IFullTextIndex`, `IIndexNavigator`
- Builders: `FullTextIndexBuilder`, `ThesaurusBuilder`
- Options: `IndexOptions`, `QueryParserOptions`
- Results: `SearchResult`, `SearchResults`
- Factories: `IIndexScorerFactory`, `IIndexNodeFactory`

### Pattern Usage

1. **Builder Pattern**: Used extensively for configuration (see `FullTextIndexBuilder`, `ThesaurusBuilder`)
2. **Factory Pattern**: For creating complex objects (see `IIndexNodeFactory`, `IIndexScorerFactory`)
3. **Pool Pattern**: For reusable objects (see `IndexNavigatorPool`, `SharedPool<T>`)
4. **Mutation Pattern**: For safe concurrent modifications (see `IndexMutation`, `IndexNodeMutation`)
5. **Snapshot Pattern**: For consistent read views during writes (see `IIndexSnapshot`)

## Testing Standards

### Test Framework

- **xUnit** for all unit tests
- **FluentAssertions** for expressive assertions
- Target frameworks: net8.0, net9.0

### Test Organization

1. Test classes should match source class names with `Tests` suffix
2. Inherit from base classes when common setup is needed (e.g., `QueryTestBase`, `MutationTestBase`)
3. Use descriptive test method names that explain the scenario
4. Group related tests using nested classes or theory data
5. Where possible, create a `sut` (system under test) field, initialized in the constructor that can be reused across test methods

### Test Patterns

```csharp
public class FeatureTests
{
    private readonly SystemUnderTest sut;

    public FeatureTests()
    {
        this.sut = new SystemUnderTest();
    }

    [Fact]
    public void Method_WhenCondition_ShouldExpectedBehavior()
    {
        this.sut.Method();

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(input1, expected1)]
    [InlineData(input2, expected2)]
    public void Method_WithVariousInputs_ShouldReturnExpectedResults(
        string input,
        string expected)
    {
        // Test implementation
    }
}
```

## Common Tasks

### Adding a New Feature

1. Start with the public API design in `Lifti.Core`
2. Add XML documentation to all public members
3. Consider backwards compatibility and serialization impact
4. Implement the feature with performance in mind
5. Add comprehensive unit tests
6. Update documentation in the `docs` folder if user-facing
7. Add a sample to `samples/TestConsole` if appropriate

### Working with Index Mutations

When modifying index state:

1. Use `IndexMutation<TKey>` for all structural changes
2. Acquire write locks appropriately
3. Create new `IndexSnapshot<TKey>` after mutations
4. Ensure thread-safety for concurrent readers and writers

### Adding Query Features

1. Update `QueryParser` for new syntax
2. Create query part classes in `Querying/QueryParts`
3. Implement execution logic in query part
4. Add appropriate scoring logic
5. Test with various query combinations

### Working with Tokenization

1. Implement `IIndexTokenizer` for custom tokenization
2. Use `TokenizerBuilder` for fluent configuration
3. Consider stem word caching for performance
4. Test with various languages and character sets

## Performance Considerations

1. **Memory Allocation**: Minimize allocations in hot paths. Use object pooling (`IndexNavigatorPool`, `SharedPool<T>`).
2. **String Operations**: Use `ReadOnlySpan<char>` and `VirtualString` to avoid allocations.
3. **Collections**: Prefer arrays over lists where size is known. Use `ChildNodeMap` for index nodes.
4. **Async/Await**: Only use async when I/O-bound or for batching operations.
5. **Locking**: Keep lock duration minimal. Use `SemaphoreSlim` for async-compatible locking.

## Documentation

### XML Documentation

All public APIs require XML documentation:

```csharp
/// <summary>
/// Brief description of what this does.
/// </summary>
/// <param name="paramName">Description of the parameter.</param>
/// <returns>Description of the return value.</returns>
/// <exception cref="ExceptionType">When this exception is thrown.</exception>
public ReturnType MethodName(ParamType paramName)
{
    // Implementation
}
```

### Website Documentation

Documentation is built with Hugo in the `docs` folder:

- Getting started guides in `docs/content/en/docs/getting-started/`
- Use code examples that can be run in `samples/TestConsole`

## Build and Development

### Building the Project

Use the VS Code task: `build`

### Running Tests

```bash
dotnet test Lifti.sln
```

### Local Documentation

```bash
cd docs
hugo server
```

## Common Patterns to Follow

### Creating an Index

```csharp
var index = new FullTextIndexBuilder<TKey>()
    .WithObjectTokenization<TObject>(options => options
        .WithKey(o => o.Id)
        .WithField("FieldName", o => o.Property))
    .WithDefaultTokenizer(/* custom tokenizer */)
    .Build();
```

### Querying

```csharp
var results = await index.SearchAsync("query text");
// or
var query = index.QueryParser.Parse("field=value & other");
var results = await index.SearchAsync(query);
```

### Serialization

```csharp
// Serialize
await using var stream = File.Create("index.dat");
await index.SerializeAsync(stream);

// Deserialize
await using var stream = File.OpenRead("index.dat");
var index = await FullTextIndex<TKey>.DeserializeAsync(stream);
```

## Key Types to Understand

- **TKey**: Generic key type for indexed items (must be `notnull`)
- **IndexNode**: Tree node in the character-based trie structure
- **DocumentMetadata**: Tracks document information and statistics
- **IndexSnapshot**: Consistent view of index for querying
- **IIndexNavigator**: Traverses the index structure during queries
- **OkapiBm25Scorer**: Default scoring algorithm implementation

## Version Compatibility

- Serialization format versioning is critical
- Test deserialization of older formats in `test/Lifti.Tests/Serialization`
- Keep test data files (V2.dat, V3.dat, V4.dat, V5.dat, V6.dat, V7.dat) for regression testing
- Document breaking changes clearly

### Creating Test Indexes for New Serialization Versions

When introducing a new serialization version (e.g., V8):

1. **Update the commented CreateTestIndex method** in `BinarySerializerTests.cs`:

   - Uncomment the `[Fact]` attribute
   - Update the filename to the new version (e.g., `V8.dat`)
   - Ensure it creates an index with representative data including:
     - Static fields
     - Dynamic fields
     - Score boosting metadata (freshness and magnitude)
     - Multi-byte Unicode characters (e.g., `亜`)
     - Any new metadata being tracked in the current version

2. **Run the test once** to generate the .dat file:

   ```bash
   dotnet test --filter "FullyQualifiedName~CreateTestIndex" --framework net9.0
   ```

3. **Add the file to TestResources**:

   - Add entry to `TestResources.resx`:
     ```xml
     <data name="v8Index" type="System.Resources.ResXFileRef, System.Windows.Forms">
       <value>V8.dat;System.Byte[], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
     </data>
     ```
   - Add property to `TestResources.Designer.cs`:
     ```csharp
     internal static byte[] v8Index {
         get {
             object obj = ResourceManager.GetObject("v8Index", resourceCulture);
             return ((byte[])(obj));
         }
     }
     ```

4. **Create the deserialization test**:

   ```csharp
   [Fact]
   public async Task ShouldDeserializeV8Index()
   {
       var index = CreateObjectIndex();
       var serializer = new BinarySerializer<int>();
       using (var stream = new MemoryStream(TestResources.v8Index))
       {
           await serializer.DeserializeAsync(index, stream);
       }

       // Verify searches work
       index.Search("serialized").Should().HaveCount(1);
       index.Search("亜").Should().HaveCount(1);

       // Verify new metadata is present (adapt to what V8 adds)
       // Example: verify score boosting metadata
       var objectScoreBoostMetadata = index.Metadata.GetObjectTypeScoreBoostMetadata(1);
       objectScoreBoostMetadata.CalculateScoreBoost(index.Metadata.GetDocumentMetadata(0))
           .Should().Be(20D);
   }
   ```

5. **Re-comment the CreateTestIndex method** after generating the .dat file to prevent accidental execution

6. **Verify all tests pass**:
   ```bash
   dotnet test --framework net9.0
   ```

This process ensures backward compatibility testing for all serialization versions.

## Commit Message Guidelines

All commit messages must follow these conventions to maintain a clear project history:

### Format

Use the conventional commits format:

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes only
- `style`: Code style changes (formatting, missing semicolons, etc.)
- `refactor`: Code changes that neither fix bugs nor add features
- `perf`: Performance improvements
- `test`: Adding or updating tests
- `chore`: Changes to build process, tools, or dependencies

### Subject Line

- Keep under 72 characters
- Use imperative mood ("Add feature" not "Added feature" or "Adds feature")
- Don't capitalize the first letter after the colon
- Don't end with a period

### Breaking Changes - REQUIRED DOCUMENTATION

**CRITICAL**: Any commit that introduces breaking changes to the public API MUST include:

1. **Check `PublicAPI.Unshipped.txt`**: Review changes in `src/Lifti.Core/PublicApi/PublicAPI.Unshipped.txt` to identify API modifications
2. **Breaking change marker**: Include `BREAKING CHANGE:` in the footer
3. **List affected APIs**: Explicitly list removed or modified public APIs
4. **Context and rationale**: Explain WHY the breaking change was necessary (performance, design improvement, fixing fundamental issues, etc.)
5. **Migration guidance**: Provide clear instructions for users to adapt their code

#### Breaking Change Example

```
feat(serialization): add field-level statistics tracking

Track last token index for each field to enable precise phrase scoring
and improved relevance calculations. This change enhances query accuracy
but requires serialization format changes.

BREAKING CHANGE: Serialization format updated to V7

Affected APIs (see PublicAPI.Unshipped.txt):
- FieldStatistics now includes LastTokenIndex property
- DocumentStatistics.StatisticsByField structure modified
- Binary serialization format incompatible with V6

Rationale:
Previous versions lacked per-field token position tracking, limiting
phrase query accuracy. V7 adds LastTokenIndex to support future phrase
proximity scoring improvements and cross-field query optimizations.

Migration:
- Existing V6 indexes will deserialize with default values
- Re-index data to take advantage of new field statistics
- Update custom serializers if using IIndexSerializer
```

### Non-Breaking Change Examples

```
feat(query): add fuzzy matching support for wildcard queries

Extends wildcard query syntax to support fuzzy matching (~N notation)
for improved typo tolerance in prefix/suffix searches.
```

```
fix(tokenization): resolve emoji tokenization with surrogate pairs

Fixes issue #123 where emoji containing surrogate pairs (🤷‍♀️) were
incorrectly split during tokenization.
```

```
perf(index): optimize IndexNavigatorPool allocation strategy

Reduces memory allocations by 40% in high-throughput scenarios by
implementing better pool sizing heuristics.
```

### Commit Body

- Wrap at 72 characters
- Explain WHAT and WHY, not HOW (code shows how)
- Reference issue numbers when applicable (#123)
- Provide context for future maintainers

### Footer

- Reference related issues: `Fixes #123`, `Closes #456`, `Relates to #789`
- Include `BREAKING CHANGE:` for any public API changes
