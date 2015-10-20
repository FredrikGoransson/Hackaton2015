using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace DestroyerGame
{
    class Program
    {
        static void Main(string[] args)
        {
            var timer = new StaticTimer();

            var game = GameBuilder.NewGame();
            var player1 = game.AddPlayer("Bongo");
            game.AddPlayer("Rolly");
            game.AddPlayer("Tyrion");
            game.StartLevel(1);

            var key = ConsoleKey.Play;
            timer.Start();
            while (key != ConsoleKey.Q)
            {
                if (key == ConsoleKey.P)
                {
                    game.ShootProjectile(player1);
                }

                timer.Update();
                game.RunOne(timer);
                game.Render(timer);
                key = Console.ReadKey().Key;
            }
            timer.Stop();
        }
    }

    public static class GameConsoleRenderer
    {
        public static void Render(this Game game, ITimer timer)
        {
            Console.Clear();
            Console.WriteLine(@"                                                         _      _                                     ");
            Console.WriteLine(@" _______   _______     _______.___________..______       U______U  ____    ____  _______ .______      ");
            Console.WriteLine(@"|       \ |   ____|   /       |           ||   _  \      /  __  \  \   \  /   / |   ____||   _  \     ");
            Console.WriteLine(@"|  .--.  ||  |__     |   (----`---|  |----`|  |_)  |    |  |  |  |  \   \/   /  |  |__   |  |_)  |    ");
            Console.WriteLine(@"|  |  |  ||   __|     \   \       |  |     |      /     |  |  |  |   \_    _/   |   __|  |      /     ");
            Console.WriteLine(@"|  '--'  ||  |____.----)   |      |  |     |  |\  \----.|  `--'  |     |  |     |  |____ |  |\  \----.");
            Console.WriteLine(@"|_______/ |_______|_______/       |__|     | _| `._____| \______/      |__|     |_______|| _| `._____|");

            Console.WriteLine();
            Console.WriteLine("Level: {0}, Run {1}", game.Board.Level, timer.Ticks());
            Console.WriteLine("======================================");

            foreach (var item in game.Board.AllItems)
            {
                item.Render();
            }

            game.RenderScene();
        }

        public static void Render(this Item item)
        {
            Console.WriteLine("Item: {0} [{1}]", item.Id, item.GetType().Name);
            Console.WriteLine("Pos: {0:N1} {1:N1}", item.Center.X, item.Center.Y);
            Console.WriteLine("BB:  {0:N1} {1:N1} - {2:N1} {3:N1} ", item.BoundingBox.TopLeft.X, item.BoundingBox.TopLeft.Y, item.BoundingBox.BottomRight.X, item.BoundingBox.BottomRight.Y);
            Console.WriteLine("------------------------------------");
        }

        public static void RenderScene(this Game game)
        {
            var scene = new char[(int)game.Board.Size.Width, (int)game.Board.Size.Height];

            foreach (var item in game.Board.AllItems)
            {
                for (var i = item.BoundingBox.TopLeft.X; i < item.BoundingBox.BottomRight.X; i++)
                {
                    for (var j = item.BoundingBox.TopLeft.Y; j < item.BoundingBox.BottomRight.Y; j++)
                    {
                        if( ((int)i > 0) && ((int)i < game.Board.Size.Width) && ((int)j > 0) && ((int)j < game.Board.Size.Height))
                        {
                            scene[(int) i, (int) j] = item.Id.ToString()[0];
                        }
                    }
                }
            }

            Console.WriteLine("________________________________________________________________________");
            for (var i = game.Board.Size.TopLeft.X; i < game.Board.Size.BottomRight.X; i++)
            {
                for (var j = game.Board.Size.TopLeft.Y; j < game.Board.Size.BottomRight.Y; j++)
                {
                    if (((int)i > 0) && ((int)i < game.Board.Size.Width) && ((int)j > 0) && ((int)j < game.Board.Size.Height))
                    {
                        Console.Write("{0}", scene[(int)i, (int)j]);
                    }
                }
                Console.WriteLine();
            }
            Console.WriteLine("________________________________________________________________________");

            game.RenderCollisions();
        }

        public static void RenderCollisions(this Game game)
        {
            foreach (var collision in game.Collisions)
            {
                Console.WriteLine("Collision {0} {1}", collision.A.Id, collision.B.Id);
            }
        }
    }

    public static class RandomHelper
    {
        public static Point NewPoint(this Random random, Rect bounds)
        {
            return new Point()
            {
                X = (float)random.NextDouble() * bounds.Width + bounds.TopLeft.X,
                Y = (float)random.NextDouble() * bounds.Height + bounds.TopLeft.Y
            };
        }

        public static Vector NewVector(this Random random, float maxX, float maxY)
        {
            return new Vector()
            {
                X = (float)random.NextDouble() * maxX,
                Y = (float)random.NextDouble() * maxY
            };
        }
    }

    public static class GameBuilder
    {
        private readonly static Random Random;

        static GameBuilder()
        {
            Random = new Random(Environment.TickCount);
        }

        public static Game NewGame()
        {
            var game = new Game();
            return game;
        }

        public static Player AddPlayer(this Game game, string name)
        {
            var player = new Player();
            player.Id = game.NextId++;
            player.Name = name;
            game.Players.Add(player);

            player.Geometry = new Point[]
            {
                new Point(){X = -5.0f, Y = -5.0f}, 
                new Point(){X = 5.0f, Y = 0.0f}, 
                new Point(){X = -5.0f, Y = 5.0f}, 
            };

            return player;
        }

        public static Projectile ShootProjectile(this Game game, Motile origin)
        {
            var projectile = new Projectile();
            projectile.Id = game.NextId++;
            projectile.Center = origin.Center;
            projectile.Rotation = origin.Rotation;

            projectile.Geometry = new Point[]
            {
                new Point(){X = -0.5f, Y = -0.5f}, 
                new Point(){X = 0.5f, Y = 0.0f}, 
                new Point(){X = -0.5f, Y = 0.5f}, 
            };
            var speed = 50.0f;
            projectile.Velocity = new Vector()
            {
                X = 10.0f*(float) Math.Cos(projectile.Rotation)*speed,
                Y = 10.0f*(float) Math.Cos(projectile.Rotation)*speed
            };

            game.Board.AllItems.Add(projectile);

            return projectile;
        }

        public static Board StartLevel(this Game game, int level)
        {
            game.Board = new Board();
            game.Board.AllItems.AddRange(game.Players);
            game.Board.Level = level;
            game.Board.Size = new Rect(0, 0, 100, 100);

            // TODO: Create obstacles per level

            // TODO: Init players

            foreach (var player in game.Players)
            {
                player.Center = Random.NewPoint(game.Board.Size);
                player.Velocity = Random.NewVector(game.Board.Size.Width, game.Board.Size.Width);
            }


            return game.Board;
        }

        
    }

    public interface ITimer
    {
        int Ticks();
        float Elapsed();
        void Start();
        void Stop();
        void Update();
    }

    public class Timer : ITimer
    {
        private int _elapsedTicks;
        public int _ticks;

        public int Ticks()
        {
            return _ticks;
        }

        public float Elapsed()
        {
            return _elapsedTicks/1000.0f;
        }

        public void Start()
        {
            _elapsedTicks = Environment.TickCount;
            _ticks = 0;
        }

        public void Stop()
        {
            _ticks = 0;
        }

        public void Update()
        {
            _ticks++;
            _elapsedTicks = Environment.TickCount - _elapsedTicks;
        }
    }

    public class StaticTimer : ITimer
    {
        public int _ticks;

        public int Ticks()
        {
            return _ticks;
        }
        public float Elapsed()
        {
            return 1.0f/60.0f;
        }

        public void Start()
        {
            
        }

        public void Stop()
        {
            
        }

        public void Update()
        {
            _ticks++;
        }
    }

    public class Game
    {
        private readonly ITimer _timer;

        public Game()
        {
            Players = new List<Player>();
            Board = new Board();

            NextId = 0;
        }

        public Board Board;
        public List<Player> Players;

        public List<Collision> Collisions;
 
        public int NextId;

        public void RunOne(ITimer timer)
        {
            foreach (var item in Board.AllItems)
            {
                item.UpdateBoundingBox();
            }

            foreach (var item in Board.AllItems)
            {
                item.UpdatePhysics(timer.Elapsed(), Board);
            }

            Collisions = new List<Collision>(UpdateCollisions());

            foreach (var item in Board.AllItems)
            {
                if (item.Status == ItemStatus.Dead)
                {
                    Board.AllItems.Remove(item);
                }
            }
        }

        public IEnumerable<Collision> UpdateCollisions()
        {
            foreach (var item in Board.AllItems)
            {
                if (item is Motile)
                {
                    foreach (var checkItem in Board.AllItems)
                    {
                        if (item != checkItem && IsOverlapping(item.BoundingBox, checkItem.BoundingBox))
                        {
                            yield return new Collision() {A = item, B = checkItem};
                        }
                    }
                }
            }
        }

        private bool IsOverlapping(Rect rect1, Rect rect2)
        {
            return !(rect2.TopLeft.X > rect1.BottomRight.X
                     || rect2.BottomRight.X < rect1.TopLeft.X
                     || rect2.TopLeft.Y > rect1.BottomRight.Y
                     || rect2.BottomRight.Y < rect1.TopLeft.Y);
        }
    }

    public class Collision
    {
        public Item A;
        public Item B;
    }

    public class Board
    {
        public Board()
        {
            Obstacles = new List<Obstacle>();
            AllItems = new List<Item>();
        }

        public int Level;
        public Rect Size;
        public List<Obstacle> Obstacles;
        public List<Item> AllItems;
    }

    public struct Rect
    {
        public Rect(float x1, float y1, float x2, float y2)
        {
            this.TopLeft = new Point() {X = x1, Y = y1};
            this.BottomRight = new Point() {X = x2, Y = y2};
        }

        public Point TopLeft;
        public Point BottomRight;

        public float Width
        {
            get { return BottomRight.X - TopLeft.X; }
        }
        public float Height
        {
            get { return BottomRight.Y - TopLeft.Y; }
        }
    }

    public struct Point
    {
        public float X;
        public float Y;
    }

    public struct Vector
    {
        public float X;
        public float Y;
    }

    public class Item
    {
        public int Id { get; set; }
        public ItemStatus Status;
        public Point Center;
        public Point[] Geometry;
        public Rect BoundingBox;

        public virtual void UpdateBoundingBox()
        {
            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minY = float.MaxValue;
            var maxY = float.MinValue;

            foreach (var point in this.Geometry)
            {
                var x = this.Center.X + point.X;
                var y = this.Center.Y + point.Y;
                minX = Math.Min(x, minX);
                minY = Math.Min(x, minY);
                maxX = Math.Max(x, maxX);
                maxY = Math.Max(x, maxY);
            }

            BoundingBox = new Rect()
            {
                TopLeft = new Point() { X = minX, Y = minY },
                BottomRight = new Point() { X = maxX, Y = maxY }
            };
        }

        public virtual void UpdatePhysics(float elapsed, Board board)
        {
            
        }
    }

    public enum ItemStatus
    {
        Immobile = 1,
        Alive = 2,
        Dead = 3,
    }

    public class Stationary : Item
    {
        
    }

    public class Motile : Item
    {
        public Vector Velocity;
        public float Rotation;

        public override void UpdatePhysics(float elapsed, Board board)
        {
            var x = this.Center.X + this.Velocity.X * elapsed;
            var y = this.Center.Y + this.Velocity.Y * elapsed;
            this.Center = new Point() { X = x, Y = y };
        }
    }

    public class Player : Motile
    {
        public Player()
        {
            Status = ItemStatus.Alive;
        }

        public string Name;

        public override void UpdatePhysics(float elapsed, Board board)
        {
            var x = this.Center.X + this.Velocity.X * elapsed;
            var y = this.Center.Y + this.Velocity.Y * elapsed;

            if (x > board.Size.Width) x = (x - board.Size.Width);
            if (y > board.Size.Height) y = (y - board.Size.Height);
            if (x < 0) x = (board.Size.Width - x);
            if (y < 0) x = (board.Size.Width - y);

            this.Center = new Point() { X = x, Y = y };
        }
    }


    public class Projectile : Motile
    {
        public Projectile()
        {
            Status = ItemStatus.Alive;
        }

        public override void UpdatePhysics(float elapsed, Board board)
        {
            var x = this.Center.X + this.Velocity.X * elapsed;
            var y = this.Center.Y + this.Velocity.Y * elapsed;

            if ((x > board.Size.Width) ||
                (y > board.Size.Height) ||
                (x < 0) ||
                (y < 0))
            {
                Status = ItemStatus.Dead;
            }

            this.Center = new Point() { X = x, Y = y };
        }
    }

    public class Obstacle : Stationary
    {
        
    }
}
