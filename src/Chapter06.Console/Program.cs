using Chapter06.ConsoleApp;

var demos = new Dictionary<string, Func<Task>>
{
    ["00"] = Demos.TracingAsync,
    ["01"] = Demos.TokenMonitoringAsync,
    ["02"] = Demos.PromptInjectionDefenseAsync,
    ["03"] = Demos.MiddlewareAsync
};
var demo = args.FirstOrDefault() ?? "00";
if (demos.TryGetValue(demo, out var run)) await run();
else Console.WriteLine("Choose demo 00, 01, 02, or 03.");