using System.Diagnostics;

namespace CirclesLand.PathfinderExport.Updater;

public class Logger
{
    private int _indention = 0;

    public void Call(string description, Action action)
    {
        var indention = Indention();

        _indention++;

        var sw = new Stopwatch();
        Console.WriteLine($"{indention}+ {description}:");

        sw.Start();
        action();
        sw.Stop();

        _indention--;

        Console.WriteLine($"{indention}- {description} took {sw.Elapsed}");
    }

    public async Task Call(string description, Func<Task?> action)
    {
        var indention = Indention();

        _indention++;

        var sw = new Stopwatch();
        Console.WriteLine($"{indention}+ {description}:");

        sw.Start();
        var promise = action();
        if (promise != null)
        {
            await promise;
        }
        sw.Stop();

        _indention--;

        Console.WriteLine($"{indention}- took {sw.Elapsed}");
    }

    private string Indention()
    {
        var indention = "";
        for (int i = 0; i < _indention; i++)
            indention += "  ";
        return indention;
    }

    public void Log(string text)
    {
        var indention = Indention();
        Console.WriteLine($"{indention}* {text}");
    }
}