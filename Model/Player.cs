using MemoryPack;

[MemoryPackable]
public partial class Player
{
    public int Id;
    public string SId = "";
    public string Name = "";
    [MemoryPackIgnore]
    public Int32 Score;
    [MemoryPackIgnore]
    public bool Fake = false;
}