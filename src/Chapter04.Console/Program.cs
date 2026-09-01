using Chapter04.ConsoleApp;

var demos = new Dictionary<string, Func<Task>>
{
    ["00"] = Demos.BasicsAsync,
    ["01"] = Demos.ValidationAsync,
    ["02"] = Demos.TemplatesAsync,
    ["03"] = Demos.ToolsAndStructuredOutputAsync
};
var demo = args.FirstOrDefault() ?? "00";
if (demos.TryGetValue(demo, out var run)) await run();
else Console.WriteLine("Choose demo 00, 01, 02, or 03.");