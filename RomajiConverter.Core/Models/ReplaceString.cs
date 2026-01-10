namespace RomajiConverter.Core.Models
{
    public class ReplaceString
    {
        public ReplaceString(ushort id, string value, bool isSystem)
        {
            Id = id;
            Value = value;
            IsSystem = isSystem;
        }

        public ushort Id { get; set; }

        public string Value { get; set; }

        public bool IsSystem { get; set; }

        public override string ToString()
        {
            return Value;
        }
    }
}