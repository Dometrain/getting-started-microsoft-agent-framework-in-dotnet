using Chapter02.ConsoleApp;

var demos = new Dictionary<string, Func<Task>>
{
    ["00"] = Demos.FirstAgentAsync,
    ["01"] = Demos.FoundryAgentAsync,
    ["02"] = Demos.LocalAgentAsync,
    ["04"] = Demos.MultiTurnAsync,
    ["05"] = Demos.PersistingSessionsAsync,
    ["06"] = Demos.CompactionAsync
};
var demo = args.FirstOrDefault() ?? "00";
if (demos.TryGetValue(demo, out var run)) await run();
else Console.WriteLine("Choose demo 00, 01, 02, 04, 05, or 06.");