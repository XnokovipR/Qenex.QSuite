using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qenex.QSuite.Variables.VariableEvents
{
	public class EventsGlobal
	{
		/// <summary>
		/// Enumerable of variable event types.
		/// </summary>
		public enum VariableEventType { Undefined = 0, Periodic, OnRequest, OnValueChanged }

		/// <summary>
		/// Dictionary of event types and their corresponding string representation.
		/// </summary>
		public static readonly Dictionary<VariableEventType, string> VariableEventTypeDict = new Dictionary<VariableEventType, string>()
		{
			{ VariableEventType.Periodic, "Qenex.QSuite.VariableEvents.PeriodicVarEvent" },
			{ VariableEventType.OnRequest, "Qenex.QSuite.VariableEvents.OnRequestVarEvent" },
			{ VariableEventType.OnValueChanged, "Qenex.QSuite.VariableEvents.OnValueChangedVarEvent" }
		};
		
		public static IVarEvent CreateInstance(VariableEventType eventType)
		{
			if (!VariableEventTypeDict.TryGetValue(eventType, out var stringType))
			{
				throw new ArgumentException($"Type {eventType} not found in dictionary.");
			}
			
			var type = Type.GetType(stringType);
			if (type == null)
			{
				throw new ArgumentException($"Type {stringType} not found.");
			}

			if (Activator.CreateInstance(type) is not IVarEvent createdInstance)
			{
				throw new ArgumentException($"Instance of {type} not created.");
			}
			
			return createdInstance;
		}
	}
}
