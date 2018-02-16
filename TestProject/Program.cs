// <copyright file="Program.cs" company="GenieLabs">
// This file is part of HomeGenie-BE Project source code.
//
// HomeGenie-BE is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// HomeGenie-BE is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// You should have received a copy of the GNU General Public License
// along with HomeGenie-BE.  If not, see http://www.gnu.org/licenses.
//
// Author: Generoso Martello gene@HomeGenie-BE.it
// Project Homepage: http://HomeGenie-BE.it
// </copyright>

using System;
using System.IO;
using System.Xml.Serialization;

using MIG;
using MIG.Config;

namespace TestProject
{
    /// <summary>
    /// Main class
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Main Program
        /// </summary>
        /// <param name="args">command line arguments</param>
        public static void Main(string[] args)
        {
            Console.WriteLine("Mig Interface Hikvision test APP");

            var migService = new MigService();

            // Load the configuration from systemconfig.xml file
            MigServiceConfiguration configuration;

            // Construct an instance of the XmlSerializer with the type
            // of object that is being deserialized.
            var mySerializer = new XmlSerializer(typeof(MigServiceConfiguration));

            // To read the file, create a FileStream.
            var myFileStream = new FileStream("systemconfig.xml", FileMode.Open);

            // Call the Deserialize method and cast to the object type.
            configuration = (MigServiceConfiguration)mySerializer.Deserialize(myFileStream);

            // Set the configuration and start MIG Service
            migService.Configuration = configuration;
            migService.StartService();

            // Get a reference to the test interface
            var interfaceDomain = "Hikvision.InterfaceHikvision";
            var migInterface = migService.GetInterface(interfaceDomain);

            // Test an interface API command programmatically <module_domain>/<module_address>/<command>[/<option_0>[/../<option_n>]]
            var response = migInterface.InterfaceControl(new MigInterfaceCommand(interfaceDomain + "/3/Greet.Hello/Username"));

            // <module_domain> ::= "Hikvision.InterfaceHikvision"
            // <module_address> ::= "3"
            // <command> ::= "Greet.Hello"
            // <option_0> ::= "Username"
            // For more infos about MIG API see:
            //    http://genielabs.github.io/HomeGenie-BE/api/mig/overview.html
            //    http://genielabs.github.io/HomeGenie-BE/api/mig/mig_api_interfaces.html

            // The same command can be invoked though the WebGateway
            // http://<server_address>:8080/api/Hikvision.InterfaceHikvision/1/Greet.Hello/Username

            // Test some other interface API command
            response = migInterface.InterfaceControl(new MigInterfaceCommand(interfaceDomain + "/1/Control.On"));
            MigService.Log.Debug(response);
            response = migInterface.InterfaceControl(new MigInterfaceCommand(interfaceDomain + "/1/Control.Off"));
            MigService.Log.Debug(response);
            response = migInterface.InterfaceControl(new MigInterfaceCommand(interfaceDomain + "/2/Temperature.Get"));
            MigService.Log.Debug(response);

            Console.WriteLine("\n[Press Enter to Quit]\n");
            Console.ReadLine();
        }
    }
}
