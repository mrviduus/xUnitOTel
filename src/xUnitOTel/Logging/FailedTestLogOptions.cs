namespace xUnitOTel.Logging;

public class FailedTestLogOptions
{
    public bool Enabled { get; set; } = true;
    public string OutputDirectory { get; set; } = "logs";
}
