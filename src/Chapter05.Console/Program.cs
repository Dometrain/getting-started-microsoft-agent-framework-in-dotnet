using Chapter05.ConsoleApp;

var demos = new Dictionary<string, Func<Task>>
{
    ["00"] = Demos.FirstWorkflowAsync,
    ["01"] = Demos.SequentialAsync,
    ["02"] = Demos.ConditionalAsync,
    ["03"] = Demos.HumanInTheLoopAsync,
    ["04"] = Demos.ConcurrentAsync,
    ["05"] = Demos.SharedStateAndEventsAsync
};
var demo = args.FirstOrDefault() ?? "00";
if (demos.TryGetValue(demo, out var run)) await run();
else Console.WriteLine("Choose demo 00 through 05.");