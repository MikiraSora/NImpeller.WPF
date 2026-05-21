namespace NImpeller;

/// <summary>Integer width and height pair used by Impeller native APIs.</summary>
public partial record struct ImpellerISize
{
    /// <summary>Create a zero-size value.</summary>
    public ImpellerISize()
    {
        
    }

    /// <summary>Create an integer size.</summary>
    /// <param name="width">Width component.</param>
    /// <param name="height">Height component.</param>
    public ImpellerISize(int width, int height)
    {
        Width = width;
        Height = height;
    }
}
