(function () {
  "use strict";

  const runs = Array.isArray(globalThis.demoRuns) ? globalThis.demoRuns : [];
  const runList = document.getElementById("run-list");
  const runEmpty = document.getElementById("run-empty");
  const runCount = document.getElementById("run-count");
  const runTitle = document.getElementById("run-title");
  const runState = document.getElementById("run-state");
  const rootCount = document.getElementById("root-count");
  const providerCount = document.getElementById("provider-count");
  const warningCount = document.getElementById("warning-count");
  const resultKind = document.getElementById("result-kind");
  const runTemplate = document.getElementById("run-template");
  const rootList = document.getElementById("root-list");
  const providerList = document.getElementById("provider-list");
  const resultView = document.getElementById("run-result");
  const resultHint = document.getElementById("result-hint");
  const runOutput = document.getElementById("run-output");
  const runWarning = document.getElementById("run-warning");
  const runError = document.getElementById("run-error");

  let selectedId = runs.length > 0 ? runs[0].id : null;

  function text(value, fallback) {
    if (value === null || value === undefined) {
      return fallback || "";
    }
    return String(value);
  }

  function values(value) {
    if (Array.isArray(value)) {
      return value;
    }
    return value === null || value === undefined ? [] : [value];
  }

  function setText(element, value, fallback) {
    element.textContent = text(value, fallback);
  }

  function warningText(value) {
    if (value && typeof value === "object") {
      return text(value.code ?? value.Code ?? value.message ?? value.Message, "Unknown warning");
    }
    return text(value);
  }

  function renderTokenList(container, items, emptyText) {
    container.replaceChildren();
    const list = values(items);

    if (list.length === 0) {
      const empty = document.createElement("li");
      empty.className = "list-empty";
      empty.textContent = emptyText;
      container.append(empty);
      return;
    }

    list.forEach(function (item) {
      const token = document.createElement("li");
      token.textContent = text(item);
      container.append(token);
    });
  }

  function renderRunList() {
    runList.replaceChildren();
    setText(runCount, runs.length + " CASES");
    runEmpty.hidden = runs.length > 0;

    runs.forEach(function (run) {
      const button = document.createElement("button");
      button.type = "button";
      button.className = "run-option" + (text(run.id) === text(selectedId) ? " is-selected" : "");
      button.dataset.runId = text(run.id);

      const title = document.createElement("span");
      title.className = "run-option-title";
      title.textContent = text(run.title, "未命名观测");

      const id = document.createElement("span");
      id.className = "run-option-id";
      id.textContent = text(run.id, "unknown");

      button.append(title, id);
      runList.append(button);
    });
  }

  function renderSelectedRun() {
    const run = runs.find(function (item) {
      return text(item.id) === text(selectedId);
    });

    if (!run) {
      setText(runTitle, "尚未选择样例");
      setText(runState, "NO DATA");
      setText(rootCount, "0");
      setText(providerCount, "0");
      setText(warningCount, "0");
      setText(resultKind, "—");
      setText(runTemplate, "等待选择运行样例");
      renderTokenList(rootList, [], "暂无字段路径");
      renderTokenList(providerList, [], "暂无实际调用 Provider");
      resultView.dataset.state = "empty";
      setText(resultHint, "等待数据");
      setText(runOutput, "");
      setText(runWarning, "");
      runWarning.hidden = true;
      setText(runError, "");
      runError.hidden = true;
      return;
    }

    const roots = values(run.roots);
    const providers = values(run.providers);
    const warnings = values(run.warnings ?? run.warning ?? run.Warning);
    const hasError = run.error !== null && run.error !== undefined && text(run.error) !== "";
    const hasOutput = run.output !== null && run.output !== undefined;
    const hasWarnings = warnings.length > 0;

    setText(runTitle, run.title, "未命名观测");
    setText(runState, hasError ? "ERROR" : "OUTPUT");
    runState.className = "run-state " + (hasError ? "is-error" : "is-output");
    setText(rootCount, roots.length);
    setText(providerCount, providers.length);
    setText(warningCount, warnings.length);
    setText(resultKind, hasError ? "error" : (hasWarnings ? "output + warning" : "output"));
    setText(runTemplate, run.template, "（空模板）");
    renderTokenList(rootList, roots, "暂无字段路径");
    renderTokenList(providerList, providers, "暂无实际调用 Provider");

    resultView.dataset.state = hasError ? "error" : "output";
    setText(resultHint, hasError ? "执行失败" : (hasWarnings ? "含 MissingValue warning" : "HTML 编码输出"));
    setText(runOutput, hasError ? "" : (hasOutput ? run.output : "（无输出）"));
    setText(runWarning, hasWarnings ? "Warning: " + warnings.map(warningText).join(" · ") : "");
    runWarning.hidden = !hasWarnings;
    setText(runError, hasError ? run.error : "");
    runOutput.hidden = hasError;
    runError.hidden = !hasError;
  }

  runList.addEventListener("click", function (event) {
    const option = event.target.closest("button[data-run-id]");
    if (!option || !runList.contains(option)) {
      return;
    }
    selectedId = option.dataset.runId;
    renderRunList();
    renderSelectedRun();
  });

  renderRunList();
  renderSelectedRun();
}());
