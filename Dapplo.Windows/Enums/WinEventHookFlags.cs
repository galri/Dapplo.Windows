﻿//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016 Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System;

#endregion

namespace Dapplo.Windows.Enums
{
	/// <summary>
	///     Used for User32.SetWinEventHook
	///     See <a href="http://msdn.microsoft.com/en-us/library/windows/desktop/dd373640.aspx">here</a>
	/// </summary>
	[Flags]
	public enum WinEventHookFlags
	{
		/// <summary>
		/// The callback function is not mapped into the address space of the process that generates the event.
		/// Because the hook function is called across process boundaries, the system must queue events.
		/// Although this method is asynchronous, events are guaranteed to be in sequential order.
		/// For more information, see Out-of-Context Hook Functions.
		/// </summary>
		OutOfContext = 0,
		/// <summary>
		/// Prevents this instance of the hook from receiving the events that are generated by threads in this process.
		/// This flag does not prevent threads from generating events.
		/// </summary>
		SkipOwnThread = 1,
		/// <summary>
		/// Prevents this instance of the hook from receiving the events that are generated by the thread that is registering this hook.
		/// </summary>
		SkipOwnProcess = 2,
		/// <summary>
		/// The DLL that contains the callback function is mapped into the address space of the process that generates the event.
		/// With this flag, the system sends event notifications to the callback function as they occur.
		/// The hook function must be in a DLL when this flag is specified.
		/// This flag has no effect when both the calling process and the generating process are not 32-bit or 64-bit processes,
		/// or when the generating process is a console application.
		/// For more information, see In-Context Hook Functions.
		/// </summary>
		InContext = 4
	}
}