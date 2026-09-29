using GundamShop.Bll;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.OData;

namespace GundamShop.Api;

public class ProductsController(CatalogService catalog) : ODataController
{
    [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.Filter | AllowedQueryOptions.OrderBy | AllowedQueryOptions.Skip | AllowedQueryOptions.Top | AllowedQueryOptions.Count,
        MaxTop = 50, PageSize = 50, MaxNodeCount = 40)]
    public IQueryable<ProductODataDto> Get() => catalog.ODataProducts();
}
