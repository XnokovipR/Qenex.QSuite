namespace Qenex.QSuite.Drivers.Driver;

public interface IDataLogCsvExportDriver
{
    Task ExportCsvAsync(string filePath, CancellationToken ct = default);
}
