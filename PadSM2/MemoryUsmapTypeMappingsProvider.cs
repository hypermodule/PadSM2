using CUE4Parse.MappingsProvider.Usmap;

namespace PadSM2;

public class MemoryUsmapTypeMappingsProvider : UsmapTypeMappingsProvider
{
    private readonly byte[] _usmapBytes;
    
    public MemoryUsmapTypeMappingsProvider(byte[] usmapBytes)
    {
        _usmapBytes = usmapBytes;
        Load(_usmapBytes);
    }
    
    public override void Reload()
    {
        Load(_usmapBytes);
    }
}