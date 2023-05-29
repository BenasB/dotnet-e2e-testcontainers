using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;

namespace EndToEndTests;

public class AppFixture : IAsyncLifetime
{
    // This network will contain the Calculator and History containers
    private readonly INetwork _network;

    // Creating an image from a Dockerfile can be skipped if you are using an already pre built image
    private readonly IFutureDockerImage _calculatorImage;
    private readonly IFutureDockerImage _historyImage;

    private readonly IContainer _calculatorContainer;
    private readonly IContainer _historyContainer;
    
    // Clients are used by tests to input data and assert results
    private HttpClient? _calculatorClient;
    private HttpClient? _historyClient;
    
    public HttpClient CalculatorClient => _calculatorClient ?? throw new InvalidOperationException("Calculator client was not initialized");
    public HttpClient HistoryClient => _historyClient ?? throw new InvalidOperationException("History client was not initialized");

    public AppFixture()
    {
        _network = new NetworkBuilder()
          .Build();

        _historyImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), "History")
            // Used to clean up multi-stage intermediate layers
            .WithBuildArgument("RESOURCE_REAPER_SESSION_ID", ResourceReaper.DefaultSessionId.ToString("D"))
            .Build();

        _calculatorImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), "Calculator")
            .WithBuildArgument("RESOURCE_REAPER_SESSION_ID", ResourceReaper.DefaultSessionId.ToString("D"))
            .Build();

        _historyContainer = new ContainerBuilder()
            .WithImage(_historyImage)
            // Port binding required to allow connection from outside the docker network
            // e.g. our test process needs to access this
            .WithPortBinding(80, true)
            .WithNetwork(_network)
            .WithNetworkAliases(nameof(_historyContainer))
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80))
            .Build();

        _calculatorContainer = new ContainerBuilder()
            .WithImage(_calculatorImage)
            .WithPortBinding(80, true)
            .WithNetwork(_network)
            .WithNetworkAliases(nameof(_calculatorContainer))
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80))
            // Let Calculator know how to call History by using the docker network
            // The history container is not started at this time but we know the hostname
            // beforehand due to setting it with WithNetworkAliases on _historyContainer.
            .WithEnvironment("HistoryBaseAddress", $"http://{nameof(_historyContainer)}:80")
            .Build();
    }

    public async Task DisposeAsync()
    {
        var calculatorDisposal = _calculatorContainer?.DisposeAsync() ?? ValueTask.CompletedTask;
        var historyDisposal = _historyContainer.DisposeAsync();

        await calculatorDisposal;
        await historyDisposal;
    }

    public async Task InitializeAsync()
    {
        await _network.CreateAsync();

        // We can start both of the containers in parallel
        // However, this might not work if some dependency must be resolved at start up
        // In our case, Calculator depends on History but uses it only when processing requests and not on startup
        await Task.WhenAll(
            Task.Run(async () =>
            {
                await _historyImage.CreateAsync();
                await _historyContainer.StartAsync();
            }),
            Task.Run(async () =>
            {
                await _calculatorImage.CreateAsync();
                await _calculatorContainer.StartAsync();
            })
        );

        _historyClient = new HttpClient()
        {
            // Use _container.Hostname which will resolve to 127.0.0.1 because
            // we want to connect from the test process (outside of docker network)
            BaseAddress = new Uri($"http://{_historyContainer.Hostname}:{_historyContainer.GetMappedPublicPort(80)}")
        };

        _calculatorClient = new HttpClient()
        {
            BaseAddress = new Uri($"http://{_calculatorContainer.Hostname}:{_calculatorContainer.GetMappedPublicPort(80)}")
        };
    }
}
