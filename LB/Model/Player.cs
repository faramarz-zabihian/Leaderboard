using MemoryPack;

[MemoryPackable]
public partial class Player
{
    public string Name = "";
    [MemoryPackIgnore]
    public int Id;
    [MemoryPackIgnore]
    public string SId = "";
    [MemoryPackIgnore]
    public Int32 Score;
}