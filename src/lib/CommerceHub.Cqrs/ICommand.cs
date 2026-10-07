namespace CommerceHub.Cqrs;

/// <summary>
/// A request that changes state and produces <typeparamref name="TResponse"/>.
/// </summary>
public interface ICommand<TResponse>;

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
