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
        public static bool MayBe(double p = 0.5)
        {
            return rand.NextDouble() <= p;
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
        private List<(double, double)> _cities;
        private readonly (int, int, int, int) _allMap = (0, 18628621 * 2, -15000000, 15000000);
        private int WeatherBalloonCounter, DirigibleCounter;

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
                _windMap.Insert((int) lo, (int) la, U * 100, V * 100);
            }

            _cities = new List<(double, double)>
            {
                (24.47, 54.37),
                (9.06, 7.5),
                (9.02, 38.75),
                (5.56, -0.2),
                (36.73, 3.09),
                (31.96, 35.95),
                (52.37, 4.89),
                (42.51, 1.52),
                (39.92, 32.85),
                (-18.91, 47.54),
                (-13.83, -171.77),
                (15.34, 38.93),
                (51.18, 71.45),
                (-25.29, -57.65),
                (37.98, 23.73),
                (37.95, 58.38),
                (33.34, 44.4),
                (40.38, 49.89),
                (12.65, -8),
                (4.36, 18.56),
                (13.75, 100.5),
                (4.89, 114.94),
                (13.45, -16.58),
                (17.3, -62.73),
                (33.89, 35.5),
                (44.8, 20.47),
                (17.25, -88.77),
                (52.52, 13.41),
                (46.95, 7.45),
                (11.86, -15.6),
                (42.87, 74.59),
                (4.61, -74.08),
                (-4.27, 15.28),
                (-15.78, -47.93),
                (48.15, 17.11),
                (13.11, -59.62),
                (50.85, 4.35),
                (47.5, 19.04),
                (-3.38, 29.36),
                (44.43, 26.11),
                (-34.61, -58.38),
                (47.14, 9.52),
                (35.9, 14.51),
                (52.23, 21.01),
                (41.9, 12.45),
                (38.9, -77.04),
                (-41.29, 174.78),
                (48.21, 16.37),
                (-4.62, 55.46),
                (54.69, 25.28),
                (-22.56, 17.08),
                (17.97, 102.6),
                (-24.65, 25.91),
                (23.13, -82.38),
                (14.64, -90.51),
                (14.69, -17.44),
                (23.71, 90.41),
                (33.51, 36.29),
                (-6.21, 106.85),
                (11.59, 43.15),
                (6.8, -58.16),
                (4.85, 31.58),
                (-8.56, 125.57),
                (-6.17, 35.74),
                (25.29, 51.53),
                (53.33, -6.25),
                (38.54, 68.78),
                (40.18, 44.51),
                (45.81, 15.98),
                (33.72, 73.04),
                (34.53, 69.17),
                (30.06, 31.25),
                (0.32, 32.58),
                (-35.28, 149.13),
                (10.49, -66.88),
                (14, -61.01),
                (27.7, 85.32),
                (-1.95, 30.06),
                (50.45, 30.52),
                (13.16, -61.23),
                (18, -76.79),
                (-4.33, 15.31),
                (-0.23, -78.52),
                (47.01, 28.86),
                (6.93, 79.85),
                (9.54, -13.68),
                (55.68, 12.57),
                (3.14, 101.69),
                (0.39, 9.45),
                (-13.97, 33.79),
                (-12.04, -77.03),
                (38.72, -9.13),
                (6.13, 1.22),
                (51.51, -0.13),
                (-8.84, 13.23),
                (-15.41, 28.29),
                (46.05, 14.51),
                (49.61, 6.13),
                (7.09, 171.38),
                (40.42, -3.7),
                (3.76, 8.78),
                (4.18, 73.51),
                (12.13, -86.25),
                (26.23, 50.59),
                (14.6, 120.98),
                (-25.97, 32.58),
                (-29.32, 27.48),
                (23.58, 58.41),
                (-26.32, 31.13),
                (19.43, -99.13),
                (53.9, 27.57),
                (2.04, 45.34),
                (43.73, 7.42),
                (6.3, -10.8),
                (-34.9, -56.19),
                (-11.7, 43.26),
                (55.75, 37.62),
                (-1.28, 36.82),
                (25.06, -77.34),
                (12.11, 15.04),
                (13.51, 2.11),
                (35.18, 33.36),
                (18.09, -15.98),
                (-21.14, -175.2),
                (28.64, 77.22),
                (59.91, 10.75),
                (45.41, -75.7),
                (6.92, 158.16),
                (8.99, -79.52),
                (5.87, -55.17),
                (48.85, 2.35),
                (39.91, 116.4),
                (11.56, 104.92),
                (42.44, 19.26),
                (-17.74, 168.31),
                (-20.16, 57.5),
                (-9.48, 147.15),
                (18.54, -72.34),
                (10.67, -61.52),
                (6.5, 2.6),
                (50.09, 14.42),
                (14.93, -23.51),
                (-25.74, 28.19),
                (42.67, 21.17),
                (39.03, 125.75),
                (34.01, -6.83),
                (64.14, -21.9),
                (56.95, 24.11),
                (41.89, 12.51),
                (15.3, -61.39),
                (43.94, 12.45),
                (13.69, -89.19),
                (0.34, 6.73),
                (9.93, -84.08),
                (15.35, 44.21),
                (18.47, -69.89),
                (-33.46, -70.65),
                (43.85, 18.36),
                (17.12, -61.84),
                (12.05, -61.75),
                (37.57, 126.98),
                (1.29, 103.85),
                (42, 21.43),
                (42.7, 23.32),
                (59.33, 18.06),
                (-18.14, 178.44),
                (-19.03, -65.26),
                (25.05, 121.53),
                (59.44, 24.75),
                (41.26, 69.22),
                (41.69, 44.83),
                (35.69, 51.42),
                (14.08, -87.21),
                (41.33, 19.82),
                (35.69, 139.69),
                (32.89, 13.19),
                (36.82, 10.17),
                (27.47, 89.64),
                (12.37, -1.53),
                (47.91, 106.88),
                (8.49, -13.24),
                (21.02, 105.84),
                (-17.83, 31.05),
                (15.55, 32.53),
                (60.17, 24.94),
                (-9.43, 159.95),
                (29.37, 47.98),
                (24.69, 46.72),
                (1.33, 172.98),
                (6.82, -5.28),
                (-0.55, 166.93),
                (3.87, 11.52)
            };


            MapObjects.Add(new WeatherBalloon(new Coordinate(4940278, 6233593), _windMap));
            WeatherBalloonCounter = 1;
            DirigibleCounter = 0;
        }

        public override void Update(long elapsedMilliseconds)
        {
            try
            {
                foreach (var aerostat in MapObjects.GetAll<WeatherBalloon>())
                {
                    aerostat.Move();
                }
            }
            catch (KeyNotFoundException)
            {
                /* pass */
            }

            try
            {
                foreach (var dirigible in MapObjects.GetAll<Dirigible>())
                {
                    dirigible.Move();
                }
            }
            catch (KeyNotFoundException)
            {
                /* pass */
            }

            if (Rand.MayBe(0.3) && WeatherBalloonCounter < 300)
            {
                var coord = Rand.GenerateNext(_allMap);
                MapObjects.Add(new WeatherBalloon(coord, _windMap));
                Console.WriteLine("На координатах {0}, {1} был запущен аэростат", coord.X, coord.Y);
                WeatherBalloonCounter += 1;
            }
            else if (Rand.MayBe(0.1) && DirigibleCounter < 15)
            {
                var coord1 = _cities.RandomElement();
                var city1 = MathExtensions.LatLonToSpherMerc(coord1.Item1, coord1.Item2);
                var coord2 = _cities.RandomElement();
                var city2 = MathExtensions.LatLonToSpherMerc(coord2.Item1, coord2.Item2);
                MapObjects.Add(new Dirigible(city1, city2));
                Console.WriteLine("На координатах {0}, {1} был запущен дирижабль", city1.X, city1.Y);
                DirigibleCounter += 1;
            }
            else
            {
                try
                {
                    MapObjects.GetAll<WeatherBalloon>().Where(aerostat => aerostat.Declining()).ToList()
                        .ForEach(aerostat =>
                        {
                            MapObjects.Remove(aerostat);
                            var coord = aerostat.Coordinate;
                            WeatherBalloonCounter -= 1;
                            Console.WriteLine("На координатах {0}, {1} приземлился аэростат", coord.X, coord.Y);
                        });
                }
                catch (KeyNotFoundException)
                {
                    /* pass */
                }

                try
                {
                    MapObjects.GetAll<Dirigible>().Where(dirigible => dirigible.inDestination()).ToList()
                        .ForEach(dirigible =>
                        {
                            MapObjects.Remove(dirigible);
                            var coord = dirigible.Coordinate;
                            WeatherBalloonCounter -= 1;
                            Console.WriteLine("На координатах {0}, {1} приземлился аэростат", coord.X, coord.Y);
                        });
                }
                catch (KeyNotFoundException)
                {
                    /* pass */
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
    class WeatherBalloon : Point
    {
        private readonly WindMap _windMap;
        private int _countSteps;
        public WeatherBalloon(Coordinate coordinate, WindMap windMap) : base(coordinate)
        {
            _windMap = windMap;
            _countSteps = Rand.GenerateInRange(500, 700);
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
    
    [CustomStyle(
        @"new ol.style.Style({
            image: new ol.style.Circle({
                opacity: 1.0,
                scale: 1.0,
                radius: 2,
                fill: new ol.style.Fill({
                    color: 'rgba(255, 0, 255, 0.4)'
                }),
                stroke: new ol.style.Stroke({
                    color: 'rgba(0, 0, 0, 0.4)',
                    width: 1
                }),
            })
        });
        ")]
    class Dirigible : Point
    {
        private readonly double _speed;
        private readonly Coordinate _direction;
        
        public Dirigible(Coordinate coordinate, Coordinate direction) : base(coordinate)
        {
            _speed = Rand.GenerateInRange(700, 1500);
            _direction = direction;
        }

        public void Move()
        {
            this.Move(_direction, _speed);
        }

        public bool inDestination()
        {
            return this.distance(_direction) < _speed;
        }

        
    }


}