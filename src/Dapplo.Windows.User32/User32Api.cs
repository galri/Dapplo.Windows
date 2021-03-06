﻿//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Windows.Common;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Messages;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.SafeHandles;
using Dapplo.Windows.User32.Structs;

#endregion

namespace Dapplo.Windows.User32
{
    /// <summary>
    ///     Native wrappers for the User32 DLL
    /// </summary>
    public static class User32Api
    {
        /// <summary>
        /// The DLL Name for the User32 library
        /// </summary>
        public const string User32 = "user32";

        /// <summary>
        ///     Delegate description for the windows enumeration
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="lParam"></param>
        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        private static readonly LogSource Log = new LogSource();

        private static bool _canCallGetPhysicalCursorPos = true;

        /// <summary>
        ///     Returns the number of Displays using the Win32 functions
        /// </summary>
        /// <returns>collection of Display Info</returns>
        public static IEnumerable<DisplayInfo> AllDisplays()
        {
            var result = new List<DisplayInfo>();
            int index = 1;
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr monitor, IntPtr hdcMonitor, ref NativeRect lprcMonitor, IntPtr data) =>
            {
                var monitorInfoEx = MonitorInfoEx.Create();
                var success = GetMonitorInfo(monitor, ref monitorInfoEx);
                if (!success)
                {
                    return true;
                }
                var displayInfo = new DisplayInfo
                {
                    Index = index++,
                    ScreenWidth = Math.Abs(monitorInfoEx.Monitor.Right - monitorInfoEx.Monitor.Left),
                    ScreenHeight = Math.Abs(monitorInfoEx.Monitor.Bottom - monitorInfoEx.Monitor.Top),
                    Bounds = monitorInfoEx.Monitor,
                    WorkingArea = monitorInfoEx.WorkArea,
                    IsPrimary = (monitorInfoEx.Flags & MonitorInfoFlags.Primary) == MonitorInfoFlags.Primary
                };
                result.Add(displayInfo);
                return true;
            }, IntPtr.Zero);
            return result;
        }

        /// <summary>
        ///     Helper method to create a Win32 exception with the windows message in it
        /// </summary>
        /// <param name="method">string with current method</param>
        /// <returns>Exception</returns>
        public static Exception CreateWin32Exception(string method)
        {
            var exceptionToThrow = new Win32Exception();
            exceptionToThrow.Data.Add("Method", method);
            return exceptionToThrow;
        }

        /// <summary>
        ///     Wrapper for the GetClassLong which decides if the system is 64-bit or not and calls the right one.
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <param name="index">ClassLongIndex</param>
        /// <returns>IntPtr</returns>
        public static IntPtr GetClassLongWrapper(IntPtr hWnd, ClassLongIndex index)
        {
            if (IntPtr.Size > 4)
            {
                return GetClassLongPtr(hWnd, index);
            }
            return GetClassLong(hWnd, index);
        }

        /// <summary>
        ///     Retrieve the windows classname
        /// </summary>
        /// <param name="hWnd">IntPtr for the window</param>
        /// <returns>string</returns>
        public static string GetClassname(IntPtr hWnd)
        {
            var classNameBuilder = new StringBuilder(260, 260);
            var hresult = GetClassName(hWnd, classNameBuilder, classNameBuilder.Capacity);
            if (hresult == 0)
            {
                return null;
            }
            return classNameBuilder.ToString();
        }

        /// <summary>
        ///     Retrieves the cursor location safely, accounting for DPI settings in Vista/Windows 7.
        /// </summary>
        /// <returns>
        ///     NativePoint with cursor location, relative to the origin of the monitor setup
        ///     (i.e. negative coordinates arepossible in multiscreen setups)
        /// </returns>
        public static NativePoint GetCursorLocation()
        {
            if (Environment.OSVersion.Version.Major >= 6 && _canCallGetPhysicalCursorPos)
            {
                try
                {
                    NativePoint cursorLocation;
                    if (GetPhysicalCursorPos(out cursorLocation))
                    {
                        return cursorLocation;
                    }
                    var error = Win32.GetLastErrorCode();
                    Log.Error().WriteLine("Error retrieving PhysicalCursorPos : {0}", Win32.GetMessage(error));
                }
                catch (Exception ex)
                {
                    Log.Error().WriteLine(ex, "Exception retrieving PhysicalCursorPos, no longer calling this. Cause :");
                    _canCallGetPhysicalCursorPos = false;
                }
            }
            return new NativePoint(Cursor.Position.X, Cursor.Position.Y);
        }

        /// <summary>
        ///     Return the count of GDI objects.
        /// </summary>
        /// <returns>Return the count of GDI objects.</returns>
        public static uint GetGuiResourcesGdiCount()
        {
            using (var currentProcess = Process.GetCurrentProcess())
            {
                return GetGuiResources(currentProcess.Handle, 0);
            }
        }

        /// <summary>
        ///     Return the count of USER objects.
        /// </summary>
        /// <returns>Return the count of USER objects.</returns>
        public static uint GetGuiResourcesUserCount()
        {
            using (var currentProcess = Process.GetCurrentProcess())
            {
                return GetGuiResources(currentProcess.Handle, 1);
            }
        }

        /// <summary>
        ///     Retrieve the windows caption, also called Text
        /// </summary>
        /// <param name="hWnd">IntPtr for the window</param>
        /// <returns>string</returns>
        public static string GetText(IntPtr hWnd)
        {
            var caption = new StringBuilder(260, 260);
            GetWindowText(hWnd, caption, caption.Capacity);
            return caption.ToString();
        }

        /// <summary>
        ///     Get the text of a control, this is not the caption
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <returns>string with the text</returns>
        public static string GetTextFromWindow(IntPtr hWnd)
        {
            // Get the size of the string required to hold the window's text. 
            var size = SendMessage(hWnd, WindowsMessages.WM_GETTEXTLENGTH, 0, 0).ToInt32();

            // If the return is 0, there is no text. 
            if (size <= 0)
            {
                return null;
            }
            var text = new StringBuilder(size + 1);

            SendMessage(hWnd, WindowsMessages.WM_GETTEXT, text.Capacity, text);
            return text.ToString();
        }

        /// <summary>
        ///     Get the titlebar info ex for the specified window
        /// </summary>
        /// <param name="hWnd">IntPtr with the window handle</param>
        /// <returns>TitleBarInfoEx</returns>
        public static TitleBarInfoEx GetTitleBarInfoEx(IntPtr hWnd)
        {
            var result = TitleBarInfoEx.Create();
            SendMessage(hWnd, WindowsMessages.WM_GETTITLEBARINFOEX, IntPtr.Zero, ref result);
            return result;
        }

        /// <summary>
        ///     Wrapper for the GetWindowLong which decides if the system is 64-bit or not and calls the right one.
        /// </summary>
        /// <param name="hwnd">IntPtr</param>
        /// <param name="index">WindowLongIndex</param>
        /// <returns></returns>
        public static long GetWindowLongWrapper(IntPtr hwnd, WindowLongIndex index)
        {
            return IntPtr.Size == 8 ?
                GetWindowLongPtr(hwnd, index).ToInt64() :
                GetWindowLong(hwnd, index);
        }

        /// <summary>
        ///     Wrapper for the SetWindowLong which decides if the system is 64-bit or not and calls the right one.
        /// </summary>
        /// <param name="hwnd">IntPtr</param>
        /// <param name="index">WindowLongIndex</param>
        /// <param name="styleFlags"></param>
        public static void SetWindowLongWrapper(IntPtr hwnd, WindowLongIndex index, IntPtr styleFlags)
        {
            if (IntPtr.Size == 8)
            {
                SetWindowLongPtr(hwnd, index, styleFlags);
            }
            else
            {
                SetWindowLong(hwnd, index, styleFlags.ToInt32());
            }
        }

        private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref NativeRect lprcMonitor, IntPtr dwData);

        #region Native imports

        /// <summary>
        /// Determines the visibility state of the specified window.
        /// </summary>
        /// <param name="hWnd">A handle to the window to be tested.</param>
        /// <returns>
        /// If the specified window, its parent window, its parent's parent window, and so forth, have the WS_VISIBLE style, the return value is nonzero. Otherwise, the return value is zero.
        /// Because the return value specifies whether the window has the WS_VISIBLE style, it may be nonzero even if the window is totally obscured by other windows.
        /// 
        /// Remarks:
        /// The visibility state of a window is indicated by the WS_VISIBLE style bit. When WS_VISIBLE is set, the window is displayed and subsequent drawing into it is displayed as long as the window has the WS_VISIBLE style.
        /// Any drawing to a window with the WS_VISIBLE style will not be displayed if the window is obscured by other windows or is clipped by its parent window.
        /// </returns>
        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>
        /// Determines whether the specified window handle identifies an existing window.
        /// </summary>
        /// <param name="hWnd">A handle to the window to be tested.</param>
        /// <returns>
        /// If the window handle identifies an existing window, the return value is true.
        /// If the window handle does not identify an existing window, the return value is false.
        /// </returns>
        [DllImport(User32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);

        /// <summary>
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633522(v=vs.85).aspx">GetWindowThreadProcessId function</a>
        /// Retrieves the identifier of the thread that created the specified window and, optionally, the identifier of the process that created the window.
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="processId">A pointer to a variable that receives the process identifier. If this parameter is not NULL, GetWindowThreadProcessId copies the identifier of the process to the variable; otherwise, it does not.</param>
        /// <returns>The return value is the identifier of the thread that created the window.</returns>
        [DllImport(User32, SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        /// <summary>
        ///     Retrieves a handle to the specified window's parent or owner.
        ///     To retrieve a handle to a specified ancestor, use the GetAncestor function.
        /// </summary>
        /// <param name="hWnd">A handle to the window whose parent window handle is to be retrieved.</param>
        /// <returns>
        ///     IntPtr handle to the parent window or IntPtr.Zero if none
        ///     If the window is a child window, the return value is a handle to the parent window. If the window is a top-level
        ///     window with the WS_POPUP style, the return value is a handle to the owner window.
        /// </returns>
        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633541(v=vs.85).aspx">SetParent function</a>
        ///     Changes the parent window of the specified child window.
        /// </summary>
        /// <param name="hWndChild">IntPtr</param>
        /// <param name="hWndNewParent">IntPtr</param>
        /// <returns>
        ///     If the function succeeds, the return value is a handle to the previous parent window.
        ///     If the function fails, the return value is NULL. To get extended error information, call GetLastError.
        /// </returns>
        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        /// <summary>
        ///     Retrieves a handle to a window that has the specified relationship (Z-Order or owner) to the specified window.
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633515(v=vs.85).aspx">GetWindow function</a>
        /// </summary>
        /// <param name="hWnd">
        ///     IntPtr A handle to a window. The window handle retrieved is relative to this window, based on the
        ///     value of the uCmd parameter.
        /// </param>
        /// <param name="getWindowCommand">
        ///     GetWindowCommands The relationship between the specified window and the window whose
        ///     handle is to be retrieved. See GetWindowCommands
        /// </param>
        /// <returns></returns>
        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, GetWindowCommands getWindowCommand);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633548(v=vs.85).aspx">ShowWindow function</a>
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="nCmdShow">
        ///     ShowWindowCommands
        ///     Controls how the window is to be shown.
        ///     This parameter is ignored the first time an application calls ShowWindow, if the program that launched the
        ///     application provides a STARTUPINFO structure.
        ///     Otherwise, the first time ShowWindow is called, the value should be the value obtained by the WinMain function in
        ///     its nCmdShow parameter.
        ///     In subsequent calls, this parameter can be one of the following values.
        /// </param>
        /// <returns>bool</returns>
        [DllImport(User32, SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

        /// <summary>
        ///     Get the caption of the window
        /// </summary>
        /// <param name="hWnd">IntPtr with the window handle</param>
        /// <param name="lpString">StringBuilder which is marshalled as buffer</param>
        /// <param name="capacity">size of the buffer</param>
        /// <returns>int with the size of the caption</returns>
        [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int capacity);

        /// <summary>
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633521.aspx">GetWindowTextLength  function</a>
        /// Retrieves the length, in characters, of the specified window's title bar text (if the window has a title bar). If the specified window is a control, the function retrieves the length of the text within the control. However, GetWindowTextLength cannot retrieve the length of the text of an edit control in another application.
        /// </summary>
        /// <param name="hWnd">A handle to the window or control.</param>
        /// <returns>
        /// If the function succeeds, the return value is the length, in characters, of the text. Under certain conditions, this value may actually be greater than the length of the text. For more information, see the following Remarks section.
        /// If the window has no text, the return value is zero. To get extended error information, call GetLastError.
        /// </returns>
        [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport(User32, SetLastError = true)]
        public static extern uint GetSysColor(int nIndex);

        /// <summary>
        ///     Bring the specified window to the front
        /// </summary>
        /// <param name="hWnd">IntPtr specifying the hWnd</param>
        /// <returns>true if the call was successfull</returns>
        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>
        ///     Get the hWnd of the Desktop window
        /// </summary>
        /// <returns>IntPtr</returns>
        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr GetDesktopWindow();

        /// <summary>
        ///     Set the current foreground window
        /// </summary>
        /// <param name="hWnd">IntPtr with the handle to the window</param>
        /// <returns>bool</returns>
        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        ///     Sets the keyboard focus to the specified window. The window must be attached to the calling thread's message queue.
        /// </summary>
        /// <param name="hWnd">
        ///     A handle to the window that will receive the keyboard input. If this parameter is NULL, keystrokes
        ///     are ignored.
        /// </param>
        /// <returns>
        ///     IntPtr
        ///     If the function succeeds, the return value is the handle to the window that previously had the keyboard focus.
        ///     If the hWnd parameter is invalid or the window is not attached to the calling thread's message queue, the return
        ///     value is NULL.
        ///     To get extended error information, call GetLastError.
        /// </returns>
        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        /// <summary>
        ///     Get the WindowPlacement for the specified window
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <param name="windowPlacement">WindowPlacement</param>
        /// <returns>true if success</returns>
        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement windowPlacement);

        /// <summary>
        ///     Set the WindowPlacement for the specified window
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <param name="windowPlacement">WindowPlacement</param>
        /// <returns>true if the call was sucessfull</returns>
        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WindowPlacement windowPlacement);

        /// <summary>
        ///     Return true if the specified window is minimized
        /// </summary>
        /// <param name="hWnd">IntPtr for the hWnd</param>
        /// <returns>true if minimized</returns>
        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr hWnd);

        /// <summary>
        ///     Return true if the specified window is maximized
        /// </summary>
        /// <param name="hwnd">IntPtr for the hWnd</param>
        /// <returns>true if maximized</returns>
        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsZoomed(IntPtr hwnd);

        /// <summary>
        ///     Get the classname of the specified window
        /// </summary>
        /// <param name="hWnd">IntPtr with the hWnd</param>
        /// <param name="className">StringBuilder to place the classname into</param>
        /// <param name="nMaxCount">max size for the string builder length</param>
        /// <returns>nr of characters returned</returns>
        [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder className, int nMaxCount);

        [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr GetClassLong(IntPtr hWnd, ClassLongIndex index);

        [DllImport(User32, SetLastError = true, EntryPoint = "GetClassLongPtr")]
        private static extern IntPtr GetClassLongPtr(IntPtr hWnd, ClassLongIndex index);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd162869(v=vs.85).aspx">PrintWindow function</a>
        /// </summary>
        /// <param name="hwnd">IntPtr</param>
        /// <param name="hDc">IntPtr</param>
        /// <param name="printWindowFlags">PrintWindowFlags</param>
        /// <returns>bool</returns>
        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PrintWindow(IntPtr hwnd, IntPtr hDc, PrintWindowFlags printWindowFlags);

        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, WindowsMessages windowsMessage, SysCommands sysCommand, IntPtr lParam);

        /// <summary>
        ///     Used for WM_VSCROLL and WM_HSCROLL
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <param name="windowsMessage">WindowsMessages</param>
        /// <param name="scrollBarCommand">ScrollBarCommands</param>
        /// <param name="lParam"></param>
        /// <returns>0</returns>
        [DllImport(User32, SetLastError = true)]
        public static extern int SendMessage(IntPtr hWnd, WindowsMessages windowsMessage, ScrollBarCommands scrollBarCommand, int lParam);

        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, WindowsMessages windowsMessage, IntPtr wParam, IntPtr lParam);

        /// <summary>
        ///     Used for calls where the arguments are int
        /// </summary>
        /// <param name="hWnd">IntPtr for the Window handle</param>
        /// <param name="windowsMessage">WindowsMessages</param>
        /// <param name="wParam">int</param>
        /// <param name="lParam">int</param>
        /// <returns></returns>
        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, WindowsMessages windowsMessage, int wParam, int lParam);

        /// <summary>
        ///     SendMessage for getting TitleBarInfoEx
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="windowsMessage"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam">TitleBarInfoEx</param>
        /// <returns>LResut which is an IntPtr</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr SendMessage(IntPtr hWnd, WindowsMessages windowsMessage, IntPtr wParam, ref TitleBarInfoEx lParam);

        /// <summary>
        ///     Used for WM_GETTEXT
        /// </summary>
        /// <param name="hWnd">IntPtr for the Window handle</param>
        /// <param name="windowsMessage"></param>
        /// <param name="wParam">int with the capacity of the string builder</param>
        /// <param name="lParam">StringBuilder</param>
        /// <returns></returns>
        [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, WindowsMessages windowsMessage, int wParam, StringBuilder lParam);

        [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, WindowsMessages windowsMessage, IntPtr wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        [DllImport(User32, SetLastError = true, EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong(IntPtr hwnd, WindowLongIndex index);

        [DllImport(User32, SetLastError = true, EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, WindowLongIndex nIndex);

        [DllImport(User32, SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, WindowLongIndex index, int styleFlags);

        [DllImport(User32, SetLastError = true, EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, WindowLongIndex index, IntPtr styleFlags);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd145064(v=vs.85).aspx">
        ///         MonitorFromWindow
        ///         function
        ///     </a>
        ///     The MonitorFromWindow function retrieves a handle to the display monitor that has the largest area of intersection
        ///     with the bounding rectangle of a specified window.
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="monitorFrom">MonitorFromFlags</param>
        /// <returns>IntPtr for the monitor</returns>
        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, MonitorFrom monitorFrom);

        /// <summary>
        ///     The MonitorFromRect function retrieves a handle to the display monitor that has the largest area of intersection
        ///     with a specified rectangle.
        ///     see
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd145063(v=vs.85).aspx">MonitorFromRect function</a>
        /// </summary>
        /// <param name="rect">A RECT structure that specifies the rectangle of interest in virtual-screen coordinates.</param>
        /// <param name="monitorFrom">MonitorFromRectFlags</param>
        /// <returns>HMONITOR handle</returns>
        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr MonitorFromRect([In] ref NativeRect rect, MonitorFrom monitorFrom);

        /// <summary>
        ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633516(v=vs.85).aspx">GetWindowInfo</a>
        ///     Retrieves information about the specified window.
        /// </summary>
        /// <param name="hwnd">IntPtr</param>
        /// <param name="windowInfo">WindowInfo (use WindowInfo.Create)</param>
        /// <returns>bool if false than get the last error</returns>
        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowInfo(IntPtr hwnd, ref WindowInfo windowInfo);

        /// <summary>
        ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633497(v=vs.85).aspx">here</a>
        /// </summary>
        /// <param name="enumFunc">EnumWindowsProc</param>
        /// <param name="param">An application-defined value to be passed to the callback function.</param>
        /// <returns>true if success</returns>
        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc enumFunc, IntPtr param);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633495(v=vs.85).aspx">
        ///         EnumThreadWindows
        ///         function
        ///     </a>
        ///     Enumerates all nonchild windows associated with a thread by passing the handle to each window, in turn, to an
        ///     application-defined callback function.
        ///     EnumThreadWindows continues until the last window is enumerated or the callback function returns FALSE.
        ///     To enumerate child windows of a particular window, use the EnumChildWindows function.
        /// </summary>
        /// <param name="threadId">The identifier of the thread whose windows are to be enumerated.</param>
        /// <param name="enumFunc">EnumWindowsProc</param>
        /// <param name="param">An application-defined value to be passed to the callback function.</param>
        /// <returns></returns>
        [DllImport(User32, SetLastError = true)]
        public static extern bool EnumThreadWindows(int threadId, EnumWindowsProc enumFunc, IntPtr param);

        /// <summary>
        ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633497(v=vs.85).aspx">here</a>
        /// </summary>
        /// <param name="hWndParent">IntPtr with hwnd of parent window, if this is IntPtr.Zero this function behaves as EnumWindows</param>
        /// <param name="enumFunc">EnumWindowsProc</param>
        /// <param name="param">An application-defined value to be passed to the callback function.</param>
        /// <returns>true if success</returns>
        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc enumFunc, IntPtr param);

        /// <summary>
        ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb787583(v=vs.85).aspx">GetScrollInfo</a> for
        ///     more information.
        /// </summary>
        /// <param name="hwnd">IntPtr with the window handle</param>
        /// <param name="scrollBar">ScrollBarTypes</param>
        /// <param name="scrollInfo">ScrollInfo ref</param>
        /// <returns>bool if it worked</returns>
        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetScrollInfo(IntPtr hwnd, ScrollBarTypes scrollBar, ref ScrollInfo scrollInfo);

        /// <summary>
        ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb787595(v=vs.85).aspx">SetScrollInfo</a> for
        ///     more information.
        /// </summary>
        /// <param name="hwnd">IntPtr with the window handle</param>
        /// <param name="scrollBar">ScrollBarTypes</param>
        /// <param name="scrollInfo">ScrollInfo ref</param>
        /// <param name="redraw">bool to specify if a redraw should be made</param>
        /// <returns>bool if it worked</returns>
        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetScrollInfo(IntPtr hwnd, ScrollBarTypes scrollBar, ref ScrollInfo scrollInfo, bool redraw);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb787601(v=vs.85).aspx">ShowScrollBar function</a>
        ///     for more information.
        /// </summary>
        /// <param name="hwnd">IntPtr</param>
        /// <param name="scrollBar">ScrollBarTypes</param>
        /// <param name="show">true to show, false to hide</param>
        /// <returns>
        ///     If the function succeeds, the return value is nonzero.
        ///     If the function fails, the return value is zero. To get extended error information, call GetLastError.
        /// </returns>
        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowScrollBar(IntPtr hwnd, ScrollBarTypes scrollBar, [MarshalAs(UnmanagedType.Bool)] bool show);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/bb787581(v=vs.85).aspx">GetScrollBarInfo function</a>
        ///     for more information.
        /// </summary>
        /// <param name="hwnd">
        ///     Handle to a window associated with the scroll bar whose information is to be retrieved. If the
        ///     idObject parameter is OBJID_CLIENT, hwnd is a handle to a scroll bar control. Otherwise, hwnd is a handle to a
        ///     window created with WS_VSCROLL and/or WS_HSCROLL style.
        /// </param>
        /// <param name="idObject">
        ///     Specifies the scroll bar object. Can be ObjectIdentifiers.Client,
        ///     ObjectIdentifiers.HorizontalScrollbar, ObjectIdentifiers.VerticalScrollbar
        /// </param>
        /// <param name="scrollBarInfo">ScrollBarInfo ref</param>
        /// <returns>bool</returns>
        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetScrollBarInfo(IntPtr hwnd, ObjectIdentifiers idObject, ref ScrollBarInfo scrollBarInfo);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd144950(v=vs.85).aspx">GetWindowRgn function</a>
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <param name="hRgn">SafeHandle</param>
        /// <returns>RegionResults</returns>
        [DllImport(User32, SetLastError = true)]
        public static extern RegionResults GetWindowRgn(IntPtr hWnd, SafeHandle hRgn);

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, WindowPos uFlags);

        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr GetTopWindow(IntPtr hWnd);

        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr GetClipboardOwner();

        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        /// uiFlags: 0 - Count of GDI objects
        /// uiFlags: 1 - Count of USER objects
        /// - Win32 GDI objects (pens, brushes, fonts, palettes, regions, device contexts, bitmap headers)
        /// - Win32 USER objects:
        /// - 	WIN32 resources (accelerator tables, bitmap resources, dialog box templates, font resources, menu resources, raw data resources, string table entries, message table entries, cursors/icons)
        /// - Other USER objects (windows, menus)
        [DllImport(User32, SetLastError = true)]
        public static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);

        [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, SendMessageTimeoutFlags fuFlags, uint uTimeout, out UIntPtr lpdwResult);

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetPhysicalCursorPos(out NativePoint cursorLocation);

        [DllImport(User32, SetLastError = true)]
        public static extern int MapWindowPoints(IntPtr hwndFrom, IntPtr hwndTo, ref NativePoint lpPoints, [MarshalAs(UnmanagedType.U4)] int cPoints);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724385(v=vs.85).aspx">GetSystemMetrics function</a>
        /// </summary>
        /// <param name="index">SystemMetric</param>
        /// <returns>int</returns>
        [DllImport(User32, SetLastError = true)]
        public static extern int GetSystemMetrics(SystemMetric index);

        /// <summary>
        ///     The following is used for Icon handling, and copies a hicon to a new
        /// </summary>
        /// <param name="hIcon">IntPtr</param>
        /// <returns>SafeIconHandle</returns>
        [DllImport(User32, SetLastError = true)]
        public static extern SafeIconHandle CopyIcon(IntPtr hIcon);

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms648389(v=vs.85).aspx">GetCursorInfo function</a>
        ///     Retrieves information about the global cursor.
        /// </summary>
        /// <param name="cursorInfo">a CURSORINFO structure</param>
        /// <returns>bool</returns>
        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorInfo(out CursorInfo cursorInfo);

        [DllImport(User32, SetLastError = true)]
        public static extern bool GetIconInfo(SafeIconHandle iconHandle, out IconInfo iconInfo);

        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReleaseCapture();

        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr CreateIconIndirect(ref IconInfo icon);

        [DllImport(User32, SetLastError = true)]
        internal static extern IntPtr OpenInputDesktop(uint dwFlags, [MarshalAs(UnmanagedType.Bool)] bool fInherit, DesktopAccessRight dwDesiredAccess);

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetThreadDesktop(IntPtr hDesktop);

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseDesktop(IntPtr hDesktop);

        [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724947(v=vs.85).aspx">
        ///         SystemParametersInfo
        ///         function
        ///     </a>
        ///     For setting an IntPtr
        /// </summary>
        /// <param name="uiAction">SystemParametersInfoActions</param>
        /// <param name="uiParam">
        ///     A parameter whose usage and format depends on the system parameter being queried or set. For more
        ///     information about system-wide parameters, see the uiAction parameter. If not otherwise indicated, you must specify
        ///     zero for this parameter.
        /// </param>
        /// <param name="pvParam">IntPtr</param>
        /// <param name="fWinIni">SystemParametersInfoBehaviors</param>
        /// <returns>bool</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SystemParametersInfo(SystemParametersInfoActions uiAction, uint uiParam, IntPtr pvParam, SystemParametersInfoBehaviors fWinIni);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724947(v=vs.85).aspx">
        ///         SystemParametersInfo
        ///         function
        ///     </a>
        ///     For setting a string parameter
        /// </summary>
        /// <param name="uiAction">SystemParametersInfoActions</param>
        /// <param name="uiParam">
        ///     A parameter whose usage and format depends on the system parameter being queried or set. For more
        ///     information about system-wide parameters, see the uiAction parameter. If not otherwise indicated, you must specify
        ///     zero for this parameter.
        /// </param>
        /// <param name="pvParam">string</param>
        /// <param name="fWinIni">SystemParametersInfoBehaviors</param>
        /// <returns>bool</returns>
        [DllImport(User32, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SystemParametersInfo(SystemParametersInfoActions uiAction, uint uiParam, string pvParam, SystemParametersInfoBehaviors fWinIni);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724947(v=vs.85).aspx">
        ///         SystemParametersInfo
        ///         function
        ///     </a>
        ///     For reading a string parameter
        /// </summary>
        /// <param name="uiAction">SystemParametersInfoActions</param>
        /// <param name="uiParam">
        ///     A parameter whose usage and format depends on the system parameter being queried or set. For more
        ///     information about system-wide parameters, see the uiAction parameter. If not otherwise indicated, you must specify
        ///     zero for this parameter.
        /// </param>
        /// <param name="pvParam">string</param>
        /// <param name="fWinIni">SystemParametersInfoBehaviors</param>
        /// <returns>bool</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SystemParametersInfo(SystemParametersInfoActions uiAction, uint uiParam, StringBuilder pvParam, SystemParametersInfoBehaviors fWinIni);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724947(v=vs.85).aspx">
        ///         SystemParametersInfo
        ///         function
        ///     </a>
        ///     For setting AnimationInfo
        /// </summary>
        /// <param name="uiAction">SystemParametersInfoActions</param>
        /// <param name="uiParam">
        ///     A parameter whose usage and format depends on the system parameter being queried or set. For more
        ///     information about system-wide parameters, see the uiAction parameter. If not otherwise indicated, you must specify
        ///     zero for this parameter.
        /// </param>
        /// <param name="animationInfo">AnimationInfo</param>
        /// <param name="fWinIni">SystemParametersInfoBehaviors</param>
        /// <returns>bool</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SystemParametersInfo(SystemParametersInfoActions uiAction, uint uiParam, ref AnimationInfo animationInfo, SystemParametersInfoBehaviors fWinIni);

        #endregion
    }
}