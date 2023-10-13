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
    //WriteTimeout = (int)settings.WriteTimeout.TotalSeconds,
    //ReadTimeout = (int)settings.ReadTimeout.TotalSeconds,
};

Console.WriteLine("Opening Port: {0} at BaudRate {1}", settings.PortName, settings.BaudRate);

var cancellationTokenSource = new CancellationTokenSource();
var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));

serialPort.Open();
Console.WriteLine("Port IsOpen: {0}", serialPort.IsOpen);

if(!serialPort.IsOpen)
{
    Console.WriteLine("Failed to open portm exiting");
    System.Environment.Exit(0);
}

var serialStreamReader = new StreamReader(serialPort.BaseStream);

try
{
    if (serialPort.IsOpen)
    {
        serialPort.WriteLine("AT");
        Console.WriteLine("Sent wake AT command");

        await Task.Delay(TimeSpan.FromSeconds(5), cancellationTokenSource.Token);

        serialPort.WriteLine("AT+GPSRD=1");
        Console.WriteLine("Sent wake AT+GPSRD=1 command");
    }

    int count = 0;

    do
    {
        //var line = serialPort.ReadLine();
        var line = serialStreamReader.ReadLine();

        if (!string.IsNullOrEmpty(line.Trim()))
            logger.LogInformation(line);

        if (count++ == 100)
            break;
    }
    while (await periodicTimer.WaitForNextTickAsync(cancellationTokenSource.Token) && !cancellationTokenSource.IsCancellationRequested && serialPort.IsOpen);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
finally
{
    if (serialPort.IsOpen)
        serialPort.Close();
}

Console.WriteLine("Serial port closed, shutting down");

await periodicTimer.WaitForNextTickAsync();
periodicTimer.Dispose();

cancellationTokenSource.Cancel();
await Task.Delay(TimeSpan.FromSeconds(5));
