using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qenex.QSuite.VariableEvents
{
	public class PeriodicVarEvent : VarEvent
	{
		public int Period { get; set; }
		public TimeUnit Unit { get; set; }
	}
}
