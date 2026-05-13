namespace MetBench_BLL.SystemMT.ParameterMapping;

/// <summary>
/// JSON Pointer 风格路径解析器（RFC 6901 简化版）。
/// </summary>
/// <remarks>
/// 接受两种路径风格：
/// <list type="bullet">
///   <item>斜杠分隔："/materials/fuel/temperature_kelvin" 或 "materials/fuel/temperature_kelvin"</item>
///   <item>点分隔："materials.fuel.temperature_kelvin"</item>
/// </list>
///
/// 数组下标用方括号："materials.fuel.sigma_t[0]" 或 "materials/fuel/sigma_t/0"。
///
/// 不支持 RFC 6901 的 ~0 / ~1 转义；这是 SUT 输入数据的现实约定，
/// 字段名里不会含 / 和 ~。
/// </remarks>
public sealed class JsonPointerResolver : IFieldPathResolver
{
    public string PathSyntax => "json-pointer";

    public object? Get(IReadOnlyDictionary<string, object?> data, string path)
    {
        var segments = SplitPath(path);
        object? current = data;
        foreach (var seg in segments)
        {
            current = Step(current, seg, path);
        }
        return current;
    }

    public bool Exists(IReadOnlyDictionary<string, object?> data, string path)
    {
        try
        {
            Get(data, path);
            return true;
        }
        catch (FieldPathNotFoundException)
        {
            return false;
        }
    }

    public Dictionary<string, object?> Set(
        IReadOnlyDictionary<string, object?> data,
        string path,
        object? value)
    {
        var segments = SplitPath(path);
        var result = new Dictionary<string, object?>(data);
        SetRecursive(result, segments, 0, value, path);
        return result;
    }

    // ============= helpers =============

    private static string[] SplitPath(string path)
    {
        // 去掉前导斜杠，统一以 "/" 或 "." 分隔；展开 "[n]" 为单独段
        var normalized = path.TrimStart('/');
        if (normalized.Length == 0)
            return Array.Empty<string>();

        // 把 "[n]" 替换为 ".n" 然后按 . 和 / 拆分
        var expanded = System.Text.RegularExpressions.Regex.Replace(normalized,
            @"\[(\d+)\]", ".$1");
        return expanded.Split(new[] { '/', '.' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private static object? Step(object? current, string segment, string fullPath)
    {
        if (current is null)
            throw new FieldPathNotFoundException(fullPath);

        if (current is IReadOnlyDictionary<string, object?> dict)
        {
            if (!dict.TryGetValue(segment, out var next))
                throw new FieldPathNotFoundException(fullPath);
            return next;
        }
        if (current is IDictionary<string, object?> mutableDict)
        {
            if (!mutableDict.TryGetValue(segment, out var next))
                throw new FieldPathNotFoundException(fullPath);
            return next;
        }
        if (current is System.Collections.IList list && int.TryParse(segment, out var idx))
        {
            if (idx < 0 || idx >= list.Count)
                throw new FieldPathNotFoundException(fullPath);
            return list[idx];
        }
        throw new FieldPathNotFoundException(fullPath);
    }

    private static void SetRecursive(
        IDictionary<string, object?> current,
        string[] segments,
        int index,
        object? value,
        string fullPath)
    {
        var seg = segments[index];
        var isLeaf = index == segments.Length - 1;

        if (isLeaf)
        {
            current[seg] = value;
            return;
        }

        // 中间段 — 需要进入子 dict / 子 list
        if (!current.TryGetValue(seg, out var next))
            throw new FieldPathNotFoundException(fullPath);

        if (next is IDictionary<string, object?> subDict)
        {
            // copy-on-write
            var newSub = new Dictionary<string, object?>(subDict);
            current[seg] = newSub;
            SetRecursive(newSub, segments, index + 1, value, fullPath);
            return;
        }
        if (next is IReadOnlyDictionary<string, object?> roSubDict)
        {
            var newSub = new Dictionary<string, object?>(roSubDict);
            current[seg] = newSub;
            SetRecursive(newSub, segments, index + 1, value, fullPath);
            return;
        }
        if (next is System.Collections.IList list && int.TryParse(segments[index + 1], out var idx))
        {
            if (idx < 0 || idx >= list.Count)
                throw new FieldPathNotFoundException(fullPath);
            if (index + 1 == segments.Length - 1)
            {
                list[idx] = value;
                return;
            }
            throw new NotSupportedException(
                $"Setting nested path through array index not supported at: '{fullPath}'");
        }
        throw new FieldPathNotFoundException(fullPath);
    }
}
