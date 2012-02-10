#1. Name
XAPspy - runtime analysis of windows phone 7 applications
#2. Author
Behrang Fouladi < behrang(at)sensepost(dot)com >
#3. License, version & release date
License : GPLv3  
Version : v1.0  
Release Date : 2011/09/23

#4. Description
XAPspy is a dynamic analysis tool for windows phone 7 applications similar to droidbox for android devices. It can be used to print out method names and variables to an emulator console during runtime.

#4.1 Problem Statement and Description
Runtime analysis is an integral part of most application security assessment processes. Many powerful tools have been developed to perform execution/data flow analysis and code debugging for desktop and server operating systems. Although a few dynamic analysis tools such as DroidBox are available for Android, I currently know of no similar public tools for the Windows Phone 7 platform. The main challenge for Windows Phone 7 is the lack of a programable debugging interface in both the Emulator and phone devices. The Visual Studio 2010 debugger for Phone applications does not have an "Attach to process" feature and can only be used to debug applications for which the source code is available. Although the Kernel Independent Transport Layer (KITL) can be enabled on some Windows Phone devices at boot time which could be very useful for Kernel and unmanged code debugging, it can't be used directly for code tracing of phone applications which are executed by the .NET compact framework.

The instrumented phone application prints out method names and variables to the emulator console (that can be enabled by adding a registry key) at runtime. The console window buffer is then captured by an API Hook (WriteFile API) in the emulator process and saved to the runtrace file. I have developed a tool named "XAP Spy" in C# to automate the above process.

#5. Requirements
Windows Phone 7 SDK  
.NET framework 4.0 and 2.0 (The API hook code is based on EasyHook library which only works with .NET framework 2.0)
#6. Additional Resources 
Blog, Runtime analysis of Windows Phone 7 Applications - http://www.sensepost.com/blog/6081.html

