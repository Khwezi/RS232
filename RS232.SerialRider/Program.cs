using Microsoft.Extensions.Configuration;
using RS232.Models.Configuration;
using System.IO.Ports;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

var settings = configuration.GetRequiredSection(nameof(SerialOptions)).Get<SerialOptions>();

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

var cancellationTokenSource = new CancellationTokenSource();
var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));

serialPort.Open();

Console.WriteLine($"Port IsOpen: {serialPort.IsOpen}");

if (serialPort.IsOpen)
    serialPort.WriteLine("AT");

do
{
    var line = serialPort.ReadLine();

    if(!string.IsNullOrEmpty(line.Trim()))
        Console.WriteLine(line);
} 
while (await periodicTimer.WaitForNextTickAsync(cancellationTokenSource.Token) && !cancellationTokenSource.IsCancellationRequested && serialPort.IsOpen);

serialPort.Close();

Console.WriteLine("Serial port closed, shutting down");

await periodicTimer.WaitForNextTickAsync();
periodicTimer.Dispose();

cancellationTokenSource.Cancel();
await Task.Delay(TimeSpan.FromSeconds(5));
