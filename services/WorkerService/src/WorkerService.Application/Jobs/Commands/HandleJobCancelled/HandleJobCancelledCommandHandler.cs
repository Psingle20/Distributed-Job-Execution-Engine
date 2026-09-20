using CleanArchitecture.BuildingBlocks;
using CleanArchitecture.BuildingBlocks.Messaging;
using WorkerService.Application.Abstractions;

namespace WorkerService.Application.Jobs.Commands.HandleJobCancelled;

internal sealed class HandleJobCancelledCommandHandler(
    IProcessedMessageRepository processedMessages,
    IUnitOfWork unitOfWork) : ICommandHandler<HandleJobCancelledCommand>
{
    public async Task<Result> Handle(HandleJobCancelledCommand command, CancellationToken cancellationToken)
    {
        if (await processedMessages.ExistsAsync(command.MessageId, cancellationToken))
        {
            return Result.Success();
        }

        await processedMessages.AddAsync(command.MessageId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
