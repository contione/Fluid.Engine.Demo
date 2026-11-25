namespace Fluid.Engine.Demo.Fields;

// Replace the implementation when database-backed configuration is needed.
public interface IFieldRegistry
{
    IReadOnlyDictionary<string, FieldDefinition> GetFields(IEnumerable<string> keys);
}
