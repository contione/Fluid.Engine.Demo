globalThis.demoRuns = [
  {
    "id": "static",
    "title": "\u9759\u6001\u5BCC\u6587\u672C \u00B7 \u96F6\u6B21\u67E5\u8BE2",
    "template": "\u003Cem\u003EClear skies\u003C/em\u003E",
    "roots": [],
    "providers": [],
    "output": "\u003Cem\u003EClear skies\u003C/em\u003E",
    "error": null,
    "warning": null
  },
  {
    "id": "reuse",
    "title": "\u91CD\u590D\u5F15\u7528 \u00B7 \u4E00\u6B21\u67E5\u8BE2",
    "template": "{{ station.name }} / {{ station.name | upcase }}",
    "roots": [
      "station.name"
    ],
    "providers": [
      "stationProvider"
    ],
    "output": "North Ridge / NORTH RIDGE",
    "error": null,
    "warning": null
  },
  {
    "id": "loop",
    "title": "\u4EEA\u5668\u6E05\u5355 \u00B7 \u5FAA\u73AF\u4E0E\u7F3A\u503C",
    "template": "{% for item in instruments %}[{{ item.name | upcase }}:{{ item.id | default: \u0027N/A\u0027 }}]{% endfor %}",
    "roots": [
      "instruments"
    ],
    "providers": [
      "station-http"
    ],
    "output": "[LENS:L-07][PRISM:N/A]",
    "error": null,
    "warning": null
  },
  {
    "id": "branch",
    "title": "\u5173\u95ED\u7684\u89C2\u6D4B\u7AD9 \u00B7 \u9759\u6001\u4F9D\u8D56",
    "template": "{% if station.open %}{{ instruments | size }} online{% else %}Offline{% endif %}",
    "roots": [
      "station.open",
      "instruments"
    ],
    "providers": [
      "stationProvider",
      "station-http"
    ],
    "output": "Offline",
    "error": null,
    "warning": null
  },
  {
    "id": "assign",
    "title": "\u5C40\u90E8\u522B\u540D \u00B7 HTML \u7F16\u7801",
    "template": "{% assign note = station.note %}\u003Cp\u003E{{ note }}\u003C/p\u003E",
    "roots": [
      "station.note"
    ],
    "providers": [
      "stationProvider"
    ],
    "output": "\u003Cp\u003EIce \u0026amp; \u0026lt;fog\u0026gt;\u003C/p\u003E",
    "error": null,
    "warning": null
  },
  {
    "id": "fallback",
    "title": "\u5B57\u6BB5\u964D\u7EA7 \u00B7 \u7F3A\u503C\u8B66\u544A",
    "template": "Sky: {{ sky }}",
    "roots": [
      "sky"
    ],
    "providers": [
      "station-http"
    ],
    "output": "Sky: unreported",
    "error": null,
    "warning": "MissingValue"
  },
  {
    "id": "product-id",
    "title": "\u5546\u54C1\u6709\u4E00\u6761\u7F3A id \u00B7 \u6A21\u677F default",
    "template": "{% for product in products %}[{{ product.id | default: \u0027N/A\u0027 }}]{% endfor %}",
    "roots": [
      "products"
    ],
    "providers": [
      "station-http"
    ],
    "output": "[OPT-01][N/A]",
    "error": null,
    "warning": null
  },
  {
    "id": "empty",
    "title": "\u7A7A\u767D\u5B57\u6BB5 \u00B7 \u5F15\u64CE\u7F6E\u7A7A\u518D\u7531\u6A21\u677F\u515C\u5E95",
    "template": "[{{ label }}] / [{{ label | default: \u0027N/A\u0027 }}]",
    "roots": [
      "label"
    ],
    "providers": [
      "station-http"
    ],
    "output": "[] / [N/A]",
    "error": null,
    "warning": "MissingValue"
  },
  {
    "id": "required",
    "title": "\u5B57\u6BB5\u4E3A\u5FC5\u9700 \u00B7 \u6A21\u677F default \u4E0D\u4F1A\u6267\u884C",
    "template": "{{ requiredProducts | default: \u0027N/A\u0027 }}",
    "roots": [
      "requiredProducts"
    ],
    "providers": [
      "station-http"
    ],
    "output": null,
    "error": "MissingValue",
    "warning": null
  },
  {
    "id": "unknown",
    "title": "\u672A\u6CE8\u518C\u7684\u6570\u636E\u6E90 \u00B7 \u67E5\u8BE2\u524D\u7EC8\u6B62",
    "template": "{{ station.name }} / {{ weather.status }}",
    "roots": [
      "station.name",
      "weather.status"
    ],
    "providers": [],
    "output": null,
    "error": "UnknownField",
    "warning": null
  },
  {
    "id": "syntax",
    "title": "\u8BED\u6CD5\u9519\u8BEF \u00B7 \u96F6\u6B21\u67E5\u8BE2",
    "template": "{% for item in instruments %}",
    "roots": [],
    "providers": [],
    "output": null,
    "error": "InvalidTemplate",
    "warning": null
  }
];
