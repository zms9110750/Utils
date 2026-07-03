#if NET6_0_OR_GREATER
using System.Text.Json;

namespace zms9110750.Utils.Core.Pick;

/// <summary>解析抽取池。内联 k:v 或 file:// 殊途同归。</summary>
public static class PoolParser
{
	/// <summary>解析表达式或文件路径为名称-值字典。</summary>
	public static Dictionary<string, int> Parse(ReadOnlySpan<char> arg)
	{
		var s = arg.Trim();
		if (s.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
		{
			var path = s["file://".Length..].Trim().ToString();
			if (!File.Exists(path))
				throw new FileNotFoundException($"文件不存在 {path}");

			try
			{
				var json = File.ReadAllText(path);
				return JsonSerializer.Deserialize<Dictionary<string, int>>(json)
					?? throw new FormatException("JSON 为空");
			}
			catch (JsonException ex)
			{
				throw new FormatException($"JSON 解析失败: {ex.Message}");
			}
		}

		return ParseInline(s);
	}

	private static Dictionary<string, int> ParseInline(ReadOnlySpan<char> s)
	{
		var result = new Dictionary<string, int>();
		var text = s.ToString().Replace('，', ',');
		var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries);
		foreach (var part in parts)
		{
			var trimmed = part.Trim();
			if (trimmed.Length == 0)
				continue;

			var colonIdx = trimmed.IndexOfAny(':', '：');
			if (colonIdx < 0)
				continue;

			var name = trimmed[..colonIdx].Trim();
			var valStr = trimmed[(colonIdx + 1)..].Trim();
			if (name.Length > 0 && int.TryParse(valStr, out int val))
				result[name] = val;
		}
		return result;
	}
}
#endif
