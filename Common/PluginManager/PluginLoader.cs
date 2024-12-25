using System.Reflection;
using Qenex.QSuite.ComponentSpecification;
using Qenex.QSuite.LogSystem;

namespace Qenex.QSuite.PluginManager;

public class PluginLoader(ILogger? logger = null)
{
    public List<PluginDetails> GetPluginDetails<T>(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            var msg = $"The directory '{directoryPath}' does not exist.";
            logger?.Log(LogLevel.Error, msg);
            throw new DirectoryNotFoundException(msg);
        }

        var pluginDetails = new List<PluginDetails>();

        var dllFiles = Directory.GetFiles(directoryPath, "*.dll");

        foreach (var dllFile in dllFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllFile);
                var types = assembly.GetTypes();
                var plugins = types.Where(t => typeof(T).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

                foreach (var plugin in plugins)
                {
                    if (Activator.CreateInstance(plugin) is T instance)
                    {
                        if (instance is IComponentSpecification pluginInstance)
                        {
                            pluginDetails.Add(new PluginDetails()
                            {
                                Name = pluginInstance.Specification.Name,
                                Version = pluginInstance.Specification.Version ?? throw new Exception("Loaded plugin version not found."),
                                PathName = dllFile
                            });
                        }
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

        return pluginDetails;
    }
    
    
    public T? LoadPlugin<T>(string filePath) where T : class
    {
        if (!File.Exists(filePath))
        {
            var msg = $"The file '{filePath}' does not exist.";
            logger?.Log(LogLevel.Error, msg);
            throw new DirectoryNotFoundException(msg);
        }
        
        try
        {
            var assembly = Assembly.LoadFrom(filePath);
            var types = assembly.GetTypes();
            var drivers = types.Where(t => typeof(T).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

            foreach (var driver in drivers)
            {
                if (Activator.CreateInstance(driver) is T instance)
                {
                    return instance;
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            logger?.Log(LogLevel.Warn, $"Could not load types from assembly '{filePath}': {ex.Message}");
        }
        catch (Exception ex)
        {
            logger?.Log(LogLevel.Warn, $"Error loading assembly '{filePath}': {ex.Message}");
        }
        
        return null;
    }
    
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