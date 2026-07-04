
using zms9110750.Utils.Core;

// 鈹€鈹€鈹€ Trie: 鍓嶇紑鏍?鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
Console.WriteLine("=== Trie ===");
var trie = new Trie();
trie.Add("apple");
trie.Add("application");
Console.WriteLine($"鎼滅储 'app': {string.Join(", ", trie.Search("app"))}");

// 鈹€鈹€鈹€ Pagination: 鍒嗛〉妯″瀷 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
Console.WriteLine("\n=== Pagination ===");
var p = new Pagination(3, 10, 100) { ButtonCount = 5 };
Console.WriteLine($"绗?{p.Page}/{p.TotalPages} 椤碉紝鑼冨洿 {p.DataRange}锛屾寜閽?{p.ButtonRange}");

// 鈹€鈹€鈹€ DeferredActionScope: 寤惰繜閲婃斁 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
Console.WriteLine("\n=== DeferredActionScope ===");
using var scope = new DeferredActionScope();
scope.Add(() => Console.WriteLine("  閲婃斁鍔ㄤ綔"));
Console.WriteLine("  浣滅敤鍩熺粨鏉燂紝鑷姩閲婃斁...");

// 鈹€鈹€鈹€ ProgressStream: 杩涘害娴?鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
Console.WriteLine("\n=== ProgressStream ===");
var data = new byte[] { 10, 20, 30 };
using var ms = new MemoryStream(data);
using var ps = new ProgressStream(ms);
Console.WriteLine($"闀垮害 {ps.Length}锛屽彲璇?{ps.CanRead}");
