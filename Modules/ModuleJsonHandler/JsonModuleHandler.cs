using Qenex.QSuite.Driver;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.Module;
using Qenex.QSuite.ModuleJsonHandler.JsonStructure;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.UnifModule;
using Qenex.QSuite.Variable;

namespace Qenex.QSuite.ModuleJsonHandler;

public class JsonModuleHandler
{
    private ILogger? logger;
    private IList<IDriverBase> drivers;
    private IList<IProtocolBase> protocols;
    
    public JsonModuleHandler(IList<IDriverBase> loadedDrivers, IList<IProtocolBase> loadedProtocols, ILogger? logger = null)
    {
        drivers = loadedDrivers;
        protocols = loadedProtocols;
        this.logger = logger;
    }
    
    public IModuleBase? GetModule(JsonModule jsonModule)
    {
        IModuleBase module = new UnifiedModule();
        
        if (module.Specification.Gid != jsonModule.Gid || module.Specification.Name != jsonModule.Name)
        {
            logger?.Log(LogLevel.Error, "Module Gid/Name does not match with the UnifiedModule Gid");
            return null;
        }
        
        module.Specification.Caption = jsonModule.Caption;
        module.Specification.Description = jsonModule.Description;
        module.Specification.Version = jsonModule.Version;
        module.Specification.Author = jsonModule.Author;
        module.Specification.Company = jsonModule.Company;
        module.Specification.CreatedOn = jsonModule.CreatedOn;

        //module.Drivers = new List<IDriverBase>();
        
        // Add drivers to module
        foreach (var jsonDriverRelation in jsonModule.JsonDriversRelations)
        {
            var foundJsonDrv = jsonModule.JsonDrivers.FirstOrDefault(d => d.Gid == jsonDriverRelation.DriverRefGid);
            if (foundJsonDrv == null)
            {
                logger?.Log(LogLevel.Error, $"Driver with Gid {jsonDriverRelation.DriverRefGid} not found in json drivers");
                return null;
            }
            
            var foundRealDrv = drivers.FirstOrDefault(d => d.Specification.Gid == jsonDriverRelation.DriverRefGid);
            if (foundRealDrv == null)
            {
                logger?.Log(LogLevel.Error, $"Driver {foundJsonDrv.Name} with Gid {jsonDriverRelation.DriverRefGid} not found in loaded drivers");
                return null;
            }
            foundRealDrv.IsEnabled = foundJsonDrv.IsEnabled;
            
            var foundProtocols = GetProtocolosBasedOnJson(jsonDriverRelation.JsonProtocolsRelations, jsonModule.JsonProtocols, jsonModule.JsonVariables);
            foreach (var foundProtocol in foundProtocols)
            {
                foundRealDrv.AddProtocol(foundProtocol);
            }
            module.AddDriver(foundRealDrv);
        }

        module.Variables = jsonModule.JsonVariables;

        return module;
    }
    
    private IList<IProtocolBase>? GetProtocolosBasedOnJson(IList<JsonProtocolRelation> jsonProtocolRelations, IList<JsonProtocol> jsonProtocols, IList<IVariableBase> jsonVariables)
    {
        var retProtocols = new List<IProtocolBase>();
        
        foreach (var jsonProtocolRelation in jsonProtocolRelations)
        {
            var foundJsonProtocol = jsonProtocols.FirstOrDefault(p => p.Gid == jsonProtocolRelation.ProtocolRefGid);
            if (foundJsonProtocol == null)
            {
                logger?.Log(LogLevel.Error, $"Protocol with Gid {jsonProtocolRelation.ProtocolRefGid} not found in json protocols");
                return null;
            }
            
            var foundProtocol = protocols.FirstOrDefault(p => p.Specification.Gid == jsonProtocolRelation.ProtocolRefGid);
            if (foundProtocol == null)
            {
                logger?.Log(LogLevel.Error, $"Protocol {foundJsonProtocol.Name} with Gid {jsonProtocolRelation.ProtocolRefGid} not found in loaded protocols");
                return null;
            }
            
            // Add variables to protocol
            var foundsVariables = GetVariablesBasedOnJson(jsonProtocolRelation.JsonVariablesRefGids, jsonVariables);
            foreach (var foundsVariable in foundsVariables)
            {
                foundProtocol.AddVariable(foundsVariable);
            }
            
            retProtocols.Add(foundProtocol);
        }

        return retProtocols;
    }
    
    private IList<IVariableBase> GetVariablesBasedOnJson(IList<Guid> variablesGids, IList<IVariableBase> jsonVariables)
    {
        var variables = new List<IVariableBase>();
        
        foreach (var variableGid in variablesGids)
        {
            var foundJsonVariable = jsonVariables.FirstOrDefault(v => v.Gid == variableGid);
            if (foundJsonVariable == null)
            {
                var msg = $"Variable with Gid {variableGid} not found in json variables";
                logger?.Log(LogLevel.Error, msg);
                throw new ArgumentException(msg);
            }
            
            variables.Add(foundJsonVariable);
        }

        return variables;
    }
    
}