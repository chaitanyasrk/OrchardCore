using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using OrchardCore.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseNLogHost();

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
    .AddOrchardCms()
    .AddSetupFeatures("OrchardCore.AutoSetup");

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



app.UseResponseCaching();

app.UseResponseCompression();

app.UseStaticFiles();

app.UseOrchardCore();

await app.RunAsync();
