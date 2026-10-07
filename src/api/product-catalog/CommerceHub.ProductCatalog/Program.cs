using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using CommerceHub.ProductCatalog.Application;
using CommerceHub.ProductCatalog.Endpoints.Attributes;
using CommerceHub.ProductCatalog.Endpoints.Categories;
using CommerceHub.ProductCatalog.Endpoints.Products;
using CommerceHub.ProductCatalog.Endpoints.Variants;
using CommerceHub.ProductCatalog.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false));
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
})
// One OpenAPI document per API version (https://aka.ms/aspnet/openapi)
.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("CommerceHub Product Catalog API");
        options.AddDocuments(app.DescribeApiVersions().Select(d => d.GroupName));
    });
}

app.UseHttpsRedirection();

app.MapCategoryEndpoints();
app.MapAttributeEndpoints();
app.MapProductEndpoints();
app.MapVariantEndpoints();

app.Run();
