using System.Text;
using System.Text.Json;
using Fluid.Engine.Demo.Contracts;
using Fluid.Engine.Demo.Examples;

Console.OutputEncoding = Encoding.UTF8;
if (args is not ([] or ["--export", _]))
{
    Console.Error.WriteLine("Usage: dotnet run [-- --export docs/runs.js]");
    return 2;
}

using var observatory = new Observatory();
var runs = new List<object>();
var passed = 0;
foreach (var example in Scenarios.All)
{
    var request = new RenderRequest { Context = { ["stationId"] = "north-ridge" }, Items = [new() { ItemId = example.Id, TemplateContent = example.Template }] };
    var fields = observatory.Service.Parse(example.Template).Variables;
    var (response, calls) = await observatory.RenderAsync(request);
    var result = response.Results.Single();
    var error = result.Errors.FirstOrDefault()?.Code.ToString();
    var warning = result.Warnings.FirstOrDefault()?.Code.ToString();
    var matches = result.Html == example.Output && error == example.Error && warning == example.Warning && calls.Order().SequenceEqual(example.Calls.Order());
    if (matches) passed++;
    Console.WriteLine($"[{(matches ? "PASS" : "FAIL")}] {example.Title}\n  模板: {example.Template}\n  依赖: {Names(fields)}\n  查询: {Names(calls)}\n  {(error is null ? "输出" : "错误")}: {error ?? result.Html}\n");
    if (warning is not null) Console.WriteLine($"  警告: {warning}\n");
    runs.Add(new { example.Id, example.Title, example.Template, Roots = fields, Providers = calls, Output = result.Html, Error = error, Warning = warning });
}
Console.WriteLine($"{passed}/{Scenarios.All.Length} 个示例符合预期。");
if (passed != Scenarios.All.Length) return 1;
if (args is ["--export", var file])
{
    var json = JsonSerializer.Serialize(runs, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
    await File.WriteAllTextAsync(file, $"globalThis.demoRuns = {json};\n", new UTF8Encoding(false));
}
return 0;

static string Names(IReadOnlyList<string> names) => names.Count == 0 ? "—" : string.Join(", ", names);
