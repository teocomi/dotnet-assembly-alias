using System.Diagnostics;
using Mono.Cecil;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Task = Microsoft.Build.Utilities.Task;

namespace Alias;

public class AliasTask :
    Task,
    ICancelableTask
{
    [Required]
    public ITaskItem[] ReferenceCopyLocalPaths{ get; set; } = null!;
    [Required] 
    public string IntermediateAssembly { get; set; } = null!;
    [Required] 
    public string ProjectDirectory { get; set; } = null!;
    [Required]
    public string IntermediateDirectory { get; set; } = null!;
    public string? AssemblyOriginatorKeyFile { get; set; }
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
    [Required]
    public ITaskItem[] AssembliesToAlias { get; set; } = null!;
    public ITaskItem[]? AssembliesToTarget { get; set; }
    public bool SignAssembly { get; set; }
    public bool Internalize { get; set; }
    [Required]
    public ITaskItem[] ReferencePath { get; set; } = null!;
    [Output]
    public ITaskItem[] CopyLocalPathsToRemove { get; set; } = null!;
    [Output]
    public ITaskItem[] CopyLocalPathsToAdd { get; set; } = null!;

    public override bool Execute()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            InnerExecute();
            return true;
        }
        catch (ErrorException exception)
        {
            Log.LogError($"AssemblyAlias: {exception}");
            return false;
        }
        finally
        {
            Log.LogMessageFromText($"Finished AssemblyAlias {stopwatch.ElapsedMilliseconds}ms", MessageImportance.Normal);
        }
    }

    void InnerExecute()
    {
        var assembliesToAlias = AssembliesToAlias.Select(x => x.ItemSpec).ToList();
        List<string> assembliesToTarget;
        if (AssembliesToTarget == null)
        {
            assembliesToTarget = new List<string>();
        }
        else
        {
            assembliesToTarget = AssembliesToTarget.Select(x => x.ItemSpec).ToList();
        }

        var references = ReferencePath.Select(x => x.ItemSpec)
            .Where(x => !assembliesToTarget.Contains(x) && !assembliesToAlias.Contains(x))
            .ToList();
        assembliesToTarget.Insert(0, IntermediateAssembly);
        
        var sourceTargetInfos = new List<SourceTargetInfo>();
        var copyLocalPathsToRemove = new List<ITaskItem>();
        var copyLocalPathsToAdd = new List<ITaskItem>();

        void ProcessCopyLocal(string sourcePath, string targetPath1)
        {
            var toRemove = ReferenceCopyLocalPaths.SingleOrDefault(x => x.ItemSpec == sourcePath);
            if (toRemove != null)
            {
                copyLocalPathsToRemove.Add(toRemove);
            }

            copyLocalPathsToAdd.Add(new TaskItem(targetPath1));
        }

        foreach (var sourcePath in assembliesToAlias)
        {
            var sourceName = Path.GetFileNameWithoutExtension(sourcePath);
            var targetName = $"{Prefix}{sourceName}{Suffix}";
            var targetPath = Path.Combine(IntermediateDirectory, $"{targetName}.dll");
            sourceTargetInfos.Add(new(sourceName, sourcePath, targetName, targetPath, true));
            ProcessCopyLocal(sourcePath, targetPath);
        }

        foreach (var sourcePath in assembliesToTarget)
        {
            var name = Path.GetFileNameWithoutExtension(sourcePath);
            var targetPath = Path.Combine(IntermediateDirectory, $"{name}.dll");
            sourceTargetInfos.Add(new(name, sourcePath, name, targetPath, false));
            ProcessCopyLocal(sourcePath, targetPath);
        }

        var separator = $"{Environment.NewLine}\t";
        var inputs = $@"
Prefix: {Prefix}
Suffix: {Suffix}
Internalize: {Internalize}
AssembliesToAlias:{separator}{string.Join(separator, assembliesToAlias.Select(Path.GetFileNameWithoutExtension))}
AssembliesToTarget:{separator}{string.Join(separator, assembliesToTarget.Select(Path.GetFileNameWithoutExtension))}
TargetInfos:{separator}{string.Join(separator, sourceTargetInfos.Select(x=> $"{x.SourceName} => {x.TargetName}"))}
";
        Log.LogMessageFromText(inputs, MessageImportance.High);

        Aliaser.Run(references, sourceTargetInfos, Internalize, GetKey());
        CopyLocalPathsToRemove = copyLocalPathsToRemove.ToArray();
        CopyLocalPathsToAdd = copyLocalPathsToAdd.ToArray();
    }

    StrongNameKeyPair? GetKey()
    {
        if (!SignAssembly)
        {
            return null;
        }

        if (AssemblyOriginatorKeyFile == null)
        {
            throw new ErrorException("AssemblyOriginatorKeyFile not defined");
        }

        if (!File.Exists(AssemblyOriginatorKeyFile))
        {
            throw new ErrorException($"AssemblyOriginatorKeyFile does no exist:{AssemblyOriginatorKeyFile}");
        }

        var bytes = File.ReadAllBytes(AssemblyOriginatorKeyFile);
        return new(bytes);
    }

    public void Cancel()
    {
    }
}