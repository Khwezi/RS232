namespace RS232.Models.Configuration;

public class SerialOptions
{
    public string? PortName { get; set; }

    public int BaudRate { get; set; }

    public TimeSpan ReadTimeout { get; set; }

    public TimeSpan WriteTimeout { get; set; }
}
