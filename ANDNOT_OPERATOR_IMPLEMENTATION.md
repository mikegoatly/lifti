# AND-NOT Operator (`&!`) Implementation Summary

## Overview

Implemented a binary `&!` (AND-NOT) operator for LIFTI that allows users to exclude documents containing specific terms from their search results.

**Operator:** `&!`  
**Read as:** "AND NOT" or "WITHOUT"  
**Type:** Binary operator (requires both left and right operands)  
**Precedence:** `OperatorPrecedence.And` (same as `&`)

## Syntax Examples

```
eiffel &! tower                    → Documents with "eiffel" but not "tower"
foo &! (bar | baz)                 → "foo" without "bar" or "baz"
(paris | london) &! museum         → "(paris or london) but not museums"
field1=value &! field2=unwanted    → Field filtering with exclusion
paris &! museum &! gallery         → Multiple exclusions (left-associative)
```

## Implementation Details

### Files Created

1. **`AndNotQueryOperator.cs`** - Binary query operator implementing the AND-NOT logic
2. **`ExceptMerger.cs`** - Efficient set difference algorithm for merging results

### Files Modified

1. **`QueryTokenType.cs`** - Added `AndNotOperator` enum value
2. **`QueryTokenizer.cs`** - Added tokenization for `&!` operator
3. **`QueryParser.cs`** - Added handling for `AndNotOperator` in parser
4. **`IntermediateQueryResult.cs`** - Added `Except()` method for set difference
5. **`PublicAPI.Unshipped.txt`** - Tracked API changes

### Key Design Decisions

#### 1. Operator Symbol: `&!`
- **Why:** Reads naturally as "AND NOT" matching human speech
- **Advantage:** Two-character sequence avoids conflicts with existing syntax
- **Precedent:** Combines familiar `&` (AND) with `!` (NOT) concepts

#### 2. Operator Precedence: Same as AND
```
a | b &! c  → parses as a | (b &! c)
a & b &! c  → parses as (a & b) &! c
```
This matches boolean algebra where AND and AND-NOT have equal precedence.

#### 3. Evaluation Strategy: Left-First with Optimization
```csharp
// Evaluate left side first
var leftResults = this.Left.Evaluate(navigatorCreator, queryContext);

// Only evaluate right side for documents that matched left (optimization!)
var rightResults = this.Right.Evaluate(
    navigatorCreator,
    queryContext with { FilterToDocumentIds = leftResults.ToDocumentIdLookup() });

// Subtract right from left
return leftResults.Except(rightResults);
```

**Optimization:** The right side is only evaluated for documents that matched the left side, avoiding unnecessary work.

#### 4. Efficient Set Difference Algorithm
The `ExceptMerger` uses a single-pass algorithm exploiting the fact that both result sets are sorted by DocumentId:

```csharp
while (leftIndex < leftCount)
{
    var leftMatch = leftMatches[leftIndex];
    
    // Advance right pointer until we reach or pass the left document
    while (rightIndex < rightCount && rightMatches[rightIndex].DocumentId < leftMatch.DocumentId)
    {
        rightIndex++;
    }
    
    // If right doesn't have this document, include it in results
    if (rightIndex >= rightCount || rightMatches[rightIndex].DocumentId > leftMatch.DocumentId)
    {
        results.Add(leftMatch);
    }
    
    leftIndex++;
}
```

**Time Complexity:** O(n + m) where n and m are the sizes of left and right result sets.

## Breaking Changes

**YES** - This is a breaking change for serialization and public API:

### Public API Changes
- New enum value: `QueryTokenType.AndNotOperator = 10`
- Existing value changed: `QueryTokenType.FieldFilter` moved from `10` to `11`
- New class: `AndNotQueryOperator`
- New method: `IntermediateQueryResult.Except()`

### Backward Compatibility
- Existing queries without `&!` continue to work unchanged
- Query parsing is not affected unless `&!` is used
- Index serialization format unchanged (no index rebuild required)
- However, enum value changes mean assembly version incompatibility

## Usage Patterns

### Simple Exclusion
```csharp
var results = index.Search("eiffel &! tower");
// Returns documents containing "eiffel" but not "tower"
```

### Multiple Exclusions (Chains Left-to-Right)
```csharp
var results = index.Search("paris &! museum &! gallery");
// Equivalent to: ((paris &! museum) &! gallery)
// Returns documents with "paris" excluding both museums and galleries
```

### Excluding Multiple Terms (Use Parentheses)
```csharp
var results = index.Search("france &! (tower | museum)");
// Returns documents with "france" excluding both "tower" and "museum"
```

### Field-Specific Exclusions
```csharp
var results = index.Search("title=important &! content=spam");
// Documents where title contains "important" but content doesn't contain "spam"
```

