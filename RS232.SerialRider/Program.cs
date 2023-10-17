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
    Encoding = System.Text.Encoding.UTF8,
    BaudRate = settings.BaudRate,
    PortName = settings.PortName,
    Parity = Parity.None,
    DataBits = 8,
    StopBits = StopBits.One,
    Handshake = Handshake.None,
    DtrEnable = true,    
    ReadTimeout = 30000,
    WriteTimeout = 1000
};

var receiveBuffer = string.Empty;
serialPort.DataReceived += SerialPort_DataReceived;

Console.WriteLine("Opening Port: {0} at BaudRate {1}", settings.PortName, settings.BaudRate);

var cancellationTokenSource = new CancellationTokenSource();
var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));

serialPort.Open();
Console.WriteLine("Port IsOpen: {0}", serialPort.IsOpen);

if(!serialPort.IsOpen)
{
    Console.WriteLine("Failed to open portm exiting");
    Environment.Exit(0);
}

void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
{
    string existingData = serialPort.ReadExisting();
    Console.Write(existingData);

    try
    {
        var line = serialPort.ReadLine();

        if(!string.IsNullOrEmpty(line))
            Console.WriteLine(line);
    }
    catch(TimeoutException)
    {
        Console.WriteLine(".");
    }
}

try
{
    Console.WriteLine("Enter EXIT to stop program");

    do
    {
        if (!serialPort.IsOpen)
            continue;
            
        var line = Console.ReadLine();

        if (line?.ToLower().Equals("exit") == true)
            break;
        
        serialPort.WriteLine(line);
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
