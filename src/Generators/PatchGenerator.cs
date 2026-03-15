using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Istarion.PatchDTO;

[Generator]
public class Generator : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // Find methods with [PatchDto] attribute
    var provider = context.SyntaxProvider
        .CreateSyntaxProvider(
            predicate: static (node, _) => IsSyntaxTargetForGeneration(node),
            transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
        .Where(static m => m is not null);

    // Generate DTO classes for each method
    context.RegisterSourceOutput(provider, Execute);
  }

  static bool IsSyntaxTargetForGeneration(SyntaxNode node)
      => node is MethodDeclarationSyntax { AttributeLists.Count: > 0 };

  static MethodInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
  {
    var methodDecl = (MethodDeclarationSyntax)context.Node;

    foreach (var attributeList in methodDecl.AttributeLists)
    {
      foreach (var attribute in attributeList.Attributes)
      {
        if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol attrSymbol)
          continue;

        var attrName = attrSymbol.ContainingType.ToDisplayString();
        var generateJson = true; // Default to true
        var isAsync = true; // Default to async
        string? dtoName = null;

        if (attrName == "Nitro.PatchDtoAttribute" || attrName.EndsWith(".PatchDtoAttribute"))
        {
          if (attribute is AttributeSyntax attrSyntax)
            foreach (var arg in attrSyntax.ArgumentList?.Arguments ?? [])
            {
              // Handle named arguments (NameEquals)
              if (arg.NameEquals != null)
              {
                switch (arg.NameEquals.Name.Identifier.ValueText)
                {
                  case "Async":
                    if (arg.Expression is LiteralExpressionSyntax l1)
                    {
                      isAsync = l1.Kind() == SyntaxKind.TrueLiteralExpression;
                    }
                    break;

                  case "Json":
                    if (arg.Expression is LiteralExpressionSyntax l2)
                    {
                      generateJson = l2.Kind() == SyntaxKind.TrueLiteralExpression;
                    }
                    break;

                  case "Name":
                    if (arg.Expression is LiteralExpressionSyntax l3 && l3.Kind() == SyntaxKind.StringLiteralExpression)
                      dtoName = l3.Token.ValueText;
                    break;
                }
              }
            }

          var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDecl);
          if (methodSymbol is IMethodSymbol method)
            return new MethodInfo(method, methodDecl, context.SemanticModel, isAsync, generateJson, dtoName);
        }
      }
    }

    return null;
  }

  void Execute(SourceProductionContext context, MethodInfo? methodInfo)
  {
    if (methodInfo == null) return;

    try
    {
      var source = GeneratePatchDto(methodInfo);
      var containingClass = methodInfo.Method.ContainingType.Name;
      var methodName = methodInfo.Method.Name;
      var className = $"{methodName}Dto";

      // Debug: report if no properties were found
      var methodSymbol = methodInfo.SemanticModel.GetDeclaredSymbol(methodInfo.Syntax);
      if (methodSymbol != null && methodSymbol.Parameters.Length > 0)
      {
        var projectionProperties = AnalyzeProjection(methodInfo.Syntax, methodInfo.SemanticModel, methodSymbol.Parameters[0].Name);
        if (projectionProperties.IsEmpty)
        {
          // Try to find anonymous object to provide better error message
          var body = (CSharpSyntaxNode?)methodInfo.Syntax.Body ?? methodInfo.Syntax.ExpressionBody?.Expression;
          var hasAnonymousObject = body?.DescendantNodesAndSelf().OfType<AnonymousObjectCreationExpressionSyntax>().Any() ?? false;

          context.ReportDiagnostic(Diagnostic.Create(
              new DiagnosticDescriptor(
                  "PATCHGEN003",
                  "No properties found",
                  hasAnonymousObject
                    ? "No properties found in projection method {0}.{1}. All properties are computed expressions (ReadOnly, binary operations, etc.) and excluded. Use Writable() for computed properties that should be included."
                    : "No anonymous object found in projection method {0}.{1}. Make sure the method returns an anonymous object with properties.",
                  "PatchGenerator",
                  DiagnosticSeverity.Warning,
                  true),
              methodInfo.Syntax.GetLocation(),
              containingClass,
              methodName));
        }
      }

      context.AddSource($"{containingClass}.{methodName}.{className}.g.cs", SourceText.From(source, Encoding.UTF8));
    }
    catch (Exception ex)
    {
      context.ReportDiagnostic(Diagnostic.Create(
          new DiagnosticDescriptor(
              "PATCHGEN001",
              "Patch generation failed",
              "Failed to generate patch for {0}: {1}",
              "PatchGenerator",
              DiagnosticSeverity.Error,
              true),
          methodInfo.Syntax.GetLocation(),
          methodInfo.Method.Name,
          ex.Message));
    }
  }

  string GeneratePatchDto(MethodInfo methodInfo)
  {
    var sb = new StringBuilder();
    var method = methodInfo.Method;
    var methodSyntax = methodInfo.Syntax;
    var semanticModel = methodInfo.SemanticModel;

    // Determine entity type from method parameter
    if (method.Parameters.Length == 0)
      throw new InvalidOperationException("Method must have at least one parameter (entity)");

    var entityParam = method.Parameters[0];
    var entityType = entityParam.Type;
    var entityTypeName = GetFullTypeName(entityType);

    // Determine containing class
    var containingType = method.ContainingType;
    var containingTypeName = containingType.Name;
    var ns = containingType.ContainingNamespace.ToDisplayString();
    var containingTypeAccessibility = GetAccessibilityModifier(containingType.DeclaredAccessibility);

    // Analyze method body - find anonymous object
    if (entityType is not INamedTypeSymbol entityTypeSymbol)
      throw new InvalidOperationException("Entity type must be a named type");

    var projectionProperties = AnalyzeProjection(methodSyntax, semanticModel, entityParam.Name);

    // Usings
    sb.AppendLine("#nullable enable");
    sb.AppendLine("using System;");
    sb.AppendLine("using System.Runtime.CompilerServices;");
    if (methodInfo.IsAsync)
    {
      sb.AppendLine("using System.Threading;");
      sb.AppendLine("using System.Threading.Tasks;");
    }
    if (methodInfo.GenerateJson)
    {
      sb.AppendLine("using System.IO;");
      sb.AppendLine("using System.Text.Json;");
    }
    sb.AppendLine();

    // Namespace
    if (!string.IsNullOrEmpty(ns))
    {
      sb.AppendLine($"namespace {ns};");
      sb.AppendLine();
    }

    // DTO class at namespace level (not nested)
    // Class name: use explicit Name from attribute, or default to {ContainingClassName}{MethodName}
    var methodName = methodInfo.Method.Name;
    var dtoClassName = !string.IsNullOrEmpty(methodInfo.DtoName) ? methodInfo.DtoName : $"{containingTypeName}{methodName}";
    sb.AppendLine($"public partial class {dtoClassName}()");
    sb.AppendLine("{");
    // ThreadStatic or AsyncLocal target (for Load method) - must be declared before _target
    if (methodInfo.IsAsync)
    {
      sb.AppendLine($"  static readonly AsyncLocal<{entityTypeName}?> target = new();");
      sb.AppendLine($"  readonly {entityTypeName} _target = target.Value!;");
    }
    else
    {
      sb.AppendLine($"  [ThreadStatic]");
      sb.AppendLine($"  static {entityTypeName}? target;");
      sb.AppendLine($"  readonly {entityTypeName} _target = target!;");
    }
    sb.AppendLine();

    // Generate properties
    foreach (var prop in projectionProperties)
      GenerateProperty(sb, prop);

    // Load<T> or LoadAsync<T> method
    sb.AppendLine();
    if (methodInfo.IsAsync)
    {
      sb.AppendLine($"  public static async Task<{dtoClassName}?> LoadAsync<T>({entityTypeName} resource, T state, Func<T, Task<{dtoClassName}?>> loader)");
      sb.AppendLine("  {");
      sb.AppendLine("    ArgumentNullException.ThrowIfNull(resource);");
      sb.AppendLine("    ArgumentNullException.ThrowIfNull(loader);");
      sb.AppendLine();
      sb.AppendLine("    var previous = target.Value;");
      sb.AppendLine("    target.Value = resource;");
      sb.AppendLine("    try");
      sb.AppendLine("    {");
      sb.AppendLine("      return await loader(state);");
      sb.AppendLine("    }");
      sb.AppendLine("    finally");
      sb.AppendLine("    {");
      sb.AppendLine("      target.Value = previous;");
      sb.AppendLine("    }");
      sb.AppendLine("  }");
    }
    else
    {
      sb.AppendLine($"  public static {dtoClassName}? Load<T>({entityTypeName} resource, T state, Func<T, {dtoClassName}?> loader)");
      sb.AppendLine("  {");
      sb.AppendLine("    ArgumentNullException.ThrowIfNull(resource);");
      sb.AppendLine("    ArgumentNullException.ThrowIfNull(loader);");
      sb.AppendLine();
      sb.AppendLine("    var previous = target;");
      sb.AppendLine("    target = resource;");
      sb.AppendLine("    try");
      sb.AppendLine("    {");
      sb.AppendLine("      return loader(state);");
      sb.AppendLine("    }");
      sb.AppendLine("    finally");
      sb.AppendLine("    {");
      sb.AppendLine("      target = previous;");
      sb.AppendLine("    }");
      sb.AppendLine("  }");
    }

    // Generate Stream overload if GenerateJson is enabled
    if (methodInfo.GenerateJson)
    {
      sb.AppendLine();
      if (methodInfo.IsAsync)
      {
        sb.AppendLine($"  public static async Task<{dtoClassName}?> LoadJsonAsync({entityTypeName} resource, Stream stream)");
        sb.AppendLine($"    => await LoadAsync(resource, stream, async (s) => await JsonSerializer.DeserializeAsync<{dtoClassName}>(s));");
      }
      else
      {
        sb.AppendLine($"  public static {dtoClassName}? LoadJson({entityTypeName} resource, Stream stream)");
        sb.AppendLine($"    => Load(resource, stream, (s) => JsonSerializer.Deserialize<{dtoClassName}>(s));");
      }
    }
    sb.AppendLine("}");

    return sb.ToString();
  }


  ImmutableArray<PropertyInfo> AnalyzeProjection(MethodDeclarationSyntax methodSyntax, SemanticModel semanticModel, string entityParamName)
  {
    var properties = ImmutableArray.CreateBuilder<PropertyInfo>();

    // Find anonymous object in method body
    var body = (CSharpSyntaxNode?)methodSyntax.Body ?? methodSyntax.ExpressionBody?.Expression;
    if (body == null) return properties.ToImmutable();

    // Find AnonymousObjectCreationExpression
    // Use DescendantNodesAndSelf to include the body itself if it's already an anonymous object
    var anonymousObject = body.DescendantNodesAndSelf()
        .OfType<AnonymousObjectCreationExpressionSyntax>()
        .FirstOrDefault();

    if (anonymousObject == null) return properties.ToImmutable();

    // Determine entity type from method parameter
    var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax);
    if (methodSymbol == null || methodSymbol.Parameters.Length == 0)
      return properties.ToImmutable();

    var entityParam = methodSymbol.Parameters[0];
    if (entityParam.Type is not INamedTypeSymbol entityType) return properties.ToImmutable();

    // Use actual parameter name from method symbol, not the passed parameter
    var actualParamName = entityParam.Name;

    // Analyze each property of the anonymous object
    foreach (var initializer in anonymousObject.Initializers)
    {
      var propInfo = AnalyzePropertyInitializer(initializer, semanticModel, actualParamName, entityType);
      // AnalyzePropertyInitializer returns null for computed expressions (except Writable)
      if (propInfo != null)
        properties.Add(propInfo);
    }

    return properties.ToImmutable();
  }

  PropertyInfo? AnalyzePropertyInitializer(AnonymousObjectMemberDeclaratorSyntax initializer, SemanticModel semanticModel, string entityParamName, INamedTypeSymbol entityType)
  {
    // Determine property name
    string? propName = null;
    ExpressionSyntax? expression = null;
    bool isReadOnly = false;
    bool isWritable = false;
    ITypeSymbol? propertyType = null;
    string? defaultValue = null;

    if (initializer.NameEquals != null)
    {
      // Explicit name: Name = expression
      propName = initializer.NameEquals.Name.Identifier.ValueText;
      expression = initializer.Expression;
    }
    else
    {
      // Short form: entity.Property
      expression = initializer.Expression;
      if (expression is MemberAccessExpressionSyntax memberAccess)
      {
        propName = memberAccess.Name.Identifier.ValueText;
      }
    }

    if (propName == null || expression == null) return null;

    // Check if expression is a simple access to entity.Property
    bool isSimplePropertyAccess = IsSimplePropertyAccess(expression, semanticModel, entityParamName);

    // If it's a simple property access - include it
    if (!isSimplePropertyAccess)
    {
      // It's a computed expression - check if it's Writable
      if (expression is InvocationExpressionSyntax invocation)
      {
        var invokedSymbol = semanticModel.GetSymbolInfo(invocation).Symbol;
        if (invokedSymbol is IMethodSymbol method)
        {
          var methodName = method.Name;
          var containingType = method.ContainingType.ToDisplayString();

          // Only Writable is allowed for computed expressions
          if ((methodName == "Value" && containingType.Contains("Writable")) || methodName == "Writable")
          {
            isWritable = true;
            // Extract argument from Writable(expression) or Writable.Value(expression)
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
              expression = invocation.ArgumentList.Arguments[0].Expression;
            }
            // Check for second parameter (default value)
            if (invocation.ArgumentList.Arguments.Count > 1)
            {
              var secondArg = invocation.ArgumentList.Arguments[1].Expression;
              // Try to extract constant value
              var constantValue = semanticModel.GetConstantValue(secondArg);
              if (constantValue.HasValue)
              {
                defaultValue = FormatConstantValue(constantValue.Value, secondArg);
              }
              else if (secondArg is LiteralExpressionSyntax literal)
              {
                // For literals, use the token text directly (it's already formatted)
                defaultValue = literal.ToString();
              }
              else
              {
                // For non-constant expressions, use string representation
                defaultValue = secondArg.ToString();
              }
            }
          }
          else
          {
            // Any other method call (ReadOnly, or any other function) is excluded
            return null; // Exclude computed expressions that are not Writable
          }
        }
        else
        {
          // Not a method call, but still computed - exclude it
          return null;
        }
      }
      else
      {
        // Not a simple access and not a method call (e.g., binary operation, constant) - exclude it
        return null;
      }
    }
    // If isSimplePropertyAccess == true, continue processing below

    // Determine property type from entity.Property
    // Also determine the original entity property name for setter generation
    string? entityPropertyName = null;

    // If it's Writable, use expression type directly
    if (isWritable)
    {
      var typeInfo = semanticModel.GetTypeInfo(expression);
      propertyType = typeInfo.Type;
      // For Writable, we don't need entityPropertyName as it's a regular auto-property
    }
    else
    {
      // First try to find via member access
      if (expression is MemberAccessExpressionSyntax memberAccess2)
      {
        var symbolInfo = semanticModel.GetSymbolInfo(memberAccess2);
        var memberSymbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
        if (memberSymbol is IPropertySymbol prop)
        {
          propertyType = prop.Type;
          entityPropertyName = prop.Name; // Store original property name from entity
        }
        else if (memberSymbol is IFieldSymbol field)
        {
          propertyType = field.Type;
          entityPropertyName = field.Name; // Store original field name from entity
        }
      }

      // If not found via member access, search for property in entity type by name
      // This handles cases where propName matches entity property name (no renaming)
      if (propertyType == null)
      {
        var entityMember = entityType.GetMembers(propName).FirstOrDefault();
        if (entityMember is IPropertySymbol entityProp)
        {
          propertyType = entityProp.Type;
          entityPropertyName = propName; // No renaming, use propName
        }
        else if (entityMember is IFieldSymbol entityField)
        {
          propertyType = entityField.Type;
          entityPropertyName = propName; // No renaming, use propName
        }
      }

      // If still not found, use expression type as fallback
      // This handles cases where GetSymbolInfo failed but GetTypeInfo works
      if (propertyType == null)
      {
        var typeInfo = semanticModel.GetTypeInfo(expression);
        propertyType = typeInfo.Type;
        // If we got type from GetTypeInfo, try to determine entity property name from member access
        if (propertyType != null && expression is MemberAccessExpressionSyntax memberAccess3)
        {
          var memberName = memberAccess3.Name.Identifier.ValueText;
          // Check if this member exists in entity type
          var entityMember = entityType.GetMembers(memberName).FirstOrDefault();
          if (entityMember != null)
          {
            entityPropertyName = memberName;
          }
        }
      }
    }

    if (propertyType == null) return null;

    return new PropertyInfo(propName, propertyType, isReadOnly, isWritable, entityPropertyName, defaultValue);
  }

  // Simple access: only entity.Property or entity.Field
  // Without operations, method calls, constants, etc.
  static bool IsSimplePropertyAccess(ExpressionSyntax expression, SemanticModel semanticModel, string entityParamName)
  {
    if (expression is not MemberAccessExpressionSyntax memberAccess)
      return false;

    // Check that the expression part is a simple identifier matching entity parameter name
    var expr = memberAccess.Expression;
    if (expr is IdentifierNameSyntax identifier)
    {
      return identifier.Identifier.ValueText == entityParamName;
    }

    // Also handle cases where expression might be a parenthesized expression wrapping identifier
    if (expr is ParenthesizedExpressionSyntax parenExpr &&
        parenExpr.Expression is IdentifierNameSyntax parenIdentifier)
    {
      return parenIdentifier.Identifier.ValueText == entityParamName;
    }

    return false;
  }

  void GenerateProperty(StringBuilder sb, PropertyInfo prop)
  {
    var propName = prop.Name;
    var propType = prop.Type;

    // Writable fields - regular auto-properties with their own value (always nullable)
    if (prop.IsWritable)
    {
      var writableTypeName = GetFullTypeName(propType);
      // Make writable properties nullable
      var nullableWritableTypeName = MakeNullable(writableTypeName, propType);
      // Add default value if provided
      var initializer = prop.DefaultValue != null ? $" = {prop.DefaultValue};" : "";
      sb.AppendLine($"  public {nullableWritableTypeName} {propName} {{ get; set; }}{initializer}");
      return;
    }

    // Regular fields - proxy to _target instance field
    // Use entity property name for getter/setter if available (handles renamed properties)
    // Otherwise fall back to propName (for cases where name wasn't changed)
    var entityPropertyName = prop.EntityPropertyName ?? propName;

    // For proxy properties, preserve nullability from entity type
    var proxyTypeName = GetTypeNameWithNullability(propType);

    var setter = prop.IsReadOnly ? "" : $"set => _target!.{entityPropertyName} = value; ";

    sb.AppendLine($"  public {proxyTypeName} {propName} {{ get => _target.{entityPropertyName}; {setter}}}");

    // ReadOnly properties don't have setters
  }

  string GetTypeNameWithNullability(ITypeSymbol type)
  {
    var typeName = GetFullTypeName(type);

    // Check if typeName already ends with ? (avoid double ?)
    if (IsTypeNameNullable(typeName))
    {
      return typeName; // Already nullable
    }

    // Check if type is already nullable or is a value type
    if (type.IsValueType)
    {
      // Value types are not nullable unless they are Nullable<T>
      if (type is INamedTypeSymbol namedType && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
      {
        return typeName; // Already nullable
      }
      return typeName; // Value type, not nullable
    }

    // Reference types: check nullability annotation
    if (type.NullableAnnotation == NullableAnnotation.Annotated)
    {
      return $"{typeName}?";
    }

    // For reference types, preserve nullability from entity (if not annotated, don't add ?)
    return typeName;
  }

  string MakeNullable(string typeName, ITypeSymbol type)
  {
    // Check if typeName already ends with ? (avoid double ?)
    if (IsTypeNameNullable(typeName))
    {
      return typeName; // Already nullable
    }

    // For value types, check if it's already Nullable<T>
    if (type.IsValueType)
    {
      if (type is INamedTypeSymbol namedType && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
      {
        return typeName; // Already nullable
      }
      // For value types, make nullable
      return $"{typeName}?";
    }

    // For reference types, ALWAYS add ? for writable properties (they can be null from JSON)
    return $"{typeName}?";
  }

  string FormatConstantValue(object? value, ExpressionSyntax expression)
  {
    if (value == null) return "null";

    return value switch
    {
      string s => $"\"{s.Replace("\"", "\\\"")}\"",
      bool b => b ? "true" : "false",
      char c => $"'{c}'",
      _ => value.ToString() ?? "null"
    };
  }

  string GetFullTypeName(ITypeSymbol type)
  {
    if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
    {
      var name = namedType.Name.Split('`')[0];
      var args = string.Join(", ", namedType.TypeArguments.Select(GetFullTypeName));
      var ns = namedType.ContainingNamespace.ToDisplayString();
      return $"{ns}.{name}<{args}>";
    }

    var fullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    return fullName.Replace("global::", "");
  }

  bool IsTypeNameNullable(string typeName)
  {
    // Check if type name ends with ? (but not in the middle of generic parameters)
    // Need to check if ? is after the closing > of generic type
    if (!typeName.EndsWith("?")) return false;

    // If ends with ?>, it's a nullable generic parameter, not the type itself
    if (typeName.EndsWith("?>")) return false;

    // Check if ? is after the last > (meaning it's the type itself, not a generic parameter)
    var lastGreaterThan = typeName.LastIndexOf('>');
    if (lastGreaterThan >= 0)
    {
      // Has generics, check if ? is after the last >
      return typeName.Length > lastGreaterThan + 1 && typeName[lastGreaterThan + 1] == '?';
    }

    // No generics, just check if ends with ?
    return true;
  }

  string GetAccessibilityModifier(Accessibility accessibility)
  {
    return accessibility switch
    {
      Accessibility.Public => "public",
      Accessibility.Internal => "internal",
      Accessibility.Private => "private",
      Accessibility.Protected => "protected",
      Accessibility.ProtectedOrInternal => "protected internal",
      Accessibility.ProtectedAndInternal => "private protected",
      _ => "internal" // Default to internal if unknown
    };
  }

  record MethodInfo(IMethodSymbol Method, MethodDeclarationSyntax Syntax, SemanticModel SemanticModel, bool IsAsync = true, bool GenerateJson = true, string? DtoName = null);
  record PropertyInfo(string Name, ITypeSymbol Type, bool IsReadOnly, bool IsWritable = false, string? EntityPropertyName = null, string? DefaultValue = null);
}
