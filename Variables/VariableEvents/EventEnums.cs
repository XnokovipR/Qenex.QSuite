using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qenex.QSuite.VariableEvents
{
	public enum EventDirection
	{
		Read = 0,
		Write,
		ReadWrite
	}

	public enum TimeUnit
	{
		Day = 0,
		Hour,
		Min,
		Sec,
		Milisec
	}
}
