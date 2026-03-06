namespace Impressionist.Implementations
{
    public static class PaletteGenerators
    {
        public static readonly KMeansPaletteGenerator KMeansPaletteGenerator = new KMeansPaletteGenerator();
        public static readonly OctTreePaletteGenerator OctTreePaletteGenerator = new OctTreePaletteGenerator();
    }
}
