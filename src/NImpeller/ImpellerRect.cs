namespace NImpeller;

/// <summary>Rectangle value used by Impeller drawing APIs.</summary>
public partial record struct ImpellerRect
{
    /// <summary>Create an empty rectangle at the origin.</summary>
    public ImpellerRect()
    {
        
    }

    /// <summary>Create a rectangle from origin and size values.</summary>
    /// <param name="x">Left coordinate.</param>
    /// <param name="y">Top coordinate.</param>
    /// <param name="width">Rectangle width.</param>
    /// <param name="height">Rectangle height.</param>
    public ImpellerRect(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}
