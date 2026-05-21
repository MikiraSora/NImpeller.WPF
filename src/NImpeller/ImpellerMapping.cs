using System;

namespace NImpeller;

internal partial struct ImpellerMapping
{
    /// <summary>Marshalled native mapping state for unmanaged Impeller memory.</summary>
    public unsafe struct Marshalled : IDisposable
    {
        /// <summary>Release any native mapping state owned by this wrapper.</summary>
        public void Dispose()
        {
            
        }

        /// <summary>Pointer to the native mapping value passed to Impeller.</summary>
        public ImpellerMapping* Value { get; set; }
    }
    
    /// <summary>Marshal unmanaged memory into an Impeller mapping wrapper.</summary>
    /// <param name="contents">Memory provider to expose to Impeller.</param>
    /// <returns>A marshalled mapping wrapper.</returns>
    public static Marshalled Marshal(IImpellerUnmanagedMemory contents)
    {
        throw new NotImplementedException();
    }
}

/// <summary>Marker interface for unmanaged memory that can be exposed to Impeller as a mapping.</summary>
public interface IImpellerUnmanagedMemory
{
    
}
