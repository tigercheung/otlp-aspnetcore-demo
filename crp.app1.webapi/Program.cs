using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//------------- instrumentataion ---------------------//
var serviceName = builder.Configuration.GetValue("ServiceName", defaultValue: "otel-test")!;
var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";

// Build a resource configuration action to set service information.
Action<ResourceBuilder> resource = r => r.AddService(
    serviceName: serviceName,
    serviceVersion: serviceVersion,
    serviceInstanceId: Environment.MachineName);

////logs
//Log.Logger = new LoggerConfiguration()
//    .WriteTo.OpenTelemetry(options =>
//    {
//        options.Endpoint = builder.Configuration.GetValue("Otlp/collector:Tracing:Endpoint", defaultValue: "http://localhost:4318")!;
//        options.Protocol = OtlpProtocol.HttpProtobuf;
//    })
//    .CreateLogger();

//add OpenTelemetry with tracing and config OpenTelemetry to export trace info to lightstep 
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder => tracerProviderBuilder
        .AddSource(serviceName)
        .ConfigureResource(resource)
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri(builder.Configuration.GetValue("Otlp/collector:Tracing:Endpoint", defaultValue: "http://localhost:4318")!);
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            //opt.HttpClientFactory = () =>
            //{
            //    var clientHandler = new HttpClientHandler
            //    {
            //        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            //    };

            //    HttpClient client = new(clientHandler);
            //    client.DefaultRequestHeaders.Add("lightstep-access-token", (builder.Configuration.GetValue("Otlp:LightstepAccessToken", defaultValue: "NO_TOKEN_FOUND")!));
            //    return client;
            //};
        })
    )
    .WithMetrics(metricsProviderBuilder => metricsProviderBuilder
        .ConfigureResource(resource)
        .AddConsoleExporter()
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri(builder.Configuration.GetValue("Otlp/collector:Metrics:Endpoint", defaultValue: "http://localhost:4318")!);
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            //opt.HttpClientFactory = () =>
            //{
            //    var clientHandler = new HttpClientHandler
            //    {
            //        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            //    };

            //    HttpClient client = new(clientHandler);
            //    client.DefaultRequestHeaders.Add("lightstep-access-token", (builder.Configuration.GetValue("Otlp:LightstepAccessToken", defaultValue: "NO_TOKEN_FOUND")!));
            //    return client;
            //};
        })
   );

var app = builder.Build();

app.UseHttpsRedirection();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseAuthorization();

app.MapControllers();

app.Run();
