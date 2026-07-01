using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

IWindow window = Window.Create(WindowOptions.Default with
{
  Size = new Vector2D<int>(800,600),
  Title = "WMMO Main Window"
});

GL? gl = null;
uint pVao = 0;
uint pVbo = 0;
uint pEbo = 0;
IInputContext? inputContext = null;

float[] quadVertices =
[
  0.5f,  0.5f, 0.0f,
  0.5f, -0.5f, 0.0f,
  -0.5f, -0.5f, 0.0f,
  -0.5f,  0.5f, 0.0f
];

uint[] indices =
[
  0u, 1u, 3u,
  1u, 2u, 3u
];

window.Load += () =>
{
  Console.WriteLine("Window Loaded");

  Console.WriteLine("Creating OpenGL");
  gl = window.CreateOpenGL();
  gl.ClearColor(1f, 0.5f, 0f, 0f);
  gl.Clear(ClearBufferMask.ColorBufferBit);
  
  pVao = gl.GenVertexArray();
  gl.BindVertexArray(pVao);
  Console.WriteLine($"VAO Pointer: {pVao}");
  
  pVbo = gl.GenBuffer();
  gl.BindBuffer(
    target: BufferTargetARB.ArrayBuffer, 
    buffer: pVbo);
  Console.WriteLine($"VBO Pointer: {pVbo}");

  pEbo = gl.GenBuffer();
  gl.BindBuffer(
    target: BufferTargetARB.ElementArrayBuffer,
    buffer: pEbo);
  Console.WriteLine($"EBO Pointer: {pEbo}");
  
  gl.BufferData(
    target: BufferTargetARB.ArrayBuffer,
    data: quadVertices.AsSpan(),
    usage: BufferUsageARB.StaticDraw);

  gl.BufferData(
    target: BufferTargetARB.ElementArrayBuffer,
    data: indices.AsSpan(),
    usage: BufferUsageARB.StaticDraw);
  
  Console.WriteLine("Creating Input Context");
  inputContext = window.CreateInput();
  foreach (IKeyboard keyboard in inputContext.Keyboards)
  {
    keyboard.KeyDown += (thisKeyboard, key, keyCode) =>
    {
      if (key == Key.Escape)
      {
        window.Close();
        return;
      }
      
      Console.WriteLine($"Key: {key}");
    };
  }
};

window.Update += delta =>
{
  //Console.WriteLine($"Window Update: {delta}");
};

window.Render += delta =>
{
  //Console.WriteLine($"Window Render: {delta}");
};

window.Run();