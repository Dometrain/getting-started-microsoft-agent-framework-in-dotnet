using Chapter03.ConsoleApp;

var demos = new Dictionary<string, Func<Task>>
{
    ["00"] = Demo00FirstTool.RunAsync,
    ["01"] = Demo01MultipleTools.RunAsync,
    ["02"] = Demo02ComplexParameters.RunAsync,
    ["03"] = Demo03ToolConfirmation.RunAsync
};

var demo = args.FirstOrDefault() ?? "00";
if (!demos.TryGetValue(demo, out var run))
{
    Console.WriteLine("Choose demo 00, 01, 02, or 03.");
    return;
}

await run();