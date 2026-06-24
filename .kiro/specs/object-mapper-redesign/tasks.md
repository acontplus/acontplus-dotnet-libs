# Implementation Plan: ObjectMapper Redesign

## Overview

Redesign the `ObjectMapper` in `Acontplus.Utilities` to use expression-tree compiled delegates instead of per-call reflection. The implementation follows the design's two-phase lifecycle: a startup phase that compiles delegates for all registered type pairs, and a hot-path phase that executes pre-compiled delegates with zero reflection overhead. All code lives under `src/Acontplus.Utilities/Mapping/` with tests in `tests/Acontplus.Utilities.Tests/`.

## Tasks

- [x] 1. Set up project structure, core types, and interfaces
  - [x] 1.1 Create `TypePair` readonly struct and `IObjectMapper` interface
    - Create `src/Acontplus.Utilities/Mapping/TypePair.cs` implementing `IEquatable<TypePair>` with `SourceType`, `TargetType` properties, `Equals`, `GetHashCode` (using `HashCode.Combine`), and `ToString`
    - Create `src/Acontplus.Utilities/Mapping/IObjectMapper.cs` with `Map<TSource, TTarget>(TSource)`, `Map<TSource, TTarget>(TSource, TTarget)`, `Map<TSource, TTarget>(IEnumerable<TSource>)`, and `ProjectTo<TSource, TTarget>(IQueryable<TSource>)` members
    - Add XML documentation on all public members
    - _Requirements: 9.3_

  - [x] 1.2 Create `MappingExpressionBase` and `MappingExpression<TSource, TTarget>` fluent builder
    - Create `src/Acontplus.Utilities/Mapping/MappingExpressionBase.cs` with `TypePair Pair`, `Dictionary<string, LambdaExpression?> MemberRules`, `Dictionary<string, Func<object, object?>> DelegateResolvers`, `Dictionary<string, LambdaExpression> CtorParamRules`, and `bool HasReverseMap`
    - Create `src/Acontplus.Utilities/Mapping/MappingExpression.cs` with `ForMember` (expression overload), `ForMember` (delegate overload), `Ignore`, `ForCtorParam`, and `ReverseMap` methods — all returning `MappingExpression<TSource, TTarget>` for chaining
    - Validate at configuration time that destination expressions resolve to writable properties; throw `InvalidOperationException` otherwise
    - Add XML documentation on all public members
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_

  - [x] 1.3 Create `MappingProfile` abstract base class
    - Create `src/Acontplus.Utilities/Mapping/MappingProfile.cs` with internal `Dictionary<TypePair, MappingExpressionBase> Registrations` property
    - Implement `CreateMap<TSource, TTarget>()` that registers the `TypePair` and returns `MappingExpression<TSource, TTarget>`, overwriting any previous registration for the same pair
    - Handle `ReverseMap()` registration: register inverse `TypePair` with convention-only rules; throw if reverse pair fails validation
    - Add XML documentation on all public members
    - _Requirements: 3.1, 3.6, 3.7, 3.8_

  - [x]\* 1.4 Write unit tests for `TypePair`, `MappingExpression`, and `MappingProfile`
    - Test `TypePair` equality and hash code for identical and different type pairs
    - Test `ForMember` throws `InvalidOperationException` for non-writable destination members
    - Test `CreateMap` called twice for same pair overwrites previous registration
    - Test `ReverseMap` does not carry forward `ForMember`/`Ignore`/`ForCtorParam` rules
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_

- [x] 2. Implement `MapperConfiguration` and `MapperRegistry`
  - [x] 2.1 Create `MapperRegistry` thread-safe delegate store
    - Create `src/Acontplus.Utilities/Mapping/MapperRegistry.cs` with `ConcurrentDictionary<TypePair, Lazy<Delegate>>` backing store
    - Implement `Register(TypePair, Delegate)`, `GetOrAdd(TypePair)`, and `TryGet(TypePair, out Delegate?)` methods
    - `GetOrAdd` uses `Lazy<T>` factory to ensure single compilation under concurrent first-use for unregistered pairs
    - _Requirements: 2.1, 2.2, 2.3, 1.4_

  - [x] 2.2 Create `MapperConfiguration` with `AddProfile` and `Build`
    - Create `src/Acontplus.Utilities/Mapping/MapperConfiguration.cs` with sealed-after-build semantics
    - `AddProfile` accepts a `MappingProfile` instance; throws `InvalidOperationException` if called after `Build()`
    - `Build()` validates all registered `TypePair`s in a single pass, collecting all violations (non-existent members, type-incompatible resolvers) across all pairs; throws a single `InvalidOperationException` listing every violation if any exist
    - On successful validation, triggers delegate compilation for all registered pairs and returns a fully initialised `MapperRegistry`
    - _Requirements: 2.4, 10.3, 10.4, 1.3_

  - [ ]\* 2.3 Write property test for sealed configuration
    - **Property 3: Sealed configuration rejects late registrations**
    - **Validates: Requirement 2.4**

  - [ ]\* 2.4 Write unit tests for `MapperConfiguration` validation errors
    - Test `Build()` throws with aggregate message listing all violations for misconfigured `ForMember` targets
    - Test `Build()` throws for unsatisfiable constructors naming the target type and unsatisfied parameters
    - Test `AddProfile` after `Build()` throws `InvalidOperationException` with sealed message
    - _Requirements: 2.4, 10.3_

