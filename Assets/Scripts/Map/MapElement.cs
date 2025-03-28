using System;

[Serializable]
public class MapElement
{
    // For node, way and relation
    public string type;
    public Tags tags;

    // For node
    public double lat;
    public double lon;

    // For way
    public Coords[] geometry;

    // For relation
    public RelationMember[] members;

    [Serializable]
    public class Coords
    {
        public double lat;
        public double lon;
    }

    [Serializable]
    public class Tags
    {
        public string building;
        public string buildingLevels;
        public string highway;
        public string landuse;
        public string natural;
        public string leisure;
        public string barrier;
    }

    [Serializable]
    public class RelationMember
    {
        public string type;
        public string role;
        public Coords[] geometry;
    }
}
