using System;
using AuctionService.Context;
using Contracts;
using MassTransit;

namespace AuctionService.Consumers;

public class AuctionFinishedConsumer : IConsumer<AuctionFinished>
{
    private readonly AuctionDbContext _context;

    public AuctionFinishedConsumer(AuctionDbContext context)
    {
        this._context = context;
    }
    public async Task Consume(ConsumeContext<AuctionFinished> context)
    {
        System.Console.WriteLine("consuming auction finished");
        var auction = await _context.Auctions.FindAsync(context.Message.AuctionId);

        if (context.Message.ItemSold)
        {
            auction.Winner = context.Message.Winner;
            auction.SoldAmount = context.Message.Amount;
        }
        auction.Status = auction.SoldAmount > auction.ReservePrice
            ? Entities.Status.Finished : Entities.Status.ReserveNotMet;

        await _context.SaveChangesAsync();
    }
}