- [x] 3. Implement `ExpressionBuilder` — flat and convention mapping
  - [x] 3.1 Create `ExpressionBuilder` with flat/simple-type member mapping
    - Create `src/Acontplus.Utilities/Mapping/Internal/ExpressionBuilder.cs`
    - Implement `BuildMappingExpression` for flat mappings: case-insensitive name matching, directly-assignable types via `MemberExpression`, compatible simple types via `Convert` expression, and `Convert.ChangeType` wrapped in try/catch returning default on failure
    - Handle `ForMember` expression rules (use provided source expression) and delegate resolvers (wrap as invocation)
    - Handle `Ignore` rules (omit member from bindings)
    - Skip destination members without public setters (non-init, non-writable)
    - Skip destination members with ambiguous case-insensitive matches (two+ source members)
    - Handle null source values: assign `null` for nullable reference types, `default` for non-nullable value types
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.6, 4.7, 3.2, 3.3, 3.4_

  - [x] 3.2 Add constructor-parameter mapping to `ExpressionBuilder`
    - Implement constructor selection: prefer the public constructor with most satisfiable parameters; break ties by `GetConstructors()` order
    - Resolve each parameter in priority order: `ForCtorParam` rule → convention name match → declared default value → throw
    - Build `Expression.New(constructor, argExpressions)` and wrap with `MemberInitExpression` for remaining settable properties
    - Handle `record`/`record struct` with `init`-only properties: supply all as constructor arguments in `NewExpression`
    - Throw `InvalidOperationException` at compilation time for unresolvable parameters
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 4.5_

  - [ ]\* 3.3 Write property test for convention flat mapping
    - **Property 6: Convention flat mapping copies all name-matched members**
    - **Validates: Requirements 4.1, 4.3**

  - [ ]\* 3.4 Write property test for unmapped members retaining defaults
    - **Property 4: Unmapped and non-writable destination members retain CLR defaults**
    - **Validates: Requirements 4.2, 4.4**

  - [ ]\* 3.5 Write property test for constructor-mapped targets
    - **Property 8: Constructor-mapped targets receive correct values from source**
    - **Validates: Requirements 7.1, 7.2, 7.3, 7.5, 4.5**

- [x] 4. Implement nested object and collection mapping in `ExpressionBuilder`
  - [x] 4.1 Add nested complex-type mapping with cycle detection
    - Implement recursive `BuildMappingExpression` for nested complex-type members using `inProgress` `HashSet<TypePair>` for cycle detection
    - Inline nested `LambdaExpression` body into parent `MemberBinding` when no cycle
    - On cycle detection: create deferred `Lazy<Func<TSource, TTarget>>` closure resolved from the registry after all compilation completes; use `Expression.Invoke` with `Lazy.Value`
    - Handle null nested source: assign `null` (nullable ref type) or `default` (non-nullable value type)
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 10.5_

  - [x] 4.2 Add collection member mapping
    - Implement element `TypePair` resolution from source/destination collection generic arguments
    - Obtain or compile element `Func<TSourceElement, TTargetElement>` from registry
    - Build `Enumerable.Select` expression followed by materialisation: `.ToArray()` for `T[]`, `.ToList()` for `List<T>`/`IList<T>`/`ICollection<T>`/`IEnumerable<T>`/`IReadOnlyList<T>`/`IReadOnlyCollection<T>`
    - Null-guard: null source → `null` (nullable dest) or `new List<T>()` (non-nullable dest); empty source → empty collection of correct type
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

  - [ ]\* 4.3 Write property test for null safety
    - **Property 5: Null source values never throw — produce correct defaults**
    - **Validates: Requirements 4.6, 5.3, 5.4, 10.1**

  - [ ]\* 4.4 Write property test for collection mapping
    - **Property 9: Collection mapping preserves cardinality and correctly maps each element**
    - **Validates: Requirements 6.1, 6.3, 6.4, 6.5, 6.6**

  - [ ]\* 4.5 Write property test for cyclic object graphs
    - **Property 12: Cyclic object graphs map correctly and Build() does not throw**
    - **Validates: Requirement 10.5**

