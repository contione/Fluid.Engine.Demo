using Fluid.Engine.Demo.Providers;

namespace Fluid.Engine.Demo.Examples;

internal sealed record Scenario(string Id, string Title, string Template, string[] Calls, string? Output = null, string? Error = null, string? Warning = null);

internal static class Scenarios
{
    public static Scenario[] All { get; } =
    [
        new("static", "静态富文本 · 零次查询", "<em>Clear skies</em>", [], "<em>Clear skies</em>"),
        new("reuse", "重复引用 · 一次查询", "{{ station.name }} / {{ station.name | upcase }}", [StationProvider.ProviderId], "North Ridge / NORTH RIDGE"),
        new("loop", "仪器清单 · 循环与缺值", "{% for item in instruments %}[{{ item.name | upcase }}:{{ item.id | default: 'N/A' }}]{% endfor %}", [StationJsonProvider.ProviderId], "[LENS:L-07][PRISM:N/A]"),
        new("branch", "关闭的观测站 · 静态依赖", "{% if station.open %}{{ instruments | size }} online{% else %}Offline{% endif %}", [StationProvider.ProviderId, StationJsonProvider.ProviderId], "Offline"),
        new("assign", "局部别名 · HTML 编码", "{% assign note = station.note %}<p>{{ note }}</p>", [StationProvider.ProviderId], "<p>Ice &amp; &lt;fog&gt;</p>"),
        new("fallback", "字段降级 · 缺值警告", "Sky: {{ sky }}", [StationJsonProvider.ProviderId], "Sky: unreported", Warning: "MissingValue"),
        new("product-id", "商品有一条缺 id · 模板 default", "{% for product in products %}[{{ product.id | default: 'N/A' }}]{% endfor %}", [StationJsonProvider.ProviderId], "[OPT-01][N/A]"),
        new("empty", "空白字段 · 引擎置空再由模板兜底", "[{{ label }}] / [{{ label | default: 'N/A' }}]", [StationJsonProvider.ProviderId], "[] / [N/A]", Warning: "MissingValue"),
        new("required", "字段为必需 · 模板 default 不会执行", "{{ requiredProducts | default: 'N/A' }}", [StationJsonProvider.ProviderId], Error: "MissingValue"),
        new("unknown", "未注册的数据源 · 查询前终止", "{{ station.name }} / {{ weather.status }}", [], Error: "UnknownField"),
        new("syntax", "语法错误 · 零次查询", "{% for item in instruments %}", [], Error: "InvalidTemplate")
    ];
}
