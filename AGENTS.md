## Development Guidelines

* Use the latest C# language features where appropriate.
* Prefer modern C# patterns, including primary constructors.
* Use `sealed record` types for MediatR request and response models.

## Dependency Injection

Use `AutoRegisterInject` for dependency registration.

Apply the appropriate attribute to classes that should be registered with the DI container:

* `[RegisterScoped]`
* `[RegisterSingleton]`
* `[RegisterTransient]`

Do not manually register services unless there is a specific reason.

## MediatR Usage

Use MediatR instead of direct dependency injection for application workflows.

Prefer sending requests through MediatR rather than injecting and calling services directly across layers.

MediatR handlers should use sealed records for request and response types.

## Build Verification

Verify builds only with:

```bash
dotnet build
```

Do not use alternative build or test commands unless explicitly requested.

## Layering Rules

Discord-specific types from `Accord.Bot` must not leak into any other layer.

Keep Discord concerns isolated within `Accord.Bot`.

Other layers should depend on application/domain abstractions, not Discord.NET or bot-specific types.

## Database Changes

When database model changes are required, do not create migrations.

Instead, inform the user that they must create the migration themselves.
