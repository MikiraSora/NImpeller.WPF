namespace NImpeller;

/// <summary>RGBA color value used by Impeller drawing APIs.</summary>
public partial record struct ImpellerColor
{
    /// <summary>Create a color from 8-bit alpha, red, green, and blue components.</summary>
    /// <param name="a">Alpha component, from 0 to 255.</param>
    /// <param name="r">Red component, from 0 to 255.</param>
    /// <param name="g">Green component, from 0 to 255.</param>
    /// <param name="b">Blue component, from 0 to 255.</param>
    /// <returns>An Impeller color with normalized floating-point components.</returns>
    public static ImpellerColor FromArgb(int a, int r, int g, int b) =>
        new()
        {
            Alpha = a / 255.0f,
            Red = r / 255.0f,
            Green = g / 255.0f,
            Blue = b / 255.0f
        };
    
    /// <summary>Create an opaque color from 8-bit red, green, and blue components.</summary>
    /// <param name="r">Red component, from 0 to 255.</param>
    /// <param name="g">Green component, from 0 to 255.</param>
    /// <param name="b">Blue component, from 0 to 255.</param>
    /// <returns>An opaque Impeller color with normalized floating-point components.</returns>
    public static ImpellerColor FromRgb(int r, int g, int b) =>
        FromArgb(255, r, g, b);
}
