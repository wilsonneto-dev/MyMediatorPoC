using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IMediator, Mediator>();
builder.Services.AddTransient<IUseCase<CreateAccountInput, CreateAccountOutput>, CreateAccount>();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapGet("/accounts", () =>
{
    return new int[] { 1, 2, 3 };
}).WithOpenApi();

app.MapPost("/accounts", async ([FromBody] CreateAccountInput input, [FromServices] IMediator mediator) => {
    await mediator.Send<CreateAccountInput, CreateAccountOutput>(input);
    return input;
}).WithOpenApi();

app.Run();

interface IMediator { Task<TOutput> Send<TInput, TOutput>(TInput input) where TInput : IInput<TOutput>; }
interface IUseCase<TInput, TOutput> where TInput : IInput<TOutput> { Task<TOutput> Execute(TInput input); }
interface IInput<TOutput> { }

class Mediator : IMediator
{
    private readonly ILogger<Mediator> _logger;
    private readonly IServiceProvider _serviceProvider;

    public Mediator(ILogger<Mediator> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<TOutput> Send<TInput, TOutput>(TInput input) where TInput : IInput<TOutput>
    {
        var handler = _serviceProvider.GetService<IUseCase<TInput, TOutput>>();
        if(handler == null)
        {
            _logger.LogError("Handler not found for {Type} and {Out}", typeof(TInput), typeof(TOutput));
            return default;
        }

        _logger.LogError("Handler found \\o/ for {Type} and {Out}", typeof(TInput), typeof(TOutput));
        return await handler.Execute(input);
    }
}

record CreateAccountInput(string Email) : IInput<CreateAccountOutput>;

record CreateAccountOutput(Guid AccountId);

class CreateAccount : IUseCase<CreateAccountInput, CreateAccountOutput>
{
    private readonly ILogger<CreateAccount> _logger;

    public CreateAccount(ILogger<CreateAccount> logger) => _logger = logger;

    public Task<CreateAccountOutput> Execute(CreateAccountInput input)
    {
        _logger.LogInformation("Creating account {Email}", input.Email);
        return Task.FromResult(new CreateAccountOutput(Guid.NewGuid()));
    }
}