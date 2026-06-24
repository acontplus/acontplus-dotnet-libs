# Requirements Document

## Introduction

The `ObjectMapper` in `Acontplus.Utilities` currently pays full reflection costs on every mapping
call — `GetProperties()`, `MethodInfo.Invoke()`, `MakeGenericMethod()`, and
`Activator.CreateInstance()` are all executed at runtime with no caching. It also stores mapping
registrations in a plain `Dictionary<string, MappingConfiguration>` with no synchronization,
making concurrent use unsafe.

This redesign replaces that implementation with a high-performance mapper that compiles
`Expression`-tree delegates once (at startup / first use per type pair) and stores them in a
thread-safe registry. The new API surface is inspired by AutoMapper's fluent
`CreateMap<TSource, TTarget>()` style, Mapster's compiled-delegate approach, and Mapperly's
philosophy of zero per-call reflection overhead.

The mapper must fit inside the existing `Acontplus.Utilities` NuGet package (no new heavy
third-party dependencies), target `net10.0`, and follow the monorepo's coding patterns including
nullable reference types, `Result<T>` for service-level APIs, and XML documentation on all public
members.

---

## Glossary

- **Mapper**: The top-level `ObjectMapper` class (or its interface `IObjectMapper`) that is
  registered with the DI container and consumed by application code.
- **MapperRegistry**: The internal thread-safe store that holds compiled `TypePair → MappingDelegate`
  entries.
- **MappingDelegate**: A compiled `Func<TSource, TTarget>` (or `Action<TSource, TTarget>` for
  into-existing-instance variant) produced from an `Expression` tree.
- **TypePair**: A value type `(Type Source, Type Target)` that uniquely identifies a mapping route.
- **MappingProfile**: A class that groups a set of `CreateMap` calls, similar to AutoMapper's
  `Profile`. Consumers extend `MappingProfile` and register it via DI.
- **MapperConfiguration**: The immutable snapshot of all `MappingProfile` registrations; used to
  build the `MapperRegistry` at startup.
- **ForMember**: Fluent API call that overrides the mapping rule for a single destination member.
- **Ignore**: Fluent API call that excludes a destination member from mapping entirely.
- **ForCtorParam**: Fluent API call that maps a source member to a named constructor parameter on
  the target type.
- **Convention mapping**: Automatic matching of source and target members by name
  (case-insensitive) and compatible type when no explicit `ForMember` rule is provided.
- **Simple type**: A C# numeric primitive (`byte`, `sbyte`, `short`, `ushort`, `int`, `uint`,
  `long`, `ulong`, `float`, `double`), `bool`, `char`, `string`, `decimal`, `DateTime`,
  `DateTimeOffset`, `TimeSpan`, `Guid`, any `enum`, or a `Nullable<T>` of any of the above.
- **Complex type**: Any type that is not a Simple type (includes user-defined classes and records).
- **Collection**: Any type that is not `string` and implements `IEnumerable`.
- **Projection**: A LINQ `IQueryable<TSource>` extension that translates a mapping to a
  server-side `Select` expression without materialising source objects in memory.

---

## Requirements

### Requirement 1: Expression-compiled mapping delegates

**User Story:** As a library consumer, I want object mapping to use pre-compiled delegates instead
of per-call reflection, so that hot-path mapping operations are fast and have predictable
low latency.

#### Acceptance Criteria

1. THE `MapperRegistry` SHALL compile a `MappingDelegate` for each registered `TypePair` exactly
   once per application lifetime; subsequent requests for the same `TypePair` SHALL return the
   cached delegate without recompiling.
2. WHEN a `MappingDelegate` for a `TypePair` has already been compiled, THE `Mapper` SHALL invoke
   the cached delegate without performing any `Type.GetProperties()`, `MethodInfo.Invoke()`,
   `MakeGenericMethod()`, or `Activator.CreateInstance()` calls at the call site.
3. THE `MapperRegistry` SHALL build all `MappingDelegate` entries for explicitly registered
   `TypePair`s during the `MapperConfiguration.Build()` step so that the first call to
   `Map<TSource, TTarget>` for any registered `TypePair` does not trigger delegate compilation.
