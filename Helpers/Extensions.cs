/**
 *   Copyright (C) 2021 okaygo, OneXDeveloper
 *
 *   https://github.com/misterokaygo/D2RAssist/
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
using static D2RAssist.Types.Game;

namespace D2RAssist.Helpers
{
    public static class Extensions
    {
        public static bool IsTown(this Area area) =>
            area == Area.RogueEncampment || area == Area.LutGholein || area == Area.KurastDocks ||
            area == Area.ThePandemoniumFortress || area == Area.Harrogath;
    }
}
