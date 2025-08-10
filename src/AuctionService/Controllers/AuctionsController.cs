using System;
using AuctionService.Context;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly AuctionDbContext _context;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndpoint;

    public AuctionsController(AuctionDbContext context, IMapper mapper, IPublishEndpoint publishEndpoint)
    {
        this._context = context;
        this._mapper = mapper;
        this._publishEndpoint = publishEndpoint;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions(string date)
    {
        var query = _context.Auctions.OrderBy(x => x.Item.Make).AsQueryable();

        if (!string.IsNullOrEmpty(date))
        {
            query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date)) > 0);
        }

        return await query.ProjectTo<AuctionDto>(_mapper.ConfigurationProvider).ToListAsync();
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
    {
        var auction = await _context.Auctions
            .Include(a => a.Item)
            .FirstOrDefaultAsync(a => a.Id == id);
            
        if (auction is null)
            return NotFound();

        return _mapper.Map<Auction, AuctionDto>(auction);
    }
    [HttpPost]
    public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto)
    {
        var auction = _mapper.Map<CreateAuctionDto, Auction>(auctionDto);
        //TODO: add current user as seller
        auction.Seller = "furkansahin";

        _context.Auctions.Add(auction);

        var newAuction = _mapper.Map<Auction, AuctionDto>(auction);
        await _publishEndpoint.Publish(_mapper.Map<AuctionDto, AuctionCreated>(newAuction));

        var result = await _context.SaveChangesAsync() > 0;

        if (!result)
            return BadRequest("Couldn't save auction");

        return CreatedAtAction(nameof(GetAuctionById), new { auction.Id }, newAuction);
    }
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto auctionDto)
    {
        var auction = await _context.Auctions
            .Include(a => a.Item)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (auction is null)
            return NotFound();

        //TODO: check seller name matches with current user

        auction.Item.Make = auctionDto.Make ?? auction.Item.Make;
        auction.Item.Model = auctionDto.Model ?? auction.Item.Model;
        auction.Item.Color = auctionDto.Color ?? auction.Item.Color;
        auction.Item.Mileage = auctionDto.Mileage ?? auction.Item.Mileage;
        auction.Item.Year = auctionDto.Year ?? auction.Item.Year;

        var updatedAuction = _mapper.Map<Auction, AuctionDto>(auction);
        await _publishEndpoint.Publish(_mapper.Map<AuctionDto, AuctionUpdated>(updatedAuction));

        var result = await _context.SaveChangesAsync() > 0;

        if (!result)
            return BadRequest("Couldn't update auction");

        return Ok();
    }
    [HttpDelete("{id}")]
    public async Task<ActionResult<AuctionDto>> DeleteAuction(Guid id)
    {
        var auction = await _context.Auctions.FindAsync(id);

        if (auction is null)
            return NotFound();

        //TODO: check seller name matches with current user
        _context.Remove(auction);

        await _publishEndpoint.Publish(new AuctionDeleted{Id = id.ToString()});

        var result = await _context.SaveChangesAsync() > 0;

        if (!result)
            return BadRequest("Couldn't delete auction");

        return Ok();
    }
}
