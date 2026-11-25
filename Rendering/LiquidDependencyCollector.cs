using System.Collections.Immutable;
using Fluid;
using Fluid.Ast;

namespace Fluid.Engine.Demo.Rendering;

internal sealed class LiquidDependencyCollector : AstVisitor
{
    private readonly List<string> _paths = [];
    private readonly HashSet<string> _locals = new(StringComparer.Ordinal);
    private int _blockDepth;

    public static ImmutableArray<string> Collect(IFluidTemplate template)
    {
        var collector = new LiquidDependencyCollector();
        collector.VisitTemplate(template);
        return [.. collector._paths.Distinct(StringComparer.Ordinal)];
    }

    public override Statement? Visit(Statement? statement)
    {
        if (statement is null) return null;
        if (statement is not (AssignStatement or ForStatement or IfStatement or ElseIfStatement or ElseStatement
            or OutputStatement or TextSpanStatement or CommentStatement or RawStatement or NoOpStatement
            or BreakStatement or ContinueStatement))
            throw new NotSupportedException("Unsupported Liquid statement.");

        var nested = statement is IfStatement or ForStatement;
        if (nested) _blockDepth++;
        var result = base.Visit(statement);
        if (nested) _blockDepth--;
        return result;
    }

    protected override Statement VisitAssignStatement(AssignStatement statement)
    {
        if (_blockDepth != 0) throw new NotSupportedException("Conditional assignments are not supported.");
        base.VisitAssignStatement(statement);
        _locals.Add(statement.Identifier);
        return statement;
    }

    protected override Statement VisitIfStatement(IfStatement statement)
    {
        Visit(statement.Condition);
        foreach (var child in statement.Statements) Visit(child);
        foreach (var branch in statement.ElseIfs) Visit(branch);
        Visit(statement.Else);
        return statement;
    }

    protected override Statement VisitForStatement(ForStatement statement)
    {
        Visit(statement.Source);
        Visit(statement.Range);
        Visit(statement.Limit);
        Visit(statement.Offset);

        var addedVariable = _locals.Add(statement.Identifier);
        var addedMetadata = _locals.Add("forloop");
        foreach (var child in statement.Statements) Visit(child);
        if (addedVariable) _locals.Remove(statement.Identifier);
        if (addedMetadata) _locals.Remove("forloop");
        Visit(statement.Else);
        return statement;
    }

    protected override Expression VisitMemberExpression(MemberExpression member)
    {
        if (member.Segments.FirstOrDefault() is not IdentifierSegment root || member.Segments.Any(segment =>
            segment is not IdentifierSegment identifier || identifier.Identifier.Contains('.')))
            throw new NotSupportedException("Dynamic Liquid paths are not supported.");
        if (!_locals.Contains(root.Identifier))
            _paths.Add(string.Join('.', member.Segments.Cast<IdentifierSegment>().Select(segment => segment.Identifier)));
        return member;
    }

    protected override Expression VisitFilterExpression(FilterExpression filter)
    {
        if (string.Equals(filter.Name, "raw", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("The raw filter bypasses HTML encoding.");
        return base.VisitFilterExpression(filter);
    }

    public override Expression VisitOtherExpression(Expression expression) =>
        throw new NotSupportedException("Unsupported Liquid expression.");
}