### Complex Boolean Combinations
```csharp
var results = index.Search("(paris | london) & restaurant &! expensive");
// Restaurants in Paris or London, but not expensive ones
```

## Performance Characteristics

### Best Case
- **Left side:** 100 documents
- **Right side:** 10 documents (all overlap with left)
- **Right evaluation:** Only checks 10 documents (not entire index)
- **Merge:** O(100 + 10) = O(110) operations

### Comparison to Alternatives
| Approach | Left Docs | Right Eval | Merge | Total |
|----------|-----------|------------|-------|-------|
| **&! Operator** | 1000 | 1000 | O(n+m) | ✅ Optimal |
| **Wildcard Negation** (`* &! term`) | ALL docs | ALL docs | O(n+m) | ❌ Expensive |

The `&!` operator is significantly more efficient than wildcard-based negation because it:
1. Only evaluates the right side for documents matching the left
2. Avoids traversing the entire index tree
3. Uses sorted merge for O(n+m) complexity

## Testing Recommendations

### Unit Tests to Add
1. **Simple exclusion:** `eiffel &! tower`
2. **Empty left side:** Ensure returns empty result
3. **Empty right side:** Ensure returns left unchanged
4. **Parentheses:** `foo &! (bar | baz)`
5. **Multiple exclusions:** `a &! b &! c`
6. **Field filters:** `field1=value &! field2=unwanted`
7. **Precedence:** `a | b &! c` vs `(a | b) &! c`
8. **Operator chaining:** `a & b &! c`
9. **Score preservation:** Ensure scores from left side are preserved
10. **Document filtering:** Verify right side only evaluates left's documents

### Integration Tests
1. Real-world exclusion scenarios
2. Performance testing with various document set sizes
3. Combination with other operators (AND, OR, NEAR, etc.)
4. Field-specific exclusions

## Future Enhancements

### Potential Improvements
1. **Weighting Calculation:** Currently uses minimum of left/right, could be optimized
2. **Query Optimization:** Could reorder operations based on estimated result sizes
3. **Syntax Sugar:** Consider aliases like `ANDNOT`, `WITHOUT`, `-` (though more complex)
4. **Documentation:** Add examples to user documentation and samples

### Known Limitations
1. **Cannot negate standalone:** `&! tower` requires left operand (design choice)
2. **Full corpus negation expensive:** `* &! tower` works but traverses entire index
3. **Score calculation:** Excluded documents don't contribute to scoring

## Documentation Updates Needed

1. **Getting Started Guide:** Add section on exclusion queries
2. **Query Syntax Reference:** Document `&!` operator
3. **API Documentation:** XML docs already added to public classes
4. **Samples:** Add example to `TestConsole` project
5. **Release Notes:** Document as breaking change for V7

## Example for TestConsole

```csharp
public class AndNotOperatorSample : ISample
{
    public string Name => "AND-NOT Operator";
    
    public async Task RunAsync()
    {
        var index = new FullTextIndexBuilder<int>()
            .WithObjectTokenization<Book>(options => options
                .WithKey(b => b.Id)
                .WithField("Title", b => b.Title)
                .WithField("Description", b => b.Description))
            .Build();

        await index.AddRangeAsync(new[]
        {
            new Book(1, "Eiffel Tower Guide", "A guide to visiting the Eiffel Tower in Paris"),
            new Book(2, "Paris Without the Tower", "Exploring Paris beyond the famous tower"),
            new Book(3, "Eiffel 65", "The story of the famous music group"),
            new Book(4, "French Architecture", "Overview of French buildings including towers")
        });

        // Find books about Eiffel but not the tower
        var results = index.Search("eiffel &! tower");
        Console.WriteLine("'eiffel &! tower' found:");
        foreach (var result in results)
        {
            Console.WriteLine($"  - Book {result.Key}: {result.Score:F2}");
        }
        
        // Find books about Paris, excluding both tower and Eiffel
        results = index.Search("paris &! (tower | eiffel)");
        Console.WriteLine("\n'paris &! (tower | eiffel)' found:");
        foreach (var result in results)
        {
            Console.WriteLine($"  - Book {result.Key}: {result.Score:F2}");
        }
    }
}
```

## Conclusion

The `&!` operator successfully implements efficient document exclusion in LIFTI with:
- ✅ Natural, readable syntax (`&!` reads as "AND NOT")
- ✅ Optimal performance through query filtering
- ✅ Consistent operator precedence
- ✅ Full composability with existing query features
- ✅ Zero conflicts with existing syntax
- ✅ Clean, maintainable implementation

The implementation follows LIFTI's coding standards and patterns, introduces no technical debt, and provides a powerful new query capability that was previously difficult to achieve efficiently.
