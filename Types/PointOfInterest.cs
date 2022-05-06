using GameOverlay.Drawing;
using MapAssist.Settings;
using System;

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
            if (Type == PoiType.AreaPortal)
            {
                foreach (var gameObject in gameDataObjectList)
                {
                    if (gameObject.IsPortal)
                    {
                        var area = (Area)Enum.ToObject(typeof(Area), gameObject.ObjectData.InteractType);
                        if (area.PortalLabel(difficulty) == Label)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
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
