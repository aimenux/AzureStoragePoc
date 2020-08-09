using System;
using System.Collections.Generic;
using System.Linq;
using Contracts.Models;
using Contracts.Payloads;
using Contracts.Ports.CosmosDb.Documents;
using static Contracts.Builders.RandomBuilderHelper;

namespace Contracts.Builders
{
    public class ContractsBuilder : IContractsBuilder
    {
        private readonly int _size;
        private readonly ICollection<Product> _products;
        private readonly ICollection<Order> _orders;
        private readonly ICollection<ProductDto> _productsDto;
        private readonly ICollection<OrderDto> _ordersDto;

        public ContractsBuilder(int size)
        {
            _size = size;
            _products = RandomProducts();
            _orders = RandomOrders();
            _productsDto = RandomProductsDto();
            _ordersDto = RandomOrdersDto();
        }

        public IData BuildDataContracts(string transactionId)
        {
            return new Data
            {
                Request = BuildRequest(transactionId),
                Response = BuildResponse(transactionId)
            };
        }

        public IPayload BuildPayloadContracts(string transactionId)
        {
            return new Payload
            {
                RequestDto = BuildRequestDto(transactionId),
                ResponseDto = BuildResponseDto(transactionId)
            };
        }

        private Request BuildRequest(string transactionId)
        {
            return new Request
            {
                TransactionId = transactionId,
                TransactionDate = DateTime.Now,
                Products = _products,
                Client = new Client
                {
                    Email = RandomEmail(),
                    BirthDate = RandomDate(),
                    FirstName = RandomString(),
                    LastName = RandomString(),
                }
            };
        }

        private Response BuildResponse(string transactionId)
        {
            return new Response
            {
                TransactionId = transactionId,
                TransactionDate = DateTime.Now,
                Orders = _orders
            };
        }

        private RequestDto BuildRequestDto(string transactionId)
        {
            return new RequestDto
            {
                TransactionId = transactionId,
                TransactionDate = DateTime.Now,
                Products = _productsDto,
                Client = new ClientDto
                {
                    Email = RandomEmail(),
                    BirthDate = RandomDate(),
                    FirstName = RandomString(),
                    LastName = RandomString()
                }
            };
        }

        private ResponseDto BuildResponseDto(string transactionId)
        {
            return new ResponseDto
            {
                TransactionId = transactionId,
                TransactionDate = DateTime.Now,
                Orders = _ordersDto
            };
        }

        private ICollection<Product> RandomProducts()
        {
            return Enumerable.Range(0, _size)
                .Select(_ => new Product
                {
                    Quantity = RandomNumber(),
                    UnitPrice = RandomNumber(),
                    ProductType = RandomString()
                })
                .ToList();
        }

        private ICollection<Order> RandomOrders()
        {
            return _products.Select(x => new Order
            {
                Product = x,
                OrderId = Guid.NewGuid().ToString(),
                Merchant = new Merchant
                {
                    MerchantId = Guid.NewGuid().ToString(),
                    Name = RandomString()
                },
                ProductTax = new ProductTax
                {
                    TaxRate = (decimal) RandomRate(),
                    Region = RandomString(),
                    Country = RandomString()
                }
            }).ToList();
        }

        private ICollection<ProductDto> RandomProductsDto()
        {
            return _products.Select(x => new ProductDto
            {
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice,
                ProductType = x.ProductType
            }).ToList();
        }

        private ICollection<OrderDto> RandomOrdersDto()
        {
            return _orders.Select(Map).ToList();
        }

        private static OrderDto Map(Order order)
        {
            return new OrderDto
            {
                OrderId = order.OrderId,
                OrderPrice = order.OrderPrice,
                Product = new ProductDto
                {
                    ProductType = order.Product.ProductType,
                    UnitPrice = order.Product.UnitPrice,
                    Quantity = order.Product.Quantity
                }
            };
        }
    }
}