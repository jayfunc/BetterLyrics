# RomajiConverter.Core
用于[RomajiConverter.WinUI](https://github.com/xyh20180101/RomajiConverter.WinUI)项目的罗马音转换相关逻辑的包


## 使用
```C#
//离线分词器
IEnumerable<ConvertedLine> list = RomajiHelper.ToRomaji(jpnStr);

//AI
var list = new ObservableCollection<ConvertedLine>();
await RomajiAIHelper.ToRomajiStreamingAsync(list, jpnStr, new ToRomajiAIOptions
{
    BaseUrl = "",
    Model = "",
    ApiKey = ""
}); //流式插入

//以下是类结构
public class ConvertedLine
{
    public ushort Index { get; set; } = 0;

    public string Chinese { get; set; } = string.Empty;

    public string Japanese { get; set; } = string.Empty;

    public ObservableCollection<ConvertedUnit> Units { get; set; } = new ObservableCollection<ConvertedUnit>();
}

public class ConvertedUnit
{
    public ushort LineIndex { get; set; }

    public string Japanese { get; set; }

    public string Romaji { get; set; }

    public ObservableCollection<ReplaceString> ReplaceRomaji { get; set; }

    public string Hiragana { get; set; }

    public ObservableCollection<ReplaceString> ReplaceHiragana { get; set; }

    public bool IsKanji { get; set; }

    public ushort SelectId { get; set; }
}

public class ReplaceString
{
    public ushort Id { get; set; }

    public string Value { get; set; }

    public bool IsSystem { get; set; }
}
```