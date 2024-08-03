/*
 * StackExchangeDumpConverter
 * Copyright (C) 2024 Maxwell Dreytser
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Reflection;
using CommandLine;
using StackExchangeDumpConverter;
using StackExchangeDumpConverter.Destinations;

var destinationModules = Assembly.GetExecutingAssembly().GetTypes()
    .Where(t => t.GetInterfaces().Contains(typeof(IDumpDestination)))
    .OrderBy(t => t.Name)
    .ToList();

var destinationModuleMap = destinationModules.ToDictionary(t => t.Name.Replace("DumpDestination", "").ToLower());

if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
{
    Console.WriteLine("Usage: [options] <files>...");
    Console.WriteLine("  -h, --help         Show this help output.");
    Console.WriteLine("  -d, --destination  Specify the destination that the dump will be loaded to.");
    Console.WriteLine("                     Must be one of: " + string.Join(',', destinationModuleMap.Keys));
    Console.WriteLine("  <files>            One or more .7z files containing the dump for one site.");
    Console.WriteLine();
    foreach (var module in destinationModules)
    {
        var destinationModule = (IDumpDestination) Activator.CreateInstance(module)!;
        destinationModule.WriteModuleHelp();
        Console.WriteLine();
    }

    return 0;
}

var parser = new CommandLineParser(args, StringComparison.InvariantCulture);

var start = DateTime.Now;

using (var destination =
       (IDumpDestination) Activator.CreateInstance(destinationModuleMap[parser.GetValue("destination", 'd')!])!)
{
    destination.Init(parser);

    destination.WriteSeedData();

    new DumpReader(destination, parser.GetRemainder().ToArray()).ReadDump();
}

Console.WriteLine($"Finished after {(DateTime.Now - start).TotalMinutes} minutes");

return 0;