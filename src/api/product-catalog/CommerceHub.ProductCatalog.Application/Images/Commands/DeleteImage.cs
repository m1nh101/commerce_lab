using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Images.Commands;

public sealed record DeleteImageCommand(Guid ProductId, long ImageId) : ICommand<Result>;

internal sealed class DeleteImageCommandHandler(IProductCatalogDbContext dbContext)
    : ICommandHandler<DeleteImageCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteImageCommand command, CancellationToken cancellationToken = default)
    {
        var image = await dbContext.ProductImages.FirstOrDefaultAsync(
            i => i.Id == command.ImageId && i.ProductId == command.ProductId, cancellationToken);
        if (image is null)
        {
            return ImageErrors.NotFound;
        }

        // Only the catalog reference is removed; the S3 object is left in place.
        dbContext.ProductImages.Remove(image);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
