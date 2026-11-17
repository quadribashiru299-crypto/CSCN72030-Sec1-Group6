namespace SmartShop.Tests
{
    /// <summary>Writes a throw-away simulation file for a test and deletes it afterwards.</summary>
    internal sealed class TempDataFile : IDisposable
    {
        public string Path { get; }

        public TempDataFile(params string[] lines)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"smartshop-test-{Guid.NewGuid():N}.txt");
            File.WriteAllLines(Path, lines);
        }

        /// <summary>A path that is guaranteed not to exist.</summary>
        public static string MissingPath() =>
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"smartshop-missing-{Guid.NewGuid():N}.txt");

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
