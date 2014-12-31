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

		const string vertexSource =
@"#version 150 core
in vec2 position;
in vec3 color;
out vec3 Color;
uniform mat4 projection;

void main() {
	Color = color;
	gl_Position = projection * vec4(position, 0.0, 1.0);
}";
		const string fragmentSource =
@"#version 150 core
in vec3 Color;
out vec4 outColor;
void main() {
	outColor = vec4(Color, 1.0);
}";

		//static Font fontNormal = new Font("assets/DejaVuSans.ttf");
		//static Font fontBold = new Font("assets/DejaVuSans-Bold.ttf");

        const int screenWidth = 800;
        const int screenHeight = 600;
        Matrix4 projection;

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

		float[] qtVertices = new float[] {
			0f, 0f,     1f, 0f, 0f,
            100f, 0f,   1f, 0f, 0f,
            100f, 100f, 1f, 0f, 0f,
            0f, 100f,   1f, 0f, 0f,
		};

        int[] qtElements = new int[] {
            0, 1, 2,
            2, 3, 0
        };

		public Example()
            : base(screenWidth, screenHeight)
		{
			//var view = rw.GetView();
			//view.Move(new Vector2f(512f, 256f));
			//rw.SetView(view);

			initQuadtree();
            initRendering();
			initInput();

			//posResColorData = new float[quadtree.AABB.Width * quadtree.AABB.Height];

			position = getGUIPos(0.3f, 0.5f) - new Vector2(quadtree.AABB.Width / 2f, quadtree.AABB.Height / 2f) * qtMultiplier;

			for (int i = 0; i < 1; i++)
			{
				// quadtree.ExpandFromCenter();
			}

			lastRegions = quadtree.FindConnectedComponents();
			lastRegions.Sort(new Comparison<List<RegionQuadtree<Color>>>((v1, v2) => v1.Count.CompareTo(v2.Count)));
		}

        private void initRendering()
        {
            var vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            var vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(qtVertices.Length * sizeof(float)), qtVertices, BufferUsageHint.DynamicDraw);

            var ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(qtElements.Length * sizeof(float)), qtElements, BufferUsageHint.DynamicDraw);

            var vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexSource);
            GL.CompileShader(vertexShader);

            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentSource);
            GL.CompileShader(fragmentShader);

            var shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);
            GL.BindFragDataLocation(shaderProgram, 0, "outColor"); // Not needed if only 1 output
            GL.LinkProgram(shaderProgram);
            GL.UseProgram(shaderProgram);

            var posAttrib = GL.GetAttribLocation(shaderProgram, "position");
            GL.EnableVertexAttribArray(posAttrib);
            GL.VertexAttribPointer(posAttrib, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            var colorAttrib = GL.GetAttribLocation(shaderProgram, "color");
            GL.EnableVertexAttribArray(colorAttrib);
            GL.VertexAttribPointer(colorAttrib, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 2 * sizeof(float));



            var uniProjection = GL.GetUniformLocation(shaderProgram, "projection");
            projection = Matrix4.CreateOrthographic(screenWidth, screenHeight, 0f, 0f);
            //projection = Matrix4.CreateOrthographicOffCenter(0f, screenWidth, screenHeight, 0f, 0f, 0f);
            GL.UniformMatrix4(uniProjection, true, ref projection);
            GL.Viewport(0, 0, screenWidth, screenHeight);
        }

		protected override void OnRenderFrame(FrameEventArgs e)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedInt, 0);

			this.SwapBuffers();
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
