﻿using AutoMapper;
using FluentPOS.Modules.Catalog.Core.Entites;
using FluentPOS.Modules.Catalog.Core.Features.Products.Commands;
using FluentPOS.Shared.DTOs.Catalogs.Products;

namespace FluentPOS.Modules.Catalog.Core.Mappings
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            CreateMap<AddEditProductCommand, Product>().ReverseMap();
            CreateMap<GetProductByIdResponse, Product>().ReverseMap();
            CreateMap<GetAllPagedProductsResponse, Product>().ReverseMap();
        }
    }
}
