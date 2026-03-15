# Istarion.PatchDTO

Source Generator for automatic generation of PatchDTO classes for partial updates based on projections.

## Usage

1. Add a reference to the project in your project:
```
dotnet add package Istarion.Falcorm
```

2. Define a projection method with `[PatchDto]` attribute (use `Async = true` for async version):

```csharp
public class Resource
{
    public int Id { get; set; }
    public DateTime Ts { get; set; }
    public string? Name { get; set; }
    public int TypeId { get; set; }
    public string? Description { get; set; }
    public bool Active { get; set; }
}

public class MyMappers
{
    [PatchDto]
    object Projection(Resource entity)
        => new
        {
            Id = ReadOnly.Value(entity.Id),              // Readonly - excluded from PatchDTO
            Ts = ReadOnly.Value(entity.Ts),              // Readonly - excluded from PatchDTO
            entity.Name,                                 // Simple access - getter + setter
            entity.TypeId,                               // Simple access - getter + setter
            entity.Description,                          // Simple access - getter + setter
            Active = Writable(1),                        // Computed but explicitly writable - auto-property {get; set;}
            Status = entity.Active ? "active" : "inactive"  // Computed - excluded from PatchDTO
        };
    
    [PatchDto(Async = true)]
    object ProjectionAsync(Resource entity)
        => new
        {
            Id = ReadOnly.Value(entity.Id),      // Readonly - excluded from PatchDto
            entity.Name,                         // Simple access
            entity.TypeId,                       // Simple access
            Active = Writable(entity.Active ? 1 : 0)  // Computed but writable - included
        };
}
```

**Attribute parameters:**
- `Name` (string, optional): Custom name for the DTO class. If not specified, defaults to `{MethodName}Dto`. 

3. The generator will create nested DTO classes in the same class. The class name is based on the method name: `{MethodName}Dto`:

**Synchronous version (`[PatchDto]`):**
```csharp
public partial class MyMappers
{
    public partial class ProjectionDto
    {
        [ThreadStatic]
        static Resource? target;
        
        private readonly Resource? _target;
        
        private ProjectionDTO()
        {
            _target = target;
        }
        
        // ReadOnly fields (Id, Ts) are excluded from ProjectionDto
        // Computed fields without Writable (Status) are excluded from ProjectionDto
        
        public string? Name { get => _target!.Name; set => _target!.Name = value; }
        public int TypeId { get => _target!.TypeId; set => _target!.TypeId = value; }
        public string? Description { get => _target!.Description; set => _target!.Description = value; }
        public int Active { get; set; }  // Writable - regular auto-property with its own value
        
        public static ProjectionDto Load<T>(Resource resource, T state, Func<T, ProjectionDto> loader)
        {
            ArgumentNullException.ThrowIfNull(resource);
            ArgumentNullException.ThrowIfNull(loader);
            
            var previous = target;
            target = resource;
            try
            {
                return loader(state);
            }
            finally
            {
                target = previous;
            }
        }
    }
}
```

**Asynchronous version (`[PatchDto(Async = true)]`):**
```csharp
public partial class MyMappers
{
    public partial class ProjectionAsyncDto
    {
        static readonly AsyncLocal<Resource?> target = new();
        
        private readonly Resource? _target;
        
        private ProjectionAsyncDto()
        {
            _target = target.Value;
        }
        
        // ReadOnly fields (Id) are excluded from ProjectionAsyncDto
        
        public string? Name { get => _target!.Name; set => _target!.Name = value; }
        public int TypeId { get => _target!.TypeId; set => _target!.TypeId = value; }
        public int Active { get; set; }  // Writable - regular auto-property with its own value
        
        public static async Task<ProjectionAsyncDto> LoadAsync<T>(Resource resource, T state, Func<T, Task<ProjectionAsyncDto>> loader)
        {
            ArgumentNullException.ThrowIfNull(resource);
            ArgumentNullException.ThrowIfNull(loader);
            
            var previous = target.Value;
            target.Value = resource;
            try
            {
                return await loader(state);
            }
            finally
            {
                target.Value = previous;
            }
        }
    }
   
```

```

4. Usage:

**Synchronous version:**
```csharp
var entity = new Resource { Name = "old", TypeId = 100 };

var json = """{"name": "new name", "typeId": 200, "active": 1}""";

var patch = MyMappers.ProjectionDto.Load(entity, json, jsonStr =>
    JsonSerializer.Deserialize<MyMappers.ProjectionDto>((string)jsonStr));

// With JsonSerializerContext for AOT mode:
var patch = MyMappers.ProjectionDto.Load(entity, json, jsonStr =>
    JsonSerializer.Deserialize<MyMappers.ProjectionDto>(
        jsonStr, 
        DtoJsonContext.Default.MyMappersProjectionDto));

// Setters automatically update entity via ThreadStatic target
// entity.Name = "new name", entity.TypeId = 200
// patch.Active contains deserialized value (1) that can be used for further processing
```

**Asynchronous version (async-safe):**
```csharp
var entity = new Resource { Name = "old", TypeId = 100 };

var json = """{"name": "new name", "typeId": 200}""";

var patch = await MyMappers.ProjectionAsyncDto.LoadAsync(entity, json, async jsonStr =>
    await JsonSerializer.DeserializeAsync<MyMappers.ProjectionAsyncDto>(...));
// Setters automatically update entity via AsyncLocal target
// Safe for async/await contexts
```

## Setter generation rules

**Get setters:**
- `entity.Property` — direct property access (proxy to target)
- `entity.Field` — direct field access (proxy to target)
- `Property = Writable(expression)` — explicitly marked as writable, generates regular auto-property `{get; set;}` with its own value

**Excluded from PatchDTO:**
- `ReadOnly.Value(entity.Property)` — explicitly marked as readonly, completely excluded
- `Property = constant` — constant values (without Writable), completely excluded
- `Property = entity.Property + "suffix"` — computed expressions (without Writable), completely excluded
- `Property = entity.Property?.ToString()` — method calls (without Writable), completely excluded
- `Property = entity.Property.SubProperty` — property chains (without Writable), completely excluded
- Any binary operations, ternary operators, etc. (without Writable), completely excluded

## Features

- **Nested class**: DTO classes are generated as nested partial classes in the same class where the method is defined
- **Dynamic naming**: Class name is based on method name: `{MethodName}Dto` for both `[PatchDto]` and `[PatchDto(Async = true)]`
- **ThreadStatic projection** (`[PatchDto]`): regular properties are projected to target via ThreadStatic variable
- **AsyncLocal projection** (`[PatchDto(Async = true)]`): regular properties are projected to target via AsyncLocal variable (async-safe)
- **Automatic update**: during deserialization, setters of regular properties automatically update target
- **Readonly fields**: via `ReadOnly.Value()` are completely excluded from PatchDTO
- **Writable fields**: via `Writable()` generate regular auto-properties `{get; set;}` with their own value that is deserialized and can be used for further processing
- **Computed fields**: without `Writable()` are completely excluded from PatchDTO
- **JsonSerializerContext generation**: optional `GenerateJsonContext` parameter enables source-generated JSON serialization. One `DtoJsonContext` is generated per assembly, containing all DTOs with `GenerateJsonContext = true`. If `Name` parameter is not specified, DTO class name defaults to `{MethodName}Dto`, and JsonContext property name is prefixed with containing class name (e.g., `MyMappersProjectionDto`) to avoid conflicts. Access via `DtoJsonContext.Default.{PropertyName}`
- **Universal**: works with any serialization (JSON, XML, etc.)
- **Zero reflection**: all code is generated at compile time
