namespace Lab09;

internal sealed class GraphSettings
{
    public double A { get; init; } = 2;
    public double B { get; init; } = 1;
    public double TMin { get; init; }
    public double TMax { get; init; } = Math.PI * 2;
    public double Step { get; init; } = 0.01;
    public bool FillArea { get; init; } = true;
    public bool ShowPoints { get; init; }
}
