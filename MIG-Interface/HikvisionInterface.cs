// <copyright file="InterfaceHikvision.cs" company="Bounz">
// This file is part of HomeGenie-BE Project source code.
//
// HomeGenie-BE is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// HomeGenie is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// You should have received a copy of the GNU General Public License
// along with HomeGenie-BE.  If not, see http://www.gnu.org/licenses.
//
//  Project Homepage: https://github.com/Bounz/HomeGenie-BE
//
//  Forked from Homegenie by Generoso Martello gene@homegenie.it
// </copyright>

/* Namespaces must begin MIG.Interfaces for MIG to be able to load it
* Hikvision would be come the interface domain, and the class name the interface name
* So in this code Hikvision.InterfaceHikvision is the interface name in MIG.
*/
namespace MIG.Interfaces.HomeAutomation
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using MIG.Config;
    using MIG.Interface;
    using NLog;

    /// <summary>
    /// InterfaceHikvision class
    /// </summary>
    public class Hikvision : MigInterface
    {
        /// <summary>
        /// Logger
        /// </summary>
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// List containing interface Modules
        /// </summary>
        private readonly List<InterfaceModule> modules;

        /// <summary>
        /// Initializes a new instance of the <see cref="Hikvision"/> class.
        /// </summary>
        public Hikvision()
        {
            // Creating modules would normally be done in either the connect method or within the GetModules() Method
            // See https://github.com/Bounz/HomeGenie-BE/blob/master/MIG/MIG.HomeAutomation/ZWave.cs for an Hikvision
            // THe default constuctor can then be removed.
            this.modules = new List<InterfaceModule>();

            // manually add some fake modules
            var module_1 = new InterfaceModule
            {
                Domain = this.GetDomain(),
                Address = "1",
                ModuleType = ModuleTypes.Light
            };

            var module_2 = new InterfaceModule
            {
                Domain = this.GetDomain(),
                Address = "2",
                ModuleType = ModuleTypes.Sensor
            };

            var module_3 = new InterfaceModule
            {
                Domain = this.GetDomain(),
                Address = "3",
                ModuleType = ModuleTypes.Sensor
            };

            // add them to the modules list
            this.modules.Add(module_1);
            this.modules.Add(module_2);
            this.modules.Add(module_3);
        }

        /// <summary>
        /// Event for Interface Modules Changing
        /// </summary>
        public event InterfaceModulesChangedEventHandler InterfaceModulesChanged;

        /// <summary>
        /// Event for Interface Properties Changing
        /// </summary>
        public event InterfacePropertyChangedEventHandler InterfacePropertyChanged;

        /// <summary>
        /// Gets or sets a value indicating whether this interface is enabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a the interface options
        /// </summary>
        public List<Option> Options { get; set; }

        /// <summary>
        /// Gets a value indicating whether the interface/controller device is connected or not.
        /// </summary>
        /// <value>
        /// <c>true</c> if it is connected; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Called when disposing the Mig Interface
        /// </summary>
        public void Dispose()
        {
            this.Disconnect();
        }

        /// <summary>
        /// Called when setting an interface option?
        /// </summary>
        /// <param name="option">The interface option</param>
        public void OnSetOption(Option option)
        {
            if (this.IsEnabled)
            {
                this.Connect();
            }
        }

        /// <summary>
        /// Gets the interface modules
        /// </summary>
        /// <returns>A list containing the interfaces modules</returns>
        public List<InterfaceModule> GetModules()
        {
            return this.modules;
        }

        /// <summary>
        /// Returns true if the device has been found in the system
        /// </summary>
        /// <returns>Boolean value indicating if the device is present</returns>
        public bool IsDevicePresent()
        {
            return true;
        }

        /// <summary>
        /// Sample Connect Method
        /// </summary>
        /// <returns>boolean value indicating if the connection was successful</returns>
        public bool Connect()
        {
            if (!this.IsConnected)
            {
                // Log that the interface is loading and display the version being loaded
                Log.Info("Starting {0}, Version {1}", this.GetDomain(), Assembly.GetExecutingAssembly().GetName().Version);


                // TODO: If this option isnt in the configuration file then a terminating error will be thrown.
                // TODO: Create static helper function to handle errors and logging.
                // Options can be retrieved by:
                if (string.IsNullOrEmpty(this.GetOption("MyOption1").Value))
                {
                    Log.Error("MyOption1 not configured");
                }

                if (string.IsNullOrEmpty(this.GetOption("MySecureOption1").Value))
                {
                    Log.Error("MySecureOption1 not configured");
                }

                var deviceAddress = this.GetOption("MyOption1").Value;
                Log.Info($"Connecting to {deviceAddress}");

                // Perform Connection

                // Sample Code Here....

                // End Performing Connection
                Log.Info($"{this.GetDomain()} Connected");
                this.IsConnected = true;
            }

            this.OnInterfaceModulesChanged(this.GetDomain());
            return true;
        }

        /// <summary>
        /// Sample Disconnect Method
        /// </summary>
        public void Disconnect()
        {
            if (this.IsConnected)
            {
                // TODO: Perform a disconnection from your 'hardware' here
                this.IsConnected = false;
            }
        }

        /// <summary>
        /// Handles the control of the interface from the Mig / HG web interface
        /// </summary>
        /// <param name="request">request body</param>
        /// <returns>object</returns>
        /// <exception cref="ArgumentOutOfRangeException">Argument out of range</exception>
        public object InterfaceControl(MigInterfaceCommand request)
        {
            var response = new ResponseText("OK"); // default success value

            Commands command;
            Enum.TryParse<Commands>(request.Command.Replace(".", "_"), out command);

            var module = this.modules.Find(m => m.Address.Equals(request.Address));

            if (module != null)
            {
                switch (command)
                {
                    case Commands.Control_On:
                        this.OnInterfacePropertyChanged(this.GetDomain(), request.Address, "Test Interface", "Status.Level", 1);
                        break;
                    case Commands.Control_Off:
                        this.OnInterfacePropertyChanged(this.GetDomain(), request.Address, "Test Interface", "Status.Level", 0);
                        break;
                    case Commands.Temperature_Get:
                        this.OnInterfacePropertyChanged(this.GetDomain(), request.Address, "Test Interface", "Sensor.Temperature", 19.75);
                        break;
                    case Commands.Greet_Hello:
                        this.OnInterfacePropertyChanged(this.GetDomain(), request.Address, "Test Interface", "Sensor.Message", string.Format("Hello {0}", request.GetOption(0)));
                        response = new ResponseText("Hello World!");
                        break;
                    case Commands.NotSet:
                        break;
                    default:
                        Log.Error(new ArgumentOutOfRangeException(), "Command [{0}] not recognised", command);
                        break;
                }
            }
            else
            {
                response = new ResponseText("ERROR: invalid module address");
            }

            return response;
        }

        /// <summary>
        /// Called when the interface mdoules have changed
        /// </summary>
        /// <param name="domain">The domain</param>
        protected virtual void OnInterfaceModulesChanged(string domain)
        {
            if (this.InterfaceModulesChanged != null)
            {
                var args = new InterfaceModulesChangedEventArgs(domain);
                this.InterfaceModulesChanged(this, args);
            }
        }

        /// <summary>
        /// Call this when an interface property has changed to notify MIG
        /// </summary>
        /// <param name="domain">Domain</param>
        /// <param name="source">Source</param>
        /// <param name="description">Description</param>
        /// <param name="propertyPath">Property Path</param>
        /// <param name="propertyValue">Property Value</param>
        protected virtual void OnInterfacePropertyChanged(string domain, string source, string description, string propertyPath, object propertyValue)
        {
            if (this.InterfacePropertyChanged != null)
            {
                var args = new InterfacePropertyChangedEventArgs(domain, source, description, propertyPath, propertyValue);
                this.InterfacePropertyChanged(this, args);
            }
        }
    }
}