4. WHEN a `TypePair` is not explicitly registered, THE `MapperRegistry` SHALL apply convention
   mapping on first use, compile a delegate for that `TypePair`, store it in the thread-safe
   registry, and return the cached delegate for all subsequent calls to the same `TypePair`;
   IF convention mapping cannot produce a valid delegate (e.g., no satisfiable constructor, no
   matched members), THE `MapperRegistry` SHALL throw `InvalidOperationException` naming the
   unresolvable `TypePair`.
5. THE `Mapper` SHALL support mapping source objects whose declared type is an interface or base
   class by resolving the delegate from the `MapperRegistry` using the concrete runtime type of
   the source object, not the declared type.

---

### Requirement 2: Thread-safe mapping registry

**User Story:** As a library consumer hosting the mapper as a singleton in an ASP.NET Core DI
container, I want the `MapperRegistry` to be safe for concurrent reads and writes, so that
parallel requests do not corrupt mapping state or cause race conditions.

#### Acceptance Criteria

1. THE `MapperRegistry` SHALL use `ConcurrentDictionary<TypePair, Delegate>` (or an equivalent
   thread-safe structure) as its backing store so that concurrent calls to `Map` from different
   threads never produce data corruption or thrown `InvalidOperationException`s due to
   dictionary mutation.
2. WHEN two threads simultaneously request a delegate for the same unregistered `TypePair`, THE
   `MapperRegistry` SHALL compile the delegate exactly once — using `Lazy<T>` or
   `GetOrAdd` with a factory — and return the same compiled instance to both callers.
3. THE `MapperRegistry` SHALL allow concurrent reads of existing entries without blocking, even
   when a write operation for a different `TypePair` is occurring concurrently on another thread.
4. IF `MappingProfile.CreateMap` or `MapperConfiguration.AddProfile` is called after
   `MapperConfiguration.Build()` has returned, THEN THE `MapperConfiguration` SHALL throw
   `InvalidOperationException` with a message stating that the configuration is sealed and
   no further profiles may be registered.

---

### Requirement 3: Fluent mapping configuration API

**User Story:** As a developer configuring object mappings, I want a fluent API similar to
AutoMapper's `CreateMap`, so that I can clearly express custom member mappings, ignores, and
constructor bindings in a discoverable, type-safe way.

#### Acceptance Criteria

1. THE `MappingProfile` SHALL expose a `CreateMap<TSource, TTarget>()` method that registers the
   `TypePair (TSource, TTarget)` and returns a `MappingExpression<TSource, TTarget>` for further
   fluent configuration; each call to `CreateMap` for the same `TypePair` within the same profile
   SHALL overwrite the previous registration for that pair.
2. THE `MappingExpression<TSource, TTarget>` SHALL expose a
   `ForMember<TProperty>(Expression<Func<TTarget, TProperty>> dest, Expression<Func<TSource, TProperty>> src)`
   overload that maps a source member expression to a destination member; WHEN the destination
   member expression does not resolve to a writable property of `TTarget`, THE `MappingProfile`
   SHALL throw `InvalidOperationException` at configuration time; this method SHALL return
   `MappingExpression<TSource, TTarget>` to allow chaining.
3. THE `MappingExpression<TSource, TTarget>` SHALL expose a
   `ForMember<TProperty>(Expression<Func<TTarget, TProperty>> dest, Func<TSource, TProperty> resolver)`
   overload that accepts an arbitrary delegate resolver; WHEN the destination member expression
   does not resolve to a writable property of `TTarget`, THE `MappingProfile` SHALL throw
   `InvalidOperationException` at configuration time; this method SHALL return
   `MappingExpression<TSource, TTarget>` to allow chaining.
4. THE `MappingExpression<TSource, TTarget>` SHALL expose an
   `Ignore<TProperty>(Expression<Func<TTarget, TProperty>> dest)` method that excludes the
   specified destination member from all forward-direction mapping operations; the `Ignore` rule
   SHALL apply only to the `(TSource → TTarget)` direction and SHALL NOT be carried over to any
   reverse mapping; this method SHALL return `MappingExpression<TSource, TTarget>` to allow
   chaining.
