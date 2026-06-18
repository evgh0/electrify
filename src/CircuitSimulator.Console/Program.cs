using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Mna;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Results;
using CircuitSimulator.Core.Simulation;

var builder = new CircuitBuilder();
var voltageSource = builder.AddVoltageSource("V1", 10.0);
var resistor1 = builder.AddResistor("R1", 1_000.0);
var resistor2 = builder.AddResistor("R2", 2_000.0);

builder.Connect(voltageSource.Negative, resistor2.Negative);
builder.MarkAsGround(voltageSource.Negative);
builder.Connect(voltageSource.Positive, resistor1.Positive);
builder.Connect(resistor1.Negative, resistor2.Positive);

var circuit = builder.Build();
var compiler = new CircuitCompiler();
var compiled = compiler.Compile(circuit);
var result = new DcOperatingPointSolver().Solve(compiled);

PrintNodes(compiled);
PrintTerminalMapping(circuit, compiled);
PrintEdges(compiled);
PrintVariables(compiled);
PrintLinearSystem(result.LinearSystem);
PrintVoltages(result);
PrintCurrents(result, voltageSource, resistor1, resistor2);

static void PrintNodes(CompiledCircuit compiled)
{
    Console.WriteLine("Electrical nodes");
    foreach (var node in compiled.Netlist.Nodes)
    {
        var ground = node.IsGround ? " ground" : string.Empty;
        Console.WriteLine($"  {node.Id}{ground}: {string.Join(", ", node.TerminalIds)}");
    }

    Console.WriteLine();
}

static void PrintTerminalMapping(Circuit circuit, CompiledCircuit compiled)
{
    Console.WriteLine("Terminal to node");
    foreach (var terminal in circuit.Terminals.OrderBy(terminal => terminal.Id.Value))
    {
        var component = circuit.GetComponent(terminal.OwnerComponentId);
        Console.WriteLine($"  {component.Name}.{terminal.Name} {terminal.Id} -> {compiled.Netlist.GetNode(terminal.Id)}");
    }

    Console.WriteLine();
}

static void PrintEdges(CompiledCircuit compiled)
{
    Console.WriteLine("Graph edges");
    foreach (var edge in compiled.Graph.Edges)
    {
        var component = compiled.GetComponent(edge.ComponentId);
        Console.WriteLine($"  {component.Name} {edge.ComponentId}: {string.Join(" -> ", edge.IncidentNodes)}");
    }

    Console.WriteLine();
}

static void PrintVariables(CompiledCircuit compiled)
{
    Console.WriteLine("MNA variables");
    foreach (var variable in compiled.VariableMap.Variables)
    {
        Console.WriteLine($"  {variable.Index.Value}: {variable.Name} ({variable.Kind})");
    }

    Console.WriteLine();
}

static void PrintLinearSystem(MnaLinearSystem system)
{
    Console.WriteLine("A matrix");
    for (var row = 0; row < system.Dimension; row++)
    {
        var entries = Enumerable.Range(0, system.Dimension)
            .Select(column => system.GetMatrixEntry(row, column).ToString("G10"));
        Console.WriteLine($"  [{string.Join(", ", entries)}]");
    }

    Console.WriteLine("z RHS");
    for (var row = 0; row < system.Dimension; row++)
    {
        Console.WriteLine($"  [{row}] {system.GetRightHandSideEntry(row):G10}");
    }

    Console.WriteLine();
}

static void PrintVoltages(DcOperatingPointResult result)
{
    Console.WriteLine("Node voltages");
    foreach (var voltage in result.NodeVoltages)
    {
        Console.WriteLine($"  V({voltage.NodeId}) = {voltage.Voltage:G10} V");
    }

    Console.WriteLine();
}

static void PrintCurrents(
    DcOperatingPointResult result,
    TwoTerminalComponentHandle voltageSource,
    TwoTerminalComponentHandle resistor1,
    TwoTerminalComponentHandle resistor2)
{
    Console.WriteLine("Component currents");
    Console.WriteLine($"  I(V1) = {result.GetComponentCurrent(voltageSource.ComponentId):G10} A");
    Console.WriteLine($"  I(R1) = {result.GetComponentCurrent(resistor1.ComponentId):G10} A");
    Console.WriteLine($"  I(R2) = {result.GetComponentCurrent(resistor2.ComponentId):G10} A");
}
