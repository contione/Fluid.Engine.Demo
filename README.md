# Fluid.Engine.Demo

独立的 .NET 10 控制台示例。以脱敏的观测站和仪器数据，演示真实渲染链路：**Liquid 模板 → 完整字段路径 → 字段注册表 → Provider 批量取值 → 字段策略 → HTML 输出**。示例使用实际的 `RenderService`、字段定义、Provider 路由和模板缓存，运行结果可直接导出为网页快照。

[渲染流程与在线案例](https://contione.github.io/Fluid.Engine.Demo/) · [RenderEngineOptions 配置手册](https://contione.github.io/Fluid.Engine.Demo/options.html)

## 运行

执行：

~~~powershell
dotnet run
~~~

入口运行全部 11 个场景，检查结果、告警、错误以及每个 Provider 的调用次数。预期被拒绝的模板也应显示 PASS。

## 文件职责

| 路径 | 负责什么 |
| --- | --- |
| `Fluid.Engine.Demo.csproj` | 控制台项目、目标框架及 Fluid 版本。 |
| `Program.cs` | 运行每个场景、比对结果、打印轨迹，并可导出网页快照。 |
| `Providers/*.cs` | 脱敏 Provider 实现及其客户端、JSON 读取和响应模型；包含 `StationProvider`、`StationJsonProvider`、`StationApiClient` 等 12 个文件。 |
| `Rendering/*.cs` | 负责模板解析、完整字段路径解析、注册表查找、按实际 Provider ID 合并批量 `ResolveAsync`、字段策略和模板/计划缓存，再完成渲染。 |
| `Contracts/*.cs` | 定义渲染请求、模板项、解析结果、告警及批次响应。 |
| `appsettings.json` | 实际加载的 RenderEngineOptions。修改字段映射、缺值策略或缓存配置后重新运行。 |
| `Configuration/*.cs` | 定义引擎与观测站客户端配置选项。 |
| `docs/options.html`、`docs/options.css` | 中文配置速查与三种兜底结果对比。 |
| `Fields/*.cs` | 定义字段元数据和注册表；把字段路径的根映射到 Provider，并保留成员路径供取值。 |
| `Examples/Observatory.cs` | 组装并记录观测站示例使用的真实 Provider，以及它们返回的脱敏数据。 |
| `Examples/Scenarios.cs` | 定义输入模板、预期 Provider 调用、输出和诊断码。 |
| `docs/index.html`、`site.css`、`site.js` | 静态结果页的结构、外观和案例切换；可直接打开，无需前端构建。 |
| `docs/runs.js` | 控制台成功运行后导出的案例快照，保留 `template`、`roots`、`providers`、`output`、`warning`、`error` 六类运行数据。页面只展示这些真实结果。 |
| `docs/.nojekyll` | 发布时直接提供静态文件。 |
| `.gitignore` | 排除本地构建输出和编辑器文件。 |

## 一次渲染怎样完成

1. **语法与字段路径解析**：`RenderService` 调用 Fluid 解析模板，同时收集完整字段路径。语法错误时不查询 Provider。
2. **注册表映射**：字段注册表按完整路径（或模板项的 Attributes 映射）取得字段定义，开放分组可推断未列出的成员。未知字段会使该模板项失败，不影响同批的其他有效项。
3. **按 Provider 批量取值**：同一批请求中的字段按实际 Provider ID 合并，每个 Provider 只调用一次 `ResolveAsync`；Provider 返回按字段路径对应的值。
4. **策略与缓存**：`RenderService` 按配置使用模板/计划缓存，并对缺失值、上下文无效和 Provider 错误执行字段策略；例如 `sky` 缺值时降级为 `unreported`，同时保留 `MissingValue` warning；一次批量的字段值不会泄漏到下一批。
5. **Liquid 渲染**：用本轮解析结果创建独立上下文，循环、条件、过滤器和成员查找交给 Fluid；输出使用 `HtmlEncoder.Default` 编码插值，不清洗模板中的 HTML。

**观察一个结果**：仪器清单中有一项缺少 id。模板用 `{{ item.id | default: 'N/A' }}`，最终输出 `[LENS:L-07][PRISM:N/A]`；本批的 `instruments` 字段路径归并到对应 Provider，只解析一次。

额外对照三种兜底：数组中的单个商品缺少 id，由模板的 default 显示 N/A；label 是纯空白，字段策略先置空，再由模板显示 N/A；requiredProducts 缺失且配置 Error 时整项失败，不执行过滤器。

## 查看及发布静态页

- **本地**：双击 `docs/index.html`，切换案例查看每一次运行。
- **同步示例**：修改 C# 样例后执行 `dotnet run -c Release -- --export docs/runs.js`，再刷新网页。只有自检全部通过才会覆盖快照。
- **GitHub Pages**：保留 `docs/` 中的静态文件，使用 Pages 的分支部署方式发布。网站使用相对资源路径，不依赖域名或站点根路径。
