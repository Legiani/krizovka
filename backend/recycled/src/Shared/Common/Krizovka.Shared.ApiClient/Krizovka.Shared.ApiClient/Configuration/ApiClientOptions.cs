namespace Krizovka.Shared.ApiClient.Configuration;

public class ApiClientOptions
{
    public string SrsUrl { get; set; } = "http://localhost:5601";
    public string CpsUrl { get; set; } = "http://localhost:5639";
    public string TsUrl { get; set; } = "http://localhost:5631";
    public string PcsUrl { get; set; } = "http://localhost:5632";
    public string PspUrl { get; set; } = "http://localhost:5637";
    public string BfsUrl { get; set; } = "http://localhost:5640";
    public string NfsUrl { get; set; } = "http://localhost:5608";
    public string TksUrl { get; set; } = "http://localhost:5613";
    public string AajUrl { get; set; } = "http://localhost:5634";
    public string UsUrl { get; set; } = "http://localhost:5600";
    public string WvsUrl { get; set; } = "http://localhost:5636";
    public string ApcUrl { get; set; } = "http://localhost:5635";
    public string DkmUrl { get; set; } = "http://localhost:5614";
    public string AlsUrl { get; set; } = "http://localhost:5610";
    public string PsUrl { get; set; } = "http://localhost:5615";
    public string DsUrl { get; set; } = "http://localhost:5609";
    public string OtpUrl { get; set; } = "http://localhost:5638";

    public int DefaultTimeoutSeconds { get; set; } = 30;
}
