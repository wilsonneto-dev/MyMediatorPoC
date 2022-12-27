using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IMediator, Mediator>();
builder.Services.AddTransient<IUseCase<CreateAccountInput>, CreateAccount>();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapGet("/accounts", () =>
{
    return new int[] { 1, 2, 3 };
}).WithOpenApi();

app.MapPost("/accounts", ([FromBody] CreateAccountInput input, [FromServices] IMediator mediator) => {
    mediator.Send(input);
    return input;
}).WithOpenApi();

app.Run();

interface IMediator { Task Send<T>(T input); }
interface IUseCase<TInput> { Task Execute(TInput input); }

class Mediator : IMediator
{
    private readonly ILogger<Mediator> _logger;
    private readonly IServiceProvider _serviceProvider;

    public Mediator(ILogger<Mediator> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task Send<T>(T input)
    {
        var handler = _serviceProvider.GetService<IUseCase<T>>();
        if(handler == null)
        {
            _logger.LogError("Handler not found for {Type}", typeof(T));
            return;
        }

        await handler.Execute(input);
    }
}

record CreateAccountInput(string Email);

class CreateAccount : IUseCase<CreateAccountInput>
{
    private readonly ILogger<CreateAccount> _logger;

    public CreateAccount(ILogger<CreateAccount> logger) => _logger = logger;

    public Task Execute(CreateAccountInput input)
    {
        _logger.LogInformation("Creating account {Email}", input.Email);
        return Task.CompletedTask;
    }
}