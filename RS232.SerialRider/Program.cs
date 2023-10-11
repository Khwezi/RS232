using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RS232.Models.Configuration;
using System.IO.Ports;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

var services = new ServiceCollection().AddLogging().BuildServiceProvider();
var settings = configuration.GetRequiredSection(nameof(SerialOptions)).Get<SerialOptions>();

var logger = services.GetRequiredService<ILogger<Program>>();

ArgumentNullException.ThrowIfNull(settings);

ArgumentException.ThrowIfNullOrEmpty(settings.PortName, nameof(settings.PortName));

if (settings.BaudRate <= 0) 
    throw new ArgumentException("BaudRate is invalid");

var serialPort = new SerialPort
{
    BaudRate = settings.BaudRate,
    PortName = settings.PortName,
    Parity = Parity.None,
    DataBits = 8,
    StopBits = StopBits.One,
    Handshake = Handshake.None,
    WriteTimeout = (int)settings.WriteTimeout.TotalSeconds,
    ReadTimeout = (int)settings.ReadTimeout.TotalSeconds,
};

logger.LogInformation("Opening Port: {0} at BaudRate {1}", settings.PortName, settings.BaudRate);

var cancellationTokenSource = new CancellationTokenSource();
var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));

serialPort.Open();

logger.LogInformation("Port IsOpen: {0}", serialPort.IsOpen);

if (serialPort.IsOpen)
{
    serialPort.WriteLine("AT");
    logger.LogWarning("Sent wake AT command");
}

do
{
    var line = serialPort.ReadLine();

    if(!string.IsNullOrEmpty(line.Trim()))
        logger.LogInformation(line);
} 
while (await periodicTimer.WaitForNextTickAsync(cancellationTokenSource.Token) && !cancellationTokenSource.IsCancellationRequested && serialPort.IsOpen);

serialPort.Close();

logger.LogWarning("Serial port closed, shutting down");

await periodicTimer.WaitForNextTickAsync();
periodicTimer.Dispose();

cancellationTokenSource.Cancel();
await Task.Delay(TimeSpan.FromSeconds(5));
