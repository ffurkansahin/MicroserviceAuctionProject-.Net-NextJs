using System;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers;

public class AuctionDeletedConsumer : IConsumer<AuctionDeleted>
{
    public async Task Consume(ConsumeContext<AuctionDeleted> context)
    {
        Console.WriteLine("Consuming auction deleted");

        var deletedItemId = context.Message.Id;
        var result = await DB.DeleteAsync<Item>(i => i.ID == deletedItemId);

        if (!result.IsAcknowledged)
            throw new MessageException(typeof(AuctionDeleted), "Problem occured while deleting on MongoDb");
    }
}
