using Emf2Svg;

string? inputPath = null;
string? outputPath = null;
bool listRecords = false;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "-i" when i + 1 < args.Length:
            inputPath = args[++i];
            break;
        case "-o" when i + 1 < args.Length:
            outputPath = args[++i];
            break;
        case "--list-records":
            listRecords = true;
            break;
    }
}

if (inputPath == null)
{
    Console.Error.WriteLine("Usage: emf2svg -i <input.emf> -o <output.svg>");
    Console.Error.WriteLine("       emf2svg --list-records -i <input.emf>");
    return 1;
}

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Error: input file not found: {inputPath}");
    return 1;
}

if (listRecords)
{
    EmfProcessor.ListRecords(inputPath);
    return 0;
}

if (outputPath == null)
{
    Console.Error.WriteLine("Error: output path (-o) required");
    return 1;
}

EmfProcessor.Process(inputPath, outputPath);
Console.Error.WriteLine($"Written: {outputPath}");
return 0;
