// See https://aka.ms/new-console-template for more information
using Pure.DI;
using System.Collections.Concurrent;

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
public interface ICommandHandler<in TCommand> : ICommandHandler where TCommand : ICommand
{
    Task Execute(TCommand command);
}
public interface ICommandHandler
{
    Task Execute(ICommand command);
    bool CanExecute(ICommand command);
}

public interface IDispatcher
{
    Task Execute(ICommand command);
}

sealed class CommandDispatcher : IDispatcher
{
    // private readonly Func<ICommandHandler<ImportLocalFilesCommand>> _localHandler;
    // private readonly Func<ICommandHandler<ImportRemoteFilesCommand>> _remoteHandler;

    // private readonly IEnumerable<Func<ICommandHandler>> _commandHandlers;

    // public CommandDispatcher(IEnumerable<Func<ICommandHandler>> commandHandlers)
    // {
    //     _commandHandlers = commandHandlers;
    // }
    // public CommandDispatcher(
    //     Func<ICommandHandler<ImportLocalFilesCommand>> localHandler,
    //     Func<ICommandHandler<ImportRemoteFilesCommand>> remoteHandler
    // )
    // {
    //     _localHandler = localHandler;
    //     _remoteHandler = remoteHandler;
    // }

    private readonly Composition _container;

    public CommandDispatcher(Composition container)
    {
        _container = container;
    }

    public Task Execute(ICommand command)
    {
        // var handler = _container.Resolve(typeof(ICommandHandler<>).MakeGenericType(command.GetType()));
        // var handler = _container.Resolve<ICommandHandler>(command.GetType());
        var icommandhandlerInterface = TypeCache.GetInterfaceForCommand(command.GetType());
        var impl = TypeCache.GetImplementor(icommandhandlerInterface);
        var handler = Activator.CreateInstance(impl) as ICommandHandler;
        _container.BuildUp(handler);
        return handler.Execute(command);

        // return ((ICommandHandler)handler).Execute(command);
    }

    // public Task Execute(ICommand command)
    // {
    //     return command switch
    //     {
    //         ImportLocalFilesCommand local => _localHandler().Execute(local),
    //         ImportRemoteFilesCommand remote => _remoteHandler().Execute(remote),
    //         _ => throw new NotSupportedException()
    //     };
    // }
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
    private ILocalFileSystem _localFileSystem;
    // public ImportLocalFilesCommandHandler(ILocalFileSystem localFileSystem)
    // {
    //     _localFileSystem = localFileSystem;
    // }

    // public required ILocalFileSystem LocalFileSystem { get; init; }
    [Dependency]
    public void Initialize(ILocalFileSystem fileSystem)
    {
        _localFileSystem = fileSystem;
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

    public bool CanExecute(ICommand command)
    {
        return command is ImportLocalFilesCommand;
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
    // private readonly IRemoteClient _remoteClient;
    private IRemoteClient _remoteClient;
    public ImportRemoteFilesCommandHandler() { }

    // public ImportRemoteFilesCommandHandler(IRemoteClient remoteClient)
    // {
    //     _remoteClient = remoteClient;
    // }

    // public required IRemoteClient RemoteClient { get; init; }

    [Dependency]
    public void Initialize(IRemoteClient remoteClient)
    {
        _remoteClient = remoteClient;
    }

    public bool CanExecute(ICommand command)
    {
        return command is ImportRemoteFilesCommand;
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

sealed class TypeCache
{
    private static readonly ConcurrentDictionary<Type, Type> s_cache = new();

    public static Type? GetInterfaceForCommand(Type type)
    {
        var handlerInterface = typeof(ICommandHandler<>);

        return handlerInterface.MakeGenericType(type);
    }

    public static Type? GetImplementor(Type type)
    {
        if (s_cache.TryGetValue(type, out Type? value))
        {
            return value;
        }

        var commandType = typeof(ICommand);
        var handlerInterface = typeof(ICommandHandler<>);

        var consumer = type;
        var consumerGenericArgs = type.GetGenericArguments();
        if (consumerGenericArgs.Length != 1) return null;

        var consumerCommand = consumerGenericArgs.Single();
        if (!consumerCommand.GetInterfaces().Any(i => i == commandType)) return null;

        var constructedInterfaceType = handlerInterface.MakeGenericType(consumerCommand);

        var definedClasses = handlerInterface
            .Assembly
            .GetTypes()
            .Where(t => t.IsClass && t.IsTypeDefinition)
            .ToList();

        foreach (var c in definedClasses)
        {
            var interfaces = c.GetInterfaces();
            if (interfaces.Length == 0) continue;

            var commandHandlerInterface = interfaces.SingleOrDefault(i => i == constructedInterfaceType);

            if (commandHandlerInterface != null)
            {
                s_cache.GetOrAdd(type, c);
                return c;
            }
        }

        return null;
    }
}

sealed partial class Composition
{
    // ICommandHandler Get(Type type)
    // {
    //     return (ICommandHandler)this.Resolve(type);
    // }

    void Setup() =>
        DI.Setup(nameof(Composition))
        /* Doesn't work because when using RootBind ConsumerType is Composition
         * Doesn't work because when using normal bind you can't resolve the handlers since only roots can be resolved
         * Might be able to use Tags and the Builder pattern?? CommandHandlers will need to have a default constructor
         */
        // .Roots<ICommandHandler>()
        // .RootBind<ICommandHandler>().To(ctx =>
        // {
        //     // var consumerType = ctx.ConsumerType;
        //     // var commandType = TypeInfo.GetType(ctx.Tag.ToString());
        //     // bug? "ctx.Tag as Type" -> global::Pure.DI.Tag.Anyas Type
        //     // var implementor = TypeCache.GetImplementorForCommand(ctx.Tag as Type);
        //     var implementor = TypeCache.GetImplementorForCommand(ctx.Tag);
        //     // return Get(implementor);
        //     var impl = Activator.CreateInstance(implementor);
        //     ctx.BuildUp(impl);
        //     return impl as ICommandHandler;
        // })
        // .Bind<ICommandHandler>(Tag.Any).To(ctx =>
        // {
        //     // var consumerType = ctx.ConsumerType;
        //     // var consumerType = TypeInfo.GetType(ctx.Tag.ToString());
        //     // var implementor = TypeCache.GetImplementor(consumerType);
        //     // return Get(implementor);
        //     var implementor = TypeCache.GetImplementorForCommand((Type)ctx.Tag);
        //     // return Get(implementor);
        //     var impl = Activator.CreateInstance(implementor);
        //     ctx.BuildUp(impl);
        //     return impl as ICommandHandler;
        // })
        // .Bind().To<ImportLocalFilesCommandHandler>()
        // .Bind().To<ImportRemoteFilesCommandHandler>()
        .Bind().To<RemoteClient>()
        .Bind().To<LocalFileSystem>()
        .Builders<ICommandHandler>()
        .Root<CommandDispatcher>("Dispatcher");
}
