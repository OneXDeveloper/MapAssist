/**
 *   Copyright (C) 2021 okaygo
 *
 *   https://github.com/misterokaygo/MapAssist/
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <https://www.gnu.org/licenses/>.
 **/

using MapAssist.Helpers;
using MapAssist.Interfaces;
using System;

namespace MapAssist.Types
{
    public class UiSettings : IUpdatable<UiSettings>
    {
        private readonly IntPtr _pUiSettings = IntPtr.Zero;
        private Structs.UiSettings _uiSettings;

        public UiSettings(IntPtr pUiSettings)
        {
            _pUiSettings = pUiSettings;
            Update();
        }

        public UiSettings Update()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                _uiSettings = processContext.Read<Structs.UiSettings>(_pUiSettings);
            }

            return this;
        }

        public bool MapShown => _uiSettings.MapShown == 1;
    }
}
