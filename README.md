## NonGenericCommandHandler

+ Using generic `ICommandHandler`
+ Requires casting in the individual handlers
+ When using `RootBind`, it works, but it gets cumbersome having to specify all the handlers (and this is a very simple case):
  ```csharp
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
  ```

## OnlyGenericCommandHandlerUsingRootBind

+ Receive the following exception:
  ```
  Pure.DI.CannotResolveException: Cannot resolve composition root of type ICommandHandler`1[ICommand].
  ```
  which makes sense. I know one way how to solve it, but I don't like it, see [double dispatch](#onlygenericcommandhandlerusingrootbinddoubledispatch)

## OnlyGenericCommandHandlerUsingRootBindDoubleDispatch
## OnlyGenericCommandHandlerUsingRoots

+ Receive the following exception:
  ```
  Unhandled exception. Pure.DI.CannotResolveException: Cannot resolve composition root  of type  ICommandHandler`1[ImportLocalFilesCommand].
  ```
