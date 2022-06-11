using GameOverlay.Drawing;
using MapAssist.Settings;
using System;
using System.Linq;

namespace MapAssist.Types
{
    public class PointOfInterest
    {
        public string Label;
        public Area Area;
        public Area NextArea;
        public Point Position;
        public PointOfInterestRendering RenderingSettings;
        public PoiType Type;

        public bool PoiMatchesPortal(UnitObject[] gameDataObjectList, Difficulty difficulty)
        {
            return Type == PoiType.AreaPortal && gameDataObjectList.Any(x => x.IsPortal && AreaExtensions.ToArea(x.ObjectData.InteractType).PortalLabel(difficulty) == Label);
        }
    }

    public enum PoiType
    {
        NextArea,
        PreviousArea,
        Waypoint,
        Quest,
        AreaSpecificQuest,
        AreaPortal,
        Shrine,
        SuperChest,
        ArmorWeapRack,
        Door
    }
}