- [~] 5. Checkpoint — core expression building
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Implement `DelegateCompiler` and `ObjectMapper` concrete class
  - [x] 6.1 Create `DelegateCompiler` internal helper
    - Create `src/Acontplus.Utilities/Mapping/Internal/DelegateCompiler.cs`
    - Implement `Compile(LambdaExpression)` that calls `Expression.Lambda(...).Compile()` and returns as `Delegate`
    - Implement `CompileConvention(TypePair, MapperRegistry)` that builds convention expression via `ExpressionBuilder` and compiles it
    - _Requirements: 1.1, 1.2, 1.4_

  - [x] 6.2 Create `ObjectMapper` sealed class implementing `IObjectMapper`
    - Create `src/Acontplus.Utilities/Mapping/ObjectMapper.cs` (the new instance-based, non-static class)
    - Constructor accepts `MapperRegistry`
    - `Map<TSource, TTarget>(TSource source)`: return `default(TTarget)` if source is null; resolve delegate via `TypePair` using runtime type of source; invoke cached delegate
    - `Map<TSource, TTarget>(TSource source, TTarget destination)`: throw `ArgumentNullException("destination")` if destination is null; resolve delegate for into-existing variant
    - `Map<TSource, TTarget>(IEnumerable<TSource> source)`: resolve element delegate, map each element, return `IEnumerable<TTarget>`
    - Add XML documentation on all public members
    - _Requirements: 1.2, 1.5, 6.6, 9.3, 10.1, 10.2_

  - [ ]\* 6.3 Write property test for delegate compilation idempotence
    - **Property 1: Delegate compilation is idempotent — compile once, cache forever**
    - **Validates: Requirements 1.1, 1.3, 1.4**

  - [ ]\* 6.4 Write property test for thread-safe concurrent mapping
    - **Property 2: Thread-safe concurrent mapping produces consistent results**
    - **Validates: Requirements 2.1, 2.2, 2.3**

  - [ ]\* 6.5 Write property test for null destination argument
    - **Property 13: Null destination argument always throws ArgumentNullException**
    - **Validates: Requirement 10.2**

- [x] 7. Implement `ProjectTo` queryable projection
  - [x] 7.1 Add `BuildProjectionExpression` to `ExpressionBuilder`
    - Implement `BuildProjectionExpression(TypePair, MappingExpressionBase?)` following same member-resolution rules but without `Convert.ChangeType` and without `Func<>` delegate resolvers
    - Throw `NotSupportedException` naming the offending member for any member requiring `Convert.ChangeType` or using a `Func<>` resolver
    - Handle `Ignore` rules: assign CLR default in expression tree
    - Handle `ForCtorParam` rules: produce `NewExpression` with named argument expressions
    - Nested complex-type pairs must also produce pure `MemberExpression` trees recursively
    - _Requirements: 8.2, 8.3, 8.4, 8.5_

  - [x] 7.2 Implement `ProjectTo` method in `ObjectMapper`
    - Throw `ArgumentNullException("source")` if source is null
    - Resolve or build projection expression for `TypePair`
    - Cast to `Expression<Func<TSource, TTarget>>` and pass to `IQueryable<TSource>.Select(...)`
    - Throw `InvalidOperationException` if no registration and no valid convention projection exists
    - _Requirements: 8.1, 8.6, 8.7_

  - [ ]\* 7.3 Write property test for ProjectTo matching in-memory mapping
    - **Property 10: ProjectTo produces a queryable whose results match in-memory mapping**
    - **Validates: Requirements 8.1, 8.2, 8.5**

  - [ ]\* 7.4 Write unit tests for ProjectTo error cases
    - Test `ProjectTo` throws `NotSupportedException` when `ForMember` uses `Func<>` resolver
    - Test `ProjectTo` throws `NotSupportedException` when convention member requires `Convert.ChangeType`
    - Test `ProjectTo` throws `ArgumentNullException` when source is null
    - _Requirements: 8.2, 8.4, 8.7_

- [~] 8. Checkpoint — mapper and projection working
  - Ensure all tests pass, ask the user if questions arise.

