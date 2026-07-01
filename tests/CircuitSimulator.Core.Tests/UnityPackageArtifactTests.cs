using System.Reflection;
using System.Text.Json;

namespace CircuitSimulator.Core.Tests;

public sealed class UnityPackageArtifactTests
{
    [Fact]
    public void UnityPackageContainsCompatibleManagedDependencies()
    {
        var repositoryRoot = FindRepositoryRoot();
        var packageRoot = Path.Combine(repositoryRoot, "src", "CircuitSimulator.Unity");
        var pluginRoot = Path.Combine(packageRoot, "Runtime", "Plugins");
        var corePath = Path.Combine(pluginRoot, "CircuitSimulator.Core.dll");
        var mathNetPath = Path.Combine(pluginRoot, "MathNet.Numerics.dll");

        Assert.True(File.Exists(corePath), $"Missing packaged Core assembly at {corePath}.");
        Assert.True(File.Exists(mathNetPath), $"Missing packaged Math.NET assembly at {mathNetPath}.");
        Assert.True(File.Exists(Path.Combine(pluginRoot, "CircuitSimulator.Core.xml")));

        var packagedCore = AssemblyName.GetAssemblyName(corePath);
        var runningCore = typeof(Model.Circuit).Assembly.GetName();
        Assert.Equal("CircuitSimulator.Core", packagedCore.Name);
        Assert.Equal(runningCore.Version, packagedCore.Version);

        var packagedAssembly = Assembly.LoadFile(corePath);
        Assert.NotNull(packagedAssembly.GetType("CircuitSimulator.Core.Components.SwitchParameters"));
        Assert.NotNull(packagedAssembly.GetType("CircuitSimulator.Core.Components.LedParameters"));
        Assert.NotNull(
            packagedAssembly
                .GetType("CircuitSimulator.Core.Simulation.RealtimeSimulationSession")
                ?.GetMethod("SetSwitchState"));

        var mathNet = AssemblyName.GetAssemblyName(mathNetPath);
        Assert.Equal("MathNet.Numerics", mathNet.Name);
        Assert.Equal(new Version(5, 0, 0, 0), mathNet.Version);

        using var manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(packageRoot, "package.json")));
        Assert.Equal(
            "com.evgh.circuit-simulator",
            manifest.RootElement.GetProperty("name").GetString());
        Assert.Equal("1.1.0", manifest.RootElement.GetProperty("version").GetString());
        Assert.Equal("6000.0", manifest.RootElement.GetProperty("unity").GetString());
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "CircuitSimulator.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the CircuitSimulator repository root.");
    }
}
