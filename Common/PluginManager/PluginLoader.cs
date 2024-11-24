using System.Reflection;
using Qenex.QSuite.LogSystem;

namespace Qenex.QSuite.PluginManager;

public class PluginLoader(ILogger? logger = null)
{
    public List<T> LoadPlugins<T>(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            var msg = $"The directory '{directoryPath}' does not exist.";
            logger?.Log(LogLevel.Error, msg);
            throw new DirectoryNotFoundException(msg);
        }

        var driverInstances = new List<T>();

        var dllFiles = Directory.GetFiles(directoryPath, "*.dll");

        foreach (var dllFile in dllFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllFile);
                var types = assembly.GetTypes();
                var drivers = types.Where(t => typeof(T).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

                foreach (var driver in drivers)
                {
                    if (Activator.CreateInstance(driver) is T instance)
                    {
                        driverInstances.Add(instance);
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                logger?.Log(LogLevel.Warn, $"Could not load types from assembly '{dllFile}': {ex.Message}");
            }
            catch (Exception ex)
            {
                logger?.Log(LogLevel.Warn, $"Error loading assembly '{dllFile}': {ex.Message}");
            }
        }

        return driverInstances;
    }
}