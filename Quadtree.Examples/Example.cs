using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Quadtree.Examples
{
	class Example : GameWindow
	{
		static void Main(string[] args)
		{
			using (Example example = new Example())
			{
				example.Run(60d, 60d);
			}
		}

		//static Font fontNormal = new Font("assets/DejaVuSans.ttf");
		//static Font fontBold = new Font("assets/DejaVuSans-Bold.ttf");

		RegionQuadtree<Color> quadtree;
		int qtResolution = 6;
		const float qtMultiplier = 4f;
		Dictionary<AABB2i, QuadData> rects;

		int selectionRadius = 40;
		int selection = 0;
		List<Tuple<string, Color>> selections;
		List<List<RegionQuadtree<Color>>> lastRegions;

		List<uint> freeQuadIndexes = new List<uint>();
		List<uint> freeOutlineIndexes = new List<uint>();
		Vector2 position;

		public Example()
			: base(800, 600)
		{

			//var view = rw.GetView();
			//view.Move(new Vector2f(512f, 256f));
			//rw.SetView(view);

			initQuadtree();
			initInput();

			position = getGUIPos(0.3f, 0.5f) - new Vector2(quadtree.AABB.Width / 2f, quadtree.AABB.Height / 2f) * qtMultiplier;

			for (int i = 0; i < 1; i++)
			{
				// quadtree.ExpandFromCenter();
			}

			lastRegions = quadtree.FindConnectedComponents();
			lastRegions.Sort(new Comparison<List<RegionQuadtree<Color>>>((v1, v2) => v1.Count.CompareTo(v2.Count)));
		}

		protected override void OnRenderFrame(FrameEventArgs e)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit);

			this.SwapBuffers();
		}

		public void Run()
		{
			var sw = new Stopwatch();
		}

		struct QuadData
		{
			public uint quadIndex;
			public uint outlineIndex;
		}

		private void initQuadtree()
		{
			quadtree = new RegionQuadtree<Color>(qtResolution);
			quadtree.AutoExpand = true;

			rects = new Dictionary<AABB2i, QuadData>();

			Action<object, RegionQuadtree<Color>.QuadEventArgs<Color>> onQuadAdded = (s, a) =>
			{
				var aabb = a.AABB;
				var quadColor = a.Value;
				var outlineColor = Color.FromArgb(100, 0, 0, 0);
				var rectPoints = new List<Vector2>
				{
					new Vector2(aabb.LowerBound.X, aabb.LowerBound.Y) * qtMultiplier,
					new Vector2(aabb.LowerBound.X + aabb.Width, aabb.LowerBound.Y) * qtMultiplier,
					new Vector2(aabb.LowerBound.X + aabb.Width, aabb.LowerBound.Y + aabb.Height) * qtMultiplier,
					new Vector2(aabb.LowerBound.X, aabb.LowerBound.Y + aabb.Height) * qtMultiplier,
				};

				const float outlineIntend = 1f;
				var outlinePoints = new List<Vector2>
				{
					rectPoints[0] + new Vector2(outlineIntend, outlineIntend),
					rectPoints[1] + new Vector2(-outlineIntend, outlineIntend),
					rectPoints[2] + new Vector2(-outlineIntend, -outlineIntend),
					rectPoints[3] + new Vector2(outlineIntend, -outlineIntend),
				};

				var quadData = new QuadData();

				// quad
				if (freeQuadIndexes.Count > 0)
				{
					quadData.quadIndex = freeQuadIndexes[freeQuadIndexes.Count - 1];
					freeQuadIndexes.RemoveAt(freeQuadIndexes.Count - 1);
				}
				else
				{
				}

				for (uint i = 0; i < 4; i++)
				{
				}


				// outline
				if (freeOutlineIndexes.Count > 0)
				{
					quadData.outlineIndex = freeOutlineIndexes[freeOutlineIndexes.Count - 1];
					freeOutlineIndexes.RemoveAt(freeOutlineIndexes.Count - 1);
				}
				else
				{

				}

				//rects.Add(aabb, quadData);
			};
			quadtree.OnQuadAdded += new EventHandler<RegionQuadtree<Color>.QuadEventArgs<Color>>(onQuadAdded);

			Action<object, RegionQuadtree<Color>.QuadEventArgs<Color>> onQuadRemoving = (s, a) =>
			{
				var quadData = rects[a.AABB];
                
				// quad
				freeQuadIndexes.Add(quadData.quadIndex);
				for (uint i = 0; i < 4; i++)
				{
				}

				// outline
				freeOutlineIndexes.Add(quadData.outlineIndex);
				for (uint i = 0; i < 8; i++)
				{
				}

				rects.Remove(a.AABB);
			};
			quadtree.OnQuadRemoving += new EventHandler<RegionQuadtree<Color>.QuadEventArgs<Color>>(onQuadRemoving);

			Action<object, RegionQuadtree<Color>.QuadChangedEventArgs<Color>> onQuadChanged = (s, a) =>
			{
				var quadData = rects[a.AABB];
				var quadColor = a.Value;
				var outlineColor = Color.FromArgb(100, 0, 0, 0);

				for (uint i = 0; i < 4; i++)
				{
				}

				for (uint i = 0; i < 8; i++)
				{
				}
			};
			quadtree.OnQuadChanged += new EventHandler<RegionQuadtree<Color>.QuadChangedEventArgs<Color>>(onQuadChanged);

			Action<object, RegionQuadtree<Color>.QuadExpandEventArgs<Color>> onExpand = (s, a) =>
			{
				var oldRoot = quadtree;
				var newRoot = a.NewRoot;

				this.quadtree = newRoot;

				qtResolution++;

				position -= new Vector2(a.Offset.X, a.Offset.Y) * qtMultiplier;

			};
			quadtree.OnExpand += new EventHandler<RegionQuadtree<Color>.QuadExpandEventArgs<Color>>(onExpand);

			quadtree.Set(Color.White);
		}

		private void initInput()
		{
			selections = new List<Tuple<string, Color>>
			{
				new Tuple<string, Color>("Red", Color.FromArgb(231, 76, 60)),
				new Tuple<string, Color>("Green", Color.FromArgb(46, 204, 113)),
				new Tuple<string, Color>("Blue", Color.FromArgb(52, 152, 219)),
				new Tuple<string, Color>("Yellow", Color.FromArgb(241, 196, 15)),
				new Tuple<string, Color>("Turquoise", Color.FromArgb(26, 188, 156)),
				new Tuple<string, Color>("Purple", Color.FromArgb(155, 89, 182)),
				new Tuple<string, Color>("Orange", Color.FromArgb(230, 126, 34)),
				new Tuple<string, Color>("White", Color.White),
			};
		}

		private Vector2 getGUIPos(float x, float y)
		{
			return new Vector2();
		}
	}
}