5. THE `MappingExpression<TSource, TTarget>` SHALL expose a
   `ForCtorParam<TProperty>(string paramName, Expression<Func<TSource, TProperty>> src)` method
   that binds a source member expression to a named constructor parameter on `TTarget`; WHEN
   `paramName` does not match any parameter on the constructor selected for `TTarget` at
   delegate-compilation time, THE `MapperRegistry` SHALL throw `InvalidOperationException`
   naming the unresolved parameter; this method SHALL return `MappingExpression<TSource, TTarget>`
   to allow chaining.
6. WHEN `CreateMap<TSource, TTarget>()` is called, THE `MappingProfile` SHALL NOT register the
   inverse `TypePair (TTarget, TSource)`; the inverse SHALL only be registered when `.ReverseMap()`
   is explicitly chained on the returned `MappingExpression<TSource, TTarget>`.
7. IF `.ReverseMap()` is chained and the reverse `TypePair` registration fails for any reason
   (e.g., `TSource` has no satisfiable constructor), THEN THE `MappingProfile` SHALL throw the
   originating exception without registering either the forward or the reverse `TypePair`.
8. WHEN `.ReverseMap()` is called, THE `MappingExpression<TSource, TTarget>` SHALL register the
   inverse `TypePair (TTarget, TSource)` using only name-based convention matching; the
   `ForMember`, `ForCtorParam`, and `Ignore` rules from the forward direction SHALL NOT be
   carried over to the reverse direction.

---

### Requirement 4: Convention-based flat mapping

**User Story:** As a developer mapping simple DTOs to entities and back, I want the mapper to
automatically match members by name and compatible type without requiring any explicit
configuration, so that common flat mappings need zero boilerplate.

#### Acceptance Criteria

1. WHEN no explicit `ForMember` rule exists for a destination member and exactly one source member
   whose name matches case-insensitively and whose type is directly assignable to the destination
   member type exists, THE `Mapper` SHALL copy that source value to the destination member.
2. IF no source member matches by name (case-insensitive) for a destination member and no
   `ForMember` rule exists for that destination member, THEN THE `Mapper` SHALL leave the
   destination member at its CLR default value without throwing an exception.
3. WHEN source and destination member names match (case-insensitive) and both types are Simple
   types but the source type is not directly assignable to the destination type, THE `Mapper`
   SHALL attempt `Convert.ChangeType(sourceValue, destinationMemberType)`; IF the conversion
   throws at runtime, THE `Mapper` SHALL assign `default(TDestMember)` to the destination member
   and continue mapping remaining members without throwing.
4. WHEN the destination member has no public setter and is not an `init`-only property,
   THE `Mapper` SHALL skip that member without throwing an exception.
5. WHEN the destination type is a `record` or `record struct` with `init`-only properties,
   THE `Mapper` SHALL incorporate those properties as named arguments in the constructor
   `NewExpression` of the compiled delegate rather than as post-construction property assignments.
6. WHEN a source member value is `null` and the destination member type is a non-nullable value
   type, THE `Mapper` SHALL assign `default(TDestMember)` to the destination member without
   throwing a `NullReferenceException` or `InvalidCastException`.
7. WHEN two or more source members match a destination member name case-insensitively,
   THE `Mapper` SHALL skip that destination member and continue mapping remaining members without
   throwing an exception.

---

### Requirement 5: Nested object mapping

**User Story:** As a developer mapping domain objects that contain nested complex-type properties,
I want the mapper to recursively map nested objects, so that the entire object graph is mapped in
a single `Map` call.

#### Acceptance Criteria

1. WHEN a source member is a Complex type and the corresponding destination member is a Complex
   type, THE `Mapper` SHALL embed the nested `MappingDelegate` for that `TypePair` directly into
   the parent compiled delegate at delegate-compilation time, so that no additional delegate
   lookup occurs at mapping call time.
2. WHEN no explicit registration exists for a nested `TypePair`, THE `MapperRegistry` SHALL apply
   convention mapping for that pair, compile and cache a delegate, and embed it into the parent
   delegate.
3. WHEN the source nested member value is `null` and the destination member is a nullable
   reference type, THE `Mapper` SHALL assign `null` to the destination member without throwing.
4. WHEN the source nested member value is `null` and the destination member is a non-nullable
   value type, THE `Mapper` SHALL assign `default(TDestMember)` to the destination member without
   throwing.
