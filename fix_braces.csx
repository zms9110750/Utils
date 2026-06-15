var path = args[0];
var text = File.ReadAllText(path);

// MinExpr for loop
text = text.Replace(
    "        sb.Append('[');\n        for (int i = 0; i < Items.Length; i++) { if (i > 0) { sb.Append(\", \"); } Items[i].BuildDetail(sb); }\n        sb.Append(']'); return sb;",
    "        sb.Append('[');\n        for (int i = 0; i < Items.Length; i++)\n        {\n            if (i > 0)\n            {\n                sb.Append(\", \");\n            }\n            Items[i].BuildDetail(sb);\n        }\n        sb.Append(']');\n        return sb;");

// MaxExpr for loop
text = text.Replace(
    "        sb.Append('{');\n        for (int i = 0; i < Items.Length; i++) { if (i > 0) { sb.Append(\", \"); } Items[i].BuildDetail(sb); }\n        sb.Append('}'); return sb;",
    "        sb.Append('{');\n        for (int i = 0; i < Items.Length; i++)\n        {\n            if (i > 0)\n            {\n                sb.Append(\", \");\n            }\n            Items[i].BuildDetail(sb);\n        }\n        sb.Append('}');\n        return sb;");

File.WriteAllText(path, text);
Console.WriteLine("Replaced");
