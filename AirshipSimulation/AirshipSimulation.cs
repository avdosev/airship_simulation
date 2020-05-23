using System;
using NetTopologySuite.Geometries;
using OSMLSGlobalLibrary;
using OSMLSGlobalLibrary.Map;
using OSMLSGlobalLibrary.Modules;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AirshipSimulation
{
    public class WindMap
    {
        private readonly Dictionary<Coordinate, Coordinate> _coordinatesToWindVec;
        public WindMap()
        {
            _coordinatesToWindVec = new Dictionary<Coordinate, Coordinate>();
        }

        public void Insert(Coordinate coord, double U, double V)
        {
            _coordinatesToWindVec[coord] = new Coordinate(U, V);
        }

        public Coordinate GetWindDirection(Coordinate position)
        {
            // Todo: need round position;
            return _coordinatesToWindVec[position];
        }
    }

    public class Module : OSMLSModule
    {
        private WindMap _windMap;
        protected override void Initialize()
        {
            _windMap = new WindMap();
            var csvWindData = File.ReadLines("./current-wind.csv");
            foreach (var line in csvWindData.Skip(1))
            {
                if (line.Length == 0) continue;
                // U,V,la,lo;
                var data = line.Split(',').Select(s =>
                {
                    double.TryParse(s, out var d);
                    return d;
                }).ToArray();
                double la = data[2], lo = data[3], U = data[0], V = data[1];
                var coord = MathExtensions.LatLonToSpherMerc(la, lo);
                _windMap.Insert(coord, U, V);
            }
        }
        
        public override void Update(long elapsedMilliseconds)
        {
            
        }
    }

    public static class PointExtension
    {
        public static double distance(this Point p1, Point p2) => Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        public static double distance(this Point p1, Coordinate p2) => Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));

        public static void Move(this Point p, Coordinate direction, double speed)
        {
            double MinimumDirection(double s, double d) =>
                Math.Min(speed, Math.Abs(s - d)) * Math.Sign(d - s);
            
            p.X += MinimumDirection(p.X, direction.X);
            p.Y += MinimumDirection(p.Y, direction.Y);
        }
    }


    [CustomStyle(
        @"new ol.style.Style({
            image: new ol.style.Circle({
                opacity: 1.0,
                scale: 1.0,
                radius: 3,
                fill: new ol.style.Fill({
                    color: 'rgba(224, 12, 30, 0.9)'
                }),
                stroke: new ol.style.Stroke({
                    color: 'rgba(0, 0, 0, 0.4)',
                    width: 1
                }),
            })
        });
        ")]
    class Aerostat : Point 
    {
        private Coordinate destinationPoint = null;
        public Aerostat(Coordinate coordinate) : base(coordinate)
        {
        }

        public void Move()
        {
            var speed = .0;
            this.Move(destinationPoint, speed);
        }
    }


}