5. THE `Mapper` SHALL correctly map an object graph with at least 10 levels of nested Complex-type
   properties without throwing a `StackOverflowException`.
6. IF convention mapping for a nested `TypePair` cannot produce a valid delegate (no satisfiable
   constructor and no matched members), THEN THE `MapperRegistry` SHALL throw
   `InvalidOperationException` at delegate-compilation time naming the unresolvable `TypePair`,
   so that the failure is detected before any mapping call.

---

### Requirement 6: Collection mapping

**User Story:** As a developer mapping objects with collection properties (lists, arrays,
enumerables), I want the mapper to automatically translate source collections to the correct
destination collection type, so that I do not have to write manual LINQ projections for each
collection property.

#### Acceptance Criteria

1. WHEN a source member is a Collection and the destination member is a Collection, THE `Mapper`
   SHALL resolve the `MappingDelegate` for the element `TypePair` from the `MapperRegistry`
   (applying convention mapping if not explicitly registered), map each element, and materialise
   the result into the destination collection type.
2. THE `Mapper` SHALL support the following destination collection types, using the specified
   concrete backing type at runtime: `T[]` (array), `List<T>` (List), `IList<T>` (backed by
   `List<T>`), `ICollection<T>` (backed by `List<T>`), `IEnumerable<T>` (backed by `List<T>`),
   `IReadOnlyList<T>` (backed by `List<T>`), `IReadOnlyCollection<T>` (backed by `List<T>`).
3. IF the source collection property value is `null` and the destination member is a nullable
   reference type, THEN THE `Mapper` SHALL assign `null` to the destination member.
4. IF the source collection property value is `null` and the destination member is a non-nullable
   reference type, THEN THE `Mapper` SHALL assign an empty collection of the resolved concrete
   backing type to the destination member.
5. WHEN the source collection is empty (zero elements), THE `Mapper` SHALL assign an empty
   collection of the correct destination concrete backing type to the destination member.
6. THE `Mapper` SHALL expose a top-level `Map<TSource, TTarget>(IEnumerable<TSource> source)`
   overload that resolves the delegate from the `MapperRegistry`, maps each element, and returns
   `IEnumerable<TTarget>` without performing per-call reflection.

---

### Requirement 7: Constructor parameter mapping

**User Story:** As a developer working with immutable `record` types and classes that only expose
constructor parameters (no public setters), I want the mapper to populate constructor arguments
from source properties, so that immutable target types can be mapped without compromising their
design.

#### Acceptance Criteria

1. WHEN the target type has no parameterless constructor, THE `MapperRegistry` SHALL select, at
   delegate-compilation time, the public constructor whose parameters are all satisfied — where a
   parameter is "satisfied" if a source member with a matching name (case-insensitive) and an
   assignable type exists, OR a `ForCtorParam` rule names that parameter, OR the parameter has a
   declared default value — preferring the constructor with the greatest number of parameters;
   IF two constructors have the same number of parameters, the first constructor in the order
   returned by `Type.GetConstructors()` SHALL be selected.
2. WHEN a constructor parameter name matches a source member name (case-insensitive) and the
   source member type is assignable to the parameter type, THE `MapperRegistry` SHALL include a
   `MemberExpression` for that source member as the argument for that parameter in the compiled
   `NewExpression`.
3. WHEN a `ForCtorParam` rule is registered for a parameter name that exists on the selected
   constructor, THE `MapperRegistry` SHALL use the rule's source expression as the argument for
   that parameter in preference to any convention-based match.
4. IF a `ForCtorParam` rule names a parameter that does not exist on the selected constructor,
   THEN THE `MapperRegistry` SHALL throw `InvalidOperationException` at delegate-compilation
   time, naming the `TypePair` and the unresolved parameter name.
5. WHEN a constructor parameter has a declared default value and no matching source member and no
   `ForCtorParam` rule exists for that parameter, THE `MapperRegistry` SHALL use a
   `ConstantExpression` representing the declared default value as the argument for that parameter
   in the compiled `NewExpression`.
6. IF no public constructor is fully satisfiable for the target type (i.e., at least one parameter
   has no matching source member, no `ForCtorParam` rule, and no declared default value for every
   public constructor), THEN THE `MapperRegistry` SHALL throw `InvalidOperationException` at
   delegate-compilation time with a message that names the target type and lists all unsatisfied
   parameter names.

---

### Requirement 8: LINQ queryable projection

**User Story:** As a developer building EF Core queries, I want to project a queryable source
sequence directly to a target type using the mapper configuration, so that the database server
executes the projection and only the required columns are fetched.

#### Acceptance Criteria

1. THE `IObjectMapper` interface SHALL expose a `ProjectTo<TTarget>(IQueryable<TSource> source)`
   method that returns `IQueryable<TTarget>`.
2. WHEN `ProjectTo<TTarget>` is called, THE `Mapper` SHALL construct an
   `Expression<Func<TSource, TTarget>>` from the registered (or convention-derived)
   `MappingExpression<TSource, TTarget>` and pass it to `IQueryable<TSource>.Select`, so that no
   `TSource` object is ever materialised in application memory; WHEN a convention member requires
   `Convert.ChangeType` (i.e., the source and destination Simple types are not directly
   assignable), THE `ProjectTo` SHALL throw `NotSupportedException` for that member because
   `Convert.ChangeType` is not translatable by LINQ query providers.
3. WHEN a destination member is configured with `Ignore`, THE `Mapper` SHALL omit that member
   from the `Expression<Func<TSource, TTarget>>` and assign its CLR default value in the
   expression tree.
4. WHEN a `ForMember` rule uses an arbitrary `Func<TSource, TProperty>` delegate resolver (not an
   `Expression<Func<TSource, TProperty>>`), THE `ProjectTo` SHALL throw `NotSupportedException`
   naming the destination member, because closed-over delegates cannot be inspected by a LINQ
   query provider.
5. WHEN `ForCtorParam` rules are present and the target type's constructor is used, THE
   `Mapper` SHALL produce a `NewExpression` with named argument expressions so that the LINQ
   query provider can inspect the constructor call without compiling the delegate.
6. IF no registration and no convention mapping can be derived for the `TypePair (TSource,
TTarget)` using only pure member expressions, THEN `ProjectTo<TTarget>` SHALL throw
   `InvalidOperationException` naming the `TypePair`.
7. IF `source` passed to `ProjectTo<TTarget>` is `null`, THEN THE `Mapper` SHALL throw
   `ArgumentNullException` with the parameter name `"source"`.

---

### Requirement 9: DI registration and `IObjectMapper` interface

**User Story:** As a developer wiring up the Acontplus.Utilities library in an ASP.NET Core
application, I want a clean DI registration extension that accepts mapping profiles and registers
`IObjectMapper` as a singleton, so that I can inject the mapper anywhere without referencing
internal types.

#### Acceptance Criteria

1. THE `UtilitiesServiceExtensions` SHALL expose an
   `AddObjectMapper(this IServiceCollection services, params MappingProfile[] profiles)` method
   that registers `IObjectMapper` as `ServiceLifetime.Singleton`; IF any `ForMember` expression
   targets a non-existent destination member or any `ForMember` resolver produces a type not
   assignable to the destination member type, THE method SHALL throw `InvalidOperationException`
   with a message that names each misconfigured member rather than silently swallowing the error.
2. WHEN `AddObjectMapper` is called with one or more `MappingProfile` instances, THE
   `MapperRegistry` SHALL build and compile delegates for all explicitly registered `TypePair`s
   during `IServiceProvider` construction (i.e., inside the singleton factory executed at
   `BuildServiceProvider` time), so that no delegate compilation occurs on the first mapping call
   after the application starts.
3. THE `IObjectMapper` interface SHALL expose at minimum the following members:
   - `TTarget Map<TSource, TTarget>(TSource source)`
   - `TTarget Map<TSource, TTarget>(TSource source, TTarget destination)`
   - `IEnumerable<TTarget> Map<TSource, TTarget>(IEnumerable<TSource> source)`
   - `IQueryable<TTarget> ProjectTo<TTarget>(IQueryable<TSource> source)`
4. IF `AddObjectMapper` is called with zero profiles, THE `Mapper` SHALL register successfully
   and resolve convention-based mappings on demand at first use per `TypePair`.
5. THE `IObjectMapper` registration SHALL use `ServiceLifetime.Singleton` so that the compiled
   delegate cache is shared across all requests for the lifetime of the application.
