using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Qenex.QSuite.Scripting.Script;

public enum ScriptExecutionMode
{
	[Display(Name = "Manual")]	Manual,
	[Display(Name ="Periodic")]	Periodic,
	[Display(Name = "Event Triggered")] EventTriggered,
	[Display(Name = "Startup")]	Startup,
	[Display(Name = "Shutdown")] Shutdown
	
}
