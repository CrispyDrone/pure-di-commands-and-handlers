// See https://aka.ms/new-console-template for more information
using Pure.DI;

var container = new Composition();
var dispatcher = container.Dispatcher;

args = ["Folder 1"];

var command = ParseArgs();
await dispatcher.Execute(command);

args = ["sftp://user@host.com/my-folder"];

command = ParseArgs();

await dispatcher.Execute(command);

ICommand ParseArgs()
{
    var location = args[0];
    if (location.StartsWith("sftp")) return new ImportRemoteFilesCommand(location);
    else return new ImportLocalFilesCommand(location);
}


public interface ICommand { }
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task Execute(TCommand command);
}

public interface IDispatcher
{
    Task Execute<TCommand>(TCommand command) where TCommand : ICommand;
}

sealed class CommandDispatcher : IDispatcher
{
    private readonly Composition _container;

    public CommandDispatcher(Composition container)
    {
        _container = container;
    }

    public Task Execute<TCommand>(TCommand command) where TCommand : ICommand
    {
        var handler = _container.Resolve<ICommandHandler<TCommand>>();
        return handler.Execute(command);
    }
}

public interface ILocalFileSystem
{
    FileInfo GetFile(string path);
    IEnumerable<FileInfo> GetFiles(DirectoryInfo directory);
}

public class LocalFileSystem : ILocalFileSystem
{
    public FileInfo GetFile(string path)
    {
        return new FileInfo(path);
    }

    public IEnumerable<FileInfo> GetFiles(DirectoryInfo directory)
    {
        yield return new FileInfo("A");
        yield return new FileInfo("B");
    }
}


public record ImportLocalFilesCommand
(
    string ImportDirectory
) : ICommand;

public sealed class ImportLocalFilesCommandHandler : ICommandHandler<ImportLocalFilesCommand>
{
    private readonly ILocalFileSystem _localFileSystem;

    public ImportLocalFilesCommandHandler(ILocalFileSystem localFileSystem)
    {
        _localFileSystem = localFileSystem;
    }

    public Task Execute(ImportLocalFilesCommand command)
    {
        Console.WriteLine("Importing files from the local file system.");
        var files = _localFileSystem.GetFiles(new DirectoryInfo(command.ImportDirectory));
        foreach (var file in files)
        {
            Console.WriteLine($"Reading {file}");
        }
        return Task.CompletedTask;
    }

    public Task Execute(ICommand command)
    {
        return Execute((ImportLocalFilesCommand)command);
    }
}

public interface IRemoteClient
{
    public IEnumerable<string> GetDownloadUrls(string folder);
}

public class RemoteClient : IRemoteClient
{
    public IEnumerable<string> GetDownloadUrls(string folder)
    {
        yield return "url:A";
        yield return "url:B";
    }
}

public record ImportRemoteFilesCommand
(
    string Url
) : ICommand;

public sealed class ImportRemoteFilesCommandHandler : ICommandHandler<ImportRemoteFilesCommand>
{
    private readonly IRemoteClient _remoteClient;

    public ImportRemoteFilesCommandHandler(IRemoteClient remoteClient)
    {
        _remoteClient = remoteClient;
    }

    public Task Execute(ImportRemoteFilesCommand command)
    {
        Console.WriteLine("Importing files from a remote location.");
        var urls = _remoteClient.GetDownloadUrls(command.Url);
        foreach (var url in urls)
        {
            Console.WriteLine($"Downloading {url}");
        }
        return Task.CompletedTask;
    }

    public Task Execute(ICommand command)
    {
        return Execute((ImportRemoteFilesCommand)command);
    }
}

sealed partial class Composition
{
    void Setup() =>
        DI.Setup(nameof(Composition))
        .RootBind<ICommandHandler<ImportRemoteFilesCommand>>().To<ImportRemoteFilesCommandHandler>()
        .RootBind<ICommandHandler<ImportLocalFilesCommand>>().To<ImportLocalFilesCommandHandler>()
        .Bind().To<RemoteClient>()
        .Bind().To<LocalFileSystem>()
        .Root<CommandDispatcher>("Dispatcher");
}
