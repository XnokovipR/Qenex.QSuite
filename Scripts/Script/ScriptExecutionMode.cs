using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Qenex.QSuite.Scripts.Script;

public enum ScriptExecutionMode
{
	[Display(Name = "Manual")]	Manual,
	[Display(Name ="Periodic")]	Periodic,
	[Display(Name = "On Value Changed")] OnValueChanged,
	[Display(Name = "Startup")]	Startup
}
