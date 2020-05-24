using System;
using NetTopologySuite.Geometries;
using OSMLSGlobalLibrary;
using OSMLSGlobalLibrary.Map;
using OSMLSGlobalLibrary.Modules;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace AirshipSimulation
{
    public static class CoordinateExtension
    {
        public static (double la, double lo) toLatLon(this Coordinate coord)
        {
            var la = yToLat(coord.Y);
            var lo = xToLon(coord.X);
            return (la, lo);
        }

        private static readonly double R_MAJOR = 6378137.0;
        private static readonly double R_MINOR = 6356752.3142;
        private static readonly double RATIO = R_MINOR / R_MAJOR;
        private static readonly double ECCENT = Math.Sqrt(1.0 - (RATIO * RATIO));
        private static readonly double COM = 0.5 * ECCENT;

        private static readonly double RAD2Deg = 180.0 / Math.PI;
        private static readonly double PI_2 = Math.PI / 2.0;

        public static double xToLon(double x)
        {
            return RadToDeg(x) / R_MAJOR;
        }

        public static double yToLat(double y)
        {
            double ts = Math.Exp(-y / R_MAJOR);
            double phi = PI_2 - 2 * Math.Atan(ts);
            double dphi = 1.0;
            int i = 0;
            while ((Math.Abs(dphi) > 0.0000001) && (i < 15))
            {
                double con = ECCENT * Math.Sin(phi);
                dphi = PI_2 - 2 * Math.Atan(ts * Math.Pow((1.0 - con) / (1.0 + con), COM)) - phi;
                phi += dphi;
                i++;
            }

            return RadToDeg(phi);
        }

        private static double RadToDeg(double rad)
        {
            return rad * RAD2Deg;
        }
    }

    static class Rand
    {
        private static Random rand = new Random();

        public static int GenerateInRange(int min, int max) =>
            (int) Math.Round(min - 0.5 + rand.NextDouble() * (max - min + 1));

        public static Coordinate GenerateNext((int leftX, int rightX, int downY, int upY) map) =>
            new Coordinate(GenerateInRange(map.leftX, map.rightX), GenerateInRange(map.downY, map.upY));
        
        public static T RandomElement<T>(this ICollection<T> q)
        {
            return q.Skip(rand.Next(q.Count())).FirstOrDefault();
        }

        public static bool MayBe()
        {
            return rand.Next(0, 2) == 0;
        }
    }


    public class WindMap
    {
        private readonly Dictionary<(int, int), Coordinate> _coordinatesToWindVec;
        public WindMap()
        {
            _coordinatesToWindVec = new Dictionary<(int, int), Coordinate>();
        }

        public void Insert(int x, int y, double U, double V)
        {
            _coordinatesToWindVec[(y, x)] = new Coordinate(Convert.ToInt32(U), Convert.ToInt32(V));
        }

        public Coordinate GetWindDirection(Coordinate position)
        {
            var (la, lo) = position.toLatLon();
            var y = (int)Math.Round(la);
            var x = (int)Math.Abs(Math.Round(lo));
            
            return _coordinatesToWindVec[(y, x)];
        }
    }

    public class Module : OSMLSModule
    {
        private WindMap _windMap;
        private readonly (int, int, int, int) _allMap = (0, 18628621 * 2, -15000000, 15000000);
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
                    double.TryParse(s.Replace('.', ','), out var d);
                    return d;
                }).ToArray();
                double la = data[2], lo = data[3], U = data[0], V = data[1];
                _windMap.Insert((int)lo, (int)la, U*100, V*100);
            }

            MapObjects.Add(new Aerostat(new Coordinate(4940278, 6233593),  _windMap));
        }
        
        public override void Update(long elapsedMilliseconds)
        {
            foreach (var aerostat in MapObjects.GetAll<Aerostat>())
            {
                aerostat.Move();
            }

            if (Rand.MayBe())
            {
                var coord = Rand.GenerateNext(_allMap);
                MapObjects.Add(new Aerostat(coord,  _windMap));
                Console.WriteLine("На координатах {0}, {1} был запущен аэростат", coord.X, coord.Y);
            }
            else
            {
                foreach (var a in 
                    MapObjects.GetAll<Aerostat>().Where(aerostat => aerostat.Declining())
                    )
                {
                    MapObjects.Remove(a);
                    var coord = a.Coordinate;
                    Console.WriteLine("На координатах {0}, {1} приземлился аэростат", coord.X, coord.Y);
                }
            }
        }
    }

    public static class PointExtension
    {
        public static double distance(this Point p1, Point p2) => Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        public static double distance(this Point p1, Coordinate p2) => Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));

        public static void Move(this Point p, Coordinate direction)
        {
            p.X += direction.X;
            p.Y += direction.Y;
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
        private readonly WindMap _windMap;
        private int _countSteps = 500;
        public Aerostat(Coordinate coordinate, WindMap windMap) : base(coordinate)
        {
            _windMap = windMap;
        }

        public void Move()
        {
            _countSteps = Math.Max(0, _countSteps - 1);
            var direction = _windMap.GetWindDirection(this.Coordinate);
            
            this.Move(direction);
        }

        public bool Declining()
        {
            return _countSteps == 0;
        } 
    }


}