using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using LibraryService.Core.Contracts;
using LibraryService.Service.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();

builder.WebHost.UseNetTcp(8090);
builder.WebHost.ConfigureKestrel(o => o.ListenLocalhost(5000));

var app = builder.Build();

app.UseServiceModel(sb =>
{
    sb.AddService<LibraryServiceImpl>();

    sb.AddServiceEndpoint<LibraryServiceImpl, ILibraryService>(
        new BasicHttpBinding(), "/LibraryService/http");

    sb.AddServiceEndpoint<LibraryServiceImpl, ILibraryService>(
        new NetTcpBinding(SecurityMode.None), "/LibraryService/tcp");

    var smb = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    smb.HttpGetEnabled = true;
});

app.MapGet("/", () => "Library Service v1.0\n" +
    "HTTP: http://localhost:5000/LibraryService/http\n" +
    "TCP:  net.tcp://localhost:8090/LibraryService/tcp\n" +
    "WSDL: http://localhost:5000/LibraryService/http?wsdl");

Console.WriteLine("=== Library Service ===");
Console.WriteLine("HTTP: http://localhost:5000/LibraryService/http");
Console.WriteLine("TCP:  net.tcp://localhost:8090/LibraryService/tcp");
Console.WriteLine("Users: librarian/lib123, reader/read123, admin/admin123");
Console.WriteLine();

app.Run();
