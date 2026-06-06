using LiteDB;

namespace MetBench_Domain
{
    //Domain
    public class Domain 
    {
        [BsonId]
        public int IdDomain { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // 同 Application.ToString — WPF ComboBox 默认显示 Name 而非类名 (UAT round-1)
        public override string ToString() => Name ?? string.Empty;
    }
}
