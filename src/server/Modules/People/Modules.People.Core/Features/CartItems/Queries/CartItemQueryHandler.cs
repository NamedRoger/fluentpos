﻿using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentPOS.Modules.People.Core.Abstractions;
using FluentPOS.Modules.People.Core.Entities;
using FluentPOS.Modules.People.Core.Exceptions;
using FluentPOS.Shared.Core.Extensions;
using FluentPOS.Shared.Core.Interfaces.Services.Catalog;
using FluentPOS.Shared.Core.Wrapper;
using FluentPOS.Shared.DTOs.People.CartItems;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace FluentPOS.Modules.People.Core.Features.CartItems.Queries
{
    internal class CartItemQueryHandler :
        IRequestHandler<GetCartItemsQuery, PaginatedResult<GetCartItemsResponse>>,
        IRequestHandler<GetCartItemByIdQuery, Result<GetCartItemByIdResponse>>
    {
        private readonly IPeopleDbContext _context;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer<CartItemQueryHandler> _localizer;
        private readonly IProductService _productService;

        public CartItemQueryHandler(IPeopleDbContext context, IMapper mapper, IStringLocalizer<CartItemQueryHandler> localizer, IProductService productService)
        {
            _context = context;
            _mapper = mapper;
            _localizer = localizer;
            _productService = productService;
        }

        public async Task<PaginatedResult<GetCartItemsResponse>> Handle(GetCartItemsQuery request, CancellationToken cancellationToken)
        {
            Expression<Func<CartItem, GetCartItemsResponse>> expression = e => new GetCartItemsResponse(e.Id, e.CartId, e.ProductId, e.Quantity);
            var queryable = _context.CartItems.AsQueryable();
            if (request.OrderBy?.Any() == true)
            {
                var ordering = string.Join(",", request.OrderBy);
                queryable = queryable.OrderBy(ordering);
            }
            else
            {
                queryable = queryable.OrderBy(a => a.Id);
            }
            if (request.CartId != null && !request.CartId.Equals(Guid.Empty)) queryable = queryable.Where(x => x.CartId.Equals(request.CartId));
            var cartItemList = await queryable
                .Select(expression)
                .ToPaginatedListAsync(request.PageNumber, request.PageSize);
            if (cartItemList == null) throw new CartNotFoundException();
            var mappedCartItems = _mapper.Map<PaginatedResult<GetCartItemsResponse>>(cartItemList);
            foreach (GetCartItemsResponse item in mappedCartItems.Data)
            {
                var details = await _productService.GetDetails(item.ProductId);
                if (details.Succeeded)
                {
                    item.ProductName = details.Data.Name;
                    item.ProductDescription = details.Data.Detail;
                    item.Rate = details.Data.Price;
                }
            }
            return mappedCartItems;
        }

        public async Task<Result<GetCartItemByIdResponse>> Handle(GetCartItemByIdQuery query, CancellationToken cancellationToken)
        {
            var cartItem = await _context.CartItems.Where(b => b.Id == query.Id).FirstOrDefaultAsync(cancellationToken);
            if (cartItem == null) throw new PeopleException(_localizer["Cart Item Not Found!"], HttpStatusCode.NotFound);
            var mappedCartItem = _mapper.Map<GetCartItemByIdResponse>(cartItem);
            return await Result<GetCartItemByIdResponse>.SuccessAsync(mappedCartItem);
        }
    }
}