- [x] 9. Implement DI registration and backward-compatibility shim
  - [x] 9.1 Update `UtilitiesServiceExtensions` with `AddObjectMapper`
    - Update `src/Acontplus.Utilities/Extensions/UtilitiesServiceExtensions.cs` to add the `AddObjectMapper(this IServiceCollection, params MappingProfile[])` overload
    - Register `IObjectMapper` as `ServiceLifetime.Singleton` with a factory that builds `MapperConfiguration`, calls `Build()`, and creates the `ObjectMapper` instance
    - Throw `InvalidOperationException` if `IObjectMapper` is already registered
    - When zero profiles are passed, register successfully and allow convention-based mappings on demand
    - Wire up the static shim singleton via `ObjectMapper.SetSingleton(mapper)`
    - _Requirements: 9.1, 9.2, 9.4, 9.5, 9.6_

  - [x] 9.2 Create backward-compatibility static `ObjectMapper` shim
    - Replace existing `src/Acontplus.Utilities/Mapping/ObjectMapper.cs` with the static shim annotated with `[Obsolete("Use IObjectMapper via DI. The static API will be removed in v3.0.")]`
    - Implement `CreateMap<TSource, TTarget>()`, `Map<TSource, TTarget>(TSource)`, `Map<TSource, TTarget>(TSource, TTarget)` static methods forwarding to singleton
    - Implement internal `SetSingleton(IObjectMapper)` called by DI extension
    - Static initialiser does NOT throw; singleton remains `null` until `AddObjectMapper` is called
    - Throw `InvalidOperationException` on static mapping method calls if singleton is not initialised
    - Handle late `CreateMap` after `Build()`: synchronously compile and register the new delegate
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6_

  - [ ]\* 9.3 Write property test for static shim equivalence
    - **Property 14: Static shim forwards to singleton — results are identical to DI-resolved mapper**
    - **Validates: Requirement 11.3**

  - [ ]\* 9.4 Write unit tests for DI and shim edge cases
    - Test `AddObjectMapper` throws when `IObjectMapper` already registered
    - Test static shim throws `InvalidOperationException` before `AddObjectMapper` is called
    - Test `AddObjectMapper` with zero profiles serves convention mappings on demand
    - Test static `CreateMap` after `Build()` compiles and registers the new delegate
    - _Requirements: 9.6, 11.3, 11.5, 11.6, 9.4_

- [ ] 10. Integration wiring and remaining property tests
  - [~] 10.1 Wire all components together and verify end-to-end flow
    - Ensure `MapperConfiguration.Build()` invokes `ExpressionBuilder` → `DelegateCompiler` → `MapperRegistry.Register` for each registered `TypePair`
    - Ensure `ObjectMapper.Map` resolves delegates correctly for registered pairs, convention pairs, and runtime-typed sources (interface/base class declared types resolved by concrete runtime type)
    - Ensure `Map<TSource, TTarget>(IEnumerable<TSource>)` uses the same registry lookup
    - Verify no orphaned code — all components connected
    - _Requirements: 1.2, 1.5, 6.6_

  - [ ]\* 10.2 Write property test for ignored members
    - **Property 7: Ignored members always equal CLR default in forward direction**
    - **Validates: Requirements 3.4, 8.3**

  - [ ]\* 10.3 Write property test for last-registered CreateMap wins
    - **Property 11: Last-registered CreateMap for a TypePair wins**
    - **Validates: Requirement 3.1**

  - [ ]\* 10.4 Write integration tests for EF Core in-memory projection
    - Test `ProjectTo` against an EF Core in-memory provider to verify server-side projection
    - Test `AddObjectMapper` with profiles triggers delegate compilation at startup (first `Map` call does not compile)
    - _Requirements: 8.1, 9.2_

- [~] 11. Final checkpoint
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests use FsCheck via the `FsCheck.Xunit` package with a minimum of 100 iterations per property
- Unit tests use xUnit + FluentAssertions + NSubstitute following `MethodName_Condition_ExpectedOutcome` naming
- The static shim `ObjectMapper` class file replaces the existing implementation — ensure backward compatibility with current call sites
- All code targets `net10.0` with nullable reference types enabled and XML documentation on all public members

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "1.3"] },
    { "id": 2, "tasks": ["1.4", "2.1"] },
    { "id": 3, "tasks": ["2.2"] },
    { "id": 4, "tasks": ["2.3", "2.4", "3.1"] },
    { "id": 5, "tasks": ["3.2"] },
    { "id": 6, "tasks": ["3.3", "3.4", "3.5", "4.1", "4.2"] },
    { "id": 7, "tasks": ["4.3", "4.4", "4.5", "6.1"] },
    { "id": 8, "tasks": ["6.2"] },
    { "id": 9, "tasks": ["6.3", "6.4", "6.5", "7.1"] },
    { "id": 10, "tasks": ["7.2"] },
    { "id": 11, "tasks": ["7.3", "7.4", "9.1"] },
    { "id": 12, "tasks": ["9.2"] },
    { "id": 13, "tasks": ["9.3", "9.4", "10.1"] },
    { "id": 14, "tasks": ["10.2", "10.3", "10.4"] }
  ]
}
```
