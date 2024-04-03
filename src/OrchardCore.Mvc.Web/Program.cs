using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
    })
    .Configure<BrotliCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Fastest;
    })
    .Configure<GzipCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.SmallestSize;
    })
    .AddResponseCaching()
    .AddOrchardCore()
    .AddMvc();

//builder.Services.AddControllersWithViews().AddMvcOptions(options =>
//    options.Filters.Add(
//        new ResponseCacheAttribute
//        {
//            NoStore = true,
//            Location = ResponseCacheLocation.None
//        }));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
builder.Services.AddResponseCaching();

app.UseResponseCaching();

app.UseResponseCompression();

app.UseStaticFiles();

app.UseOrchardCore();

await app.RunAsync();
