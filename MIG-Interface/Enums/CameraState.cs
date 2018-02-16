// <copyright file="CameraState.cs" company="Wallis2000.co.uk">
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
// </copyright>

namespace MIG.Interface.Enums
{
    /// <summary>
    /// Camera States
    /// </summary>
    public enum CameraState
    {
        #pragma warning disable 1591
        Start,
        Connect,
        Send,
        Receive,
        Wait
        #pragma warning restore 1591
    }
}
