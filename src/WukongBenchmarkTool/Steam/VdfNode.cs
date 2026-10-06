namespace WukongBenchmarkTool.Steam;

/// <summary>
/// Узел Valve KeyValues ("VDF") — формата, которым Steam пишет libraryfolders.vdf
/// и appmanifest_*.acf. Один узел — это либо строковое значение, либо набор
/// именованных дочерних узлов (ключи не уникальны, поэтому значение — список).
/// </summary>
public sealed class VdfNode
{
    public string? Value { get; set; }

    public Dictionary<string, List<VdfNode>> Children { get; } = new(StringComparer.OrdinalIgnoreCase);

    public VdfNode? Child(string key) =>
        Children.TryGetValue(key, out var list) && list.Count > 0 ? list[0] : null;

    public IEnumerable<KeyValuePair<string, VdfNode>> ChildEntries() =>
        Children.SelectMany(kv => kv.Value.Select(node => new KeyValuePair<string, VdfNode>(kv.Key, node)));

    public static VdfNode Parse(string text)
    {
        var position = 0;
        return ParseObject(text, ref position);
    }

    private static VdfNode ParseObject(string text, ref int position)
    {
        var node = new VdfNode();
        while (true)
        {
            SkipWhitespace(text, ref position);
            if (position >= text.Length || text[position] == '}')
            {
                if (position < text.Length)
                {
                    position++; // consume '}'
                }

                return node;
            }

            var key = ReadQuotedString(text, ref position);
            SkipWhitespace(text, ref position);

            if (position < text.Length && text[position] == '{')
            {
                position++; // consume '{'
                var child = ParseObject(text, ref position);
                AddChild(node, key, child);
            }
            else
            {
                var value = ReadQuotedString(text, ref position);
                AddChild(node, key, new VdfNode { Value = value });
            }
        }
    }

    private static void AddChild(VdfNode parent, string key, VdfNode child)
    {
        if (!parent.Children.TryGetValue(key, out var list))
        {
            list = new List<VdfNode>();
            parent.Children[key] = list;
        }

        list.Add(child);
    }

    private static void SkipWhitespace(string text, ref int position)
    {
        while (position < text.Length && char.IsWhiteSpace(text[position]))
        {
            position++;
        }
    }

    private static string ReadQuotedString(string text, ref int position)
    {
        SkipWhitespace(text, ref position);
        if (position >= text.Length || text[position] != '"')
        {
            return string.Empty;
        }

        position++; // opening quote
        var start = position;
        var sb = new System.Text.StringBuilder();
        while (position < text.Length && text[position] != '"')
        {
            if (text[position] == '\\' && position + 1 < text.Length)
            {
                sb.Append(text[position + 1]);
                position += 2;
                continue;
            }

            sb.Append(text[position]);
            position++;
        }

        position++; // closing quote
        _ = start;
        return sb.ToString();
    }
}