6. IF `AddObjectMapper` is called when `IObjectMapper` is already registered in the
   `IServiceCollection`, THEN THE method SHALL throw `InvalidOperationException` with a message
   indicating that `IObjectMapper` has already been registered, to prevent non-deterministic
   singleton resolution.

---

### Requirement 10: Null-safety and error reporting

**User Story:** As a library consumer, I want the mapper to handle null inputs gracefully and
report misconfiguration errors at startup (not at first mapping call), so that my application
fails fast on bad configuration and never throws NullReferenceException during normal use.

#### Acceptance Criteria

1. WHEN `Map<TSource, TTarget>(null)` is called, THE `Mapper` SHALL return `default(TTarget)`
   without throwing.
2. WHEN `Map<TSource, TTarget>(source, null)` is called with a null destination argument, THE
   `Mapper` SHALL throw `ArgumentNullException` with `ParamName` equal to `"destination"`.
3. WHEN `MapperConfiguration.Build()` is called, THE `MapperRegistry` SHALL validate all
   explicitly registered `TypePair`s in a single pass, collecting all violations — both
   non-existent destination members and type-incompatible `ForMember` resolvers — across all
   registered `TypePair`s; IF one or more violations are found, THE `MapperRegistry` SHALL throw
   a single `InvalidOperationException` whose message lists every violation as
   `"<TypePairDescription>: member '<MemberName>' — resolved type '<ResolvedType>' is not
assignable to destination type '<DestinationType>'"` (or `"member '<MemberName>' does not
exist on <TargetType>"` for the non-existent case), and SHALL NOT produce a usable mapper
   instance.
4. WHEN `MapperConfiguration.Build()` completes without throwing, THE `MapperRegistry` SHALL be
   fully initialised with compiled delegates for all explicitly registered `TypePair`s and ready
   to serve mapping requests.
5. IF a mapping cycle is detected during delegate compilation (e.g., `A → B → A`), THEN
   `MapperConfiguration.Build()` SHALL complete without throwing, and the compiled delegate SHALL
   produce correct mapping results when invoked at runtime.

---

### Requirement 11: Backward compatibility shim

**User Story:** As a developer who has existing code using the current static `ObjectMapper` API,
I want the redesigned implementation to maintain a static compatibility surface, so that my
existing call sites continue to compile without modification during the migration period.

#### Acceptance Criteria

1. THE `ObjectMapper` static class SHALL retain `CreateMap<TSource, TTarget>()`, `ForMember`,
   `Ignore`, and `ForCtorParam` as static or fluent-returning methods with exactly the same
   method names, parameter types, and return types as the current implementation, so that
   existing call sites compile without modification.
2. THE `ObjectMapper` static class SHALL retain `Map<TSource, TTarget>(TSource source)` and
   `Map<TSource, TTarget>(TSource source, TTarget destination)` static methods with exactly the
   same signatures as the current implementation.
3. WHEN any static method on `ObjectMapper` is invoked, the call SHALL be forwarded to the
   default singleton `IObjectMapper` instance; the default singleton SHALL be non-null and fully
   operational (capable of accepting registrations and executing maps) before the first static
   method call returns; IF the singleton has not been initialised, THE `ObjectMapper` SHALL throw
   `InvalidOperationException` with a message instructing the caller to invoke `AddObjectMapper`
   during application startup.
4. THE `ObjectMapper` static class SHALL be annotated with
   `[Obsolete("Use IObjectMapper via DI. The static API will be removed in v3.0.")]` so that
   consuming code receives a compiler warning at the call site.
5. IF the static `CreateMap<TSource, TTarget>()` is called after `MapperConfiguration.Build()`
   has been finalised, THEN THE `ObjectMapper` SHALL synchronously compile and register the new
   delegate within the same call so that the `TypePair` is resolvable by subsequent `Map` calls
   from any thread.
6. WHEN the `ObjectMapper` static class is loaded by the runtime before `AddObjectMapper` has
   been called, THE static initialiser SHALL NOT throw an exception; the singleton reference
   SHALL remain `null` and only throw `InvalidOperationException` when a static mapping method
   (not the class load itself) is first invoked without initialisation